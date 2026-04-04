using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PharmaGo.Api.Realtime;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Reservations.Commands.CreateReservation;
using PharmaGo.Application.Reservations.Commands.UpdateReservationStatus;
using PharmaGo.Application.Reservations.Queries.GetReservation;
using PharmaGo.Application.Stocks.Queries.GetLowStockAlerts;
using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.Infrastructure.Caching;
using PharmaGo.Infrastructure.Auth;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController(
    IApplicationDbContext context,
    IAppCacheService cacheService,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    IReservationStateService reservationStateService,
    RealtimeNotificationService realtimeNotificationService) : ControllerBase
{
    [Authorize(Policy = PolicyNames.ReadOwnReservations)]
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ReservationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ReservationResponse>>> GetMyReservations(CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var reservations = await QueryReservations()
            .Where(x => x.CustomerId == currentUserService.UserId.Value)
            .OrderByDescending(x => x.ReservedUntilUtc)
            .ToListAsync(cancellationToken);

        return Ok(reservations);
    }

    [Authorize(Policy = PolicyNames.ManageOrders)]
    [HttpGet("pharmacy/{pharmacyId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<ReservationResponse>>> GetForPharmacy(
        Guid pharmacyId,
        [FromQuery] ReservationStatus? status,
        CancellationToken cancellationToken)
    {
        var accessResult = await EnsurePharmacyStaffAccessAsync(pharmacyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var reservations = await QueryReservations()
            .Where(x => x.PharmacyId == pharmacyId && (!status.HasValue || x.Status == status.Value))
            .OrderByDescending(x => x.ReservedUntilUtc)
            .ToListAsync(cancellationToken);

        return Ok(reservations);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await QueryReservations()
            .Where(x => x.ReservationId == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (reservation is null)
        {
            return NotFound();
        }

        var isStaff = User.IsInRole(RoleNames.Pharmacist) || User.IsInRole(RoleNames.Moderator);
        if (!isStaff && reservation.CustomerId != currentUserService.UserId)
        {
            return Forbid();
        }

        return Ok(reservation);
    }

    [Authorize(Policy = PolicyNames.CreateReservations)]
    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> Create(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var dbContext = (ApplicationDbContext)context;

        if (request.Items.Count == 0)
        {
            return BadRequest("At least one reservation item is required.");
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            return BadRequest("Reservation item quantities must be greater than zero.");
        }

        if (request.ReserveForHours <= 0 || request.ReserveForHours > 24)
        {
            return BadRequest("ReserveForHours must be between 1 and 24.");
        }

        if (!currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            ReservationResponse? response = null;
            List<StockItem>? affectedStocks = null;
            Guid pharmacyId = Guid.Empty;
            Guid customerId = Guid.Empty;

            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                var pharmacy = await context.Pharmacies
                    .FirstOrDefaultAsync(x => x.Id == request.PharmacyId && x.IsActive, cancellationToken);

                if (pharmacy is null)
                {
                    throw new ReservationRequestException(StatusCodes.Status404NotFound, "Pharmacy was not found.");
                }

                var requestedMedicineIds = request.Items.Select(item => item.MedicineId).Distinct().ToArray();
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var stockItems = await context.StockItems
                    .Include(x => x.Medicine)
                    .Where(x => x.PharmacyId == request.PharmacyId &&
                        requestedMedicineIds.Contains(x.MedicineId) &&
                        x.IsActive &&
                        x.IsReservable &&
                        x.ExpirationDate >= today &&
                        x.Quantity > x.ReservedQuantity)
                    .OrderBy(x => x.ExpirationDate)
                    .ToListAsync(cancellationToken);

                var stockByMedicine = stockItems.GroupBy(x => x.MedicineId).ToDictionary(group => group.Key, group => group.ToList());

                foreach (var item in request.Items)
                {
                    if (!stockByMedicine.TryGetValue(item.MedicineId, out var medicineStock))
                    {
                        throw new ReservationRequestException(
                            StatusCodes.Status400BadRequest,
                            $"Medicine '{item.MedicineId}' is not available in the selected pharmacy.");
                    }

                    var availableQuantity = medicineStock.Sum(stock => stock.AvailableQuantity);
                    if (availableQuantity < item.Quantity)
                    {
                        throw new ReservationRequestException(
                            StatusCodes.Status400BadRequest,
                            $"Insufficient stock for medicine '{medicineStock[0].Medicine!.BrandName}'.");
                    }
                }

                var customer = await context.Users.FirstOrDefaultAsync(
                    x => x.Id == currentUserService.UserId.Value && x.IsActive,
                    cancellationToken);

                if (customer is null)
                {
                    throw new ReservationRequestException(StatusCodes.Status401Unauthorized, "User is not authorized.");
                }

                var reservation = new Reservation
                {
                    ReservationNumber = GenerateReservationNumber(),
                    Customer = customer,
                    Pharmacy = pharmacy,
                    Status = ReservationStatus.Confirmed,
                    Notes = request.Notes?.Trim(),
                    ReservedUntilUtc = DateTime.UtcNow.AddHours(request.ReserveForHours),
                    ConfirmedAtUtc = DateTime.UtcNow,
                    TelegramChatId = customer.TelegramChatId
                };

                affectedStocks = new List<StockItem>();

                foreach (var item in request.Items)
                {
                    var requestedQuantity = item.Quantity;
                    var medicineStock = stockByMedicine[item.MedicineId];
                    var unitPrice = medicineStock.Min(stock => stock.RetailPrice);
                    var medicine = medicineStock[0].Medicine!;

                    foreach (var stock in medicineStock)
                    {
                        if (requestedQuantity == 0)
                        {
                            break;
                        }

                        var quantityToReserve = Math.Min(requestedQuantity, stock.AvailableQuantity);
                        stock.ReservedQuantity += quantityToReserve;
                        requestedQuantity -= quantityToReserve;
                        affectedStocks.Add(stock);
                    }

                    if (requestedQuantity > 0)
                    {
                        throw new ReservationRequestException(
                            StatusCodes.Status409Conflict,
                            $"Stock changed while reserving '{medicine.BrandName}'. Please refresh and try again.");
                    }

                    reservation.Items.Add(new ReservationItem
                    {
                        MedicineId = item.MedicineId,
                        Medicine = medicine,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice
                    });
                }

                reservation.TotalAmount = reservation.Items.Sum(item => item.TotalPrice);

                await context.Reservations.AddAsync(reservation, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                pharmacyId = reservation.PharmacyId;
                customerId = customer.Id;

                response = new ReservationResponse
                {
                    ReservationId = reservation.Id,
                    ReservationNumber = reservation.ReservationNumber,
                    Status = reservation.Status,
                    PharmacyId = pharmacy.Id,
                    PharmacyName = pharmacy.Name,
                    CustomerId = customer.Id,
                    CustomerFullName = $"{customer.FirstName} {customer.LastName}",
                    PhoneNumber = customer.PhoneNumber,
                    ReservedUntilUtc = reservation.ReservedUntilUtc,
                    TotalAmount = reservation.TotalAmount,
                    Notes = reservation.Notes,
                    Items = reservation.Items.Select(item => new ReservationItemResponse
                    {
                        MedicineId = item.MedicineId,
                        MedicineName = item.Medicine!.BrandName,
                        GenericName = item.Medicine.GenericName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList()
                };
            });

            await cacheService.BumpScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);
            await cacheService.BumpScopeVersionAsync(CacheScopes.Dashboard, cancellationToken);
            await auditService.WriteAsync(
                action: "reservation.created",
                entityName: "Reservation",
                entityId: response!.ReservationId.ToString(),
                userId: customerId,
                pharmacyId: pharmacyId,
                description: $"Reservation {response.ReservationNumber} created.",
                metadata: new
                {
                    response.ReservationId,
                    response.ReservationNumber,
                    response.PharmacyId,
                    response.CustomerId,
                    response.TotalAmount,
                    ItemCount = response.Items.Count
                },
                cancellationToken: cancellationToken);

            await realtimeNotificationService.NotifyReservationCreatedAsync(pharmacyId, response!, cancellationToken);
            await PublishLowStockNotificationsAsync(affectedStocks ?? [], cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = response!.ReservationId }, response);
        }
        catch (ReservationRequestException exception)
        {
            return StatusCode(exception.StatusCode, exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Stock changed while creating the reservation. Please refresh and try again.");
        }
        catch (DbUpdateException exception) when (IsTransientConcurrencyConflict(exception))
        {
            return Conflict("Stock changed while creating the reservation. Please refresh and try again.");
        }
    }

    [Authorize]
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateReservationStatusRequest request,
        CancellationToken cancellationToken)
    {
        var dbContext = (ApplicationDbContext)context;
        var reservation = await context.Reservations
            .Include(x => x.Items)
            .ThenInclude(x => x.Medicine)
            .Include(x => x.Customer)
            .Include(x => x.Pharmacy)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (reservation is null)
        {
            return NotFound();
        }

        if (reservation.Status == request.Status)
        {
            var currentResponse = await QueryReservations()
                .Where(x => x.ReservationId == reservation.Id)
                .FirstAsync(cancellationToken);

            return Ok(currentResponse);
        }

        var isModerator = User.IsInRole(RoleNames.Moderator);
        var isPharmacist = User.IsInRole(RoleNames.Pharmacist);
        var isOwner = currentUserService.UserId == reservation.CustomerId;

        if (isPharmacist && !isModerator)
        {
            var accessResult = await EnsurePharmacyStaffAccessAsync(reservation.PharmacyId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }
        }

        if (!CanChangeStatus(reservation.Status, request.Status, isOwner, isPharmacist || isModerator))
        {
            return BadRequest("The requested reservation status transition is not allowed.");
        }

        if (!isOwner && !isPharmacist && !isModerator)
        {
            return Forbid();
        }

        List<StockItem> completedStocks = [];

        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                reservation = await context.Reservations
                    .Include(x => x.Items)
                    .ThenInclude(x => x.Medicine)
                    .Include(x => x.Customer)
                    .Include(x => x.Pharmacy)
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

                if (reservation is null)
                {
                    throw new ReservationRequestException(StatusCodes.Status404NotFound, "Reservation was not found.");
                }

                if (reservation.Status == request.Status)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return;
                }

                if (!CanChangeStatus(reservation.Status, request.Status, isOwner, isPharmacist || isModerator))
                {
                    throw new ReservationRequestException(
                        StatusCodes.Status400BadRequest,
                        "The requested reservation status transition is not allowed.");
                }

                reservation.Status = request.Status;

                if (request.Status == ReservationStatus.Cancelled)
                {
                    reservation.CancelledAtUtc = DateTime.UtcNow;
                    await reservationStateService.ReleaseReservedStockAsync(reservation, cancellationToken);
                }

                if (request.Status == ReservationStatus.Expired)
                {
                    await reservationStateService.ReleaseReservedStockAsync(reservation, cancellationToken);
                }

                if (request.Status == ReservationStatus.Completed)
                {
                    completedStocks = await context.StockItems
                        .Where(x => x.PharmacyId == reservation.PharmacyId &&
                            reservation.Items.Select(item => item.MedicineId).Contains(x.MedicineId))
                        .ToListAsync(cancellationToken);

                    await reservationStateService.CompleteReservationAsync(reservation, cancellationToken);
                }

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });
        }
        catch (ReservationRequestException exception)
        {
            return StatusCode(exception.StatusCode, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Reservation or stock changed while updating status. Please refresh and try again.");
        }
        catch (DbUpdateException exception) when (IsTransientConcurrencyConflict(exception))
        {
            return Conflict("Reservation or stock changed while updating status. Please refresh and try again.");
        }

        if (request.Status == ReservationStatus.Completed)
        {
            await PublishLowStockNotificationsAsync(completedStocks, cancellationToken);
        }

        await cacheService.BumpScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.Dashboard, cancellationToken);

        var response = await QueryReservations()
            .Where(x => x.ReservationId == id)
            .FirstAsync(cancellationToken);

        await realtimeNotificationService.NotifyReservationStatusChangedAsync(
            response.PharmacyId,
            response.CustomerId,
            response,
            cancellationToken);
        await auditService.WriteAsync(
            action: "reservation.status.updated",
            entityName: "Reservation",
            entityId: response.ReservationId.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: response.PharmacyId,
            description: $"Reservation {response.ReservationNumber} status changed to {response.Status}.",
            metadata: new
            {
                response.ReservationId,
                response.ReservationNumber,
                Status = response.Status.ToString()
            },
            cancellationToken: cancellationToken);

        return Ok(response);
    }

    private IQueryable<ReservationResponse> QueryReservations()
    {
        return context.Reservations
            .AsNoTracking()
            .Select(x => new ReservationResponse
            {
                ReservationId = x.Id,
                ReservationNumber = x.ReservationNumber,
                Status = x.Status,
                PharmacyId = x.PharmacyId,
                PharmacyName = x.Pharmacy!.Name,
                CustomerId = x.CustomerId,
                CustomerFullName = $"{x.Customer!.FirstName} {x.Customer.LastName}",
                PhoneNumber = x.Customer.PhoneNumber,
                ReservedUntilUtc = x.ReservedUntilUtc,
                TotalAmount = x.TotalAmount,
                Notes = x.Notes,
                Items = x.Items.Select(item => new ReservationItemResponse
                {
                    MedicineId = item.MedicineId,
                    MedicineName = item.Medicine!.BrandName,
                    GenericName = item.Medicine.GenericName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                }).ToList()
            });
    }

    private async Task<ActionResult?> EnsurePharmacyStaffAccessAsync(Guid pharmacyId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(RoleNames.Moderator))
        {
            return null;
        }

        if (!currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var currentUser = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == currentUserService.UserId.Value, cancellationToken);

        if (currentUser is null || currentUser.PharmacyId != pharmacyId)
        {
            return Forbid();
        }

        return null;
    }

    private static bool CanChangeStatus(
        ReservationStatus currentStatus,
        ReservationStatus nextStatus,
        bool isOwner,
        bool isStaff)
    {
        if (isOwner)
        {
            return nextStatus == ReservationStatus.Cancelled &&
                (currentStatus == ReservationStatus.Pending ||
                 currentStatus == ReservationStatus.Confirmed ||
                 currentStatus == ReservationStatus.ReadyForPickup);
        }

        if (!isStaff)
        {
            return false;
        }

        return (currentStatus, nextStatus) switch
        {
            (ReservationStatus.Pending, ReservationStatus.Confirmed) => true,
            (ReservationStatus.Pending, ReservationStatus.Cancelled) => true,
            (ReservationStatus.Pending, ReservationStatus.Expired) => true,
            (ReservationStatus.Confirmed, ReservationStatus.ReadyForPickup) => true,
            (ReservationStatus.Confirmed, ReservationStatus.Cancelled) => true,
            (ReservationStatus.Confirmed, ReservationStatus.Expired) => true,
            (ReservationStatus.ReadyForPickup, ReservationStatus.Completed) => true,
            (ReservationStatus.ReadyForPickup, ReservationStatus.Cancelled) => true,
            (ReservationStatus.ReadyForPickup, ReservationStatus.Expired) => true,
            _ => false
        };
    }

    private static string GenerateReservationNumber()
    {
        return $"RES-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
    }

    private async Task PublishLowStockNotificationsAsync(IEnumerable<StockItem> stockItems, CancellationToken cancellationToken)
    {
        foreach (var stockItem in stockItems
                     .GroupBy(x => x.Id)
                     .Select(group => group.Last())
                     .Where(x => x.AvailableQuantity <= x.ReorderLevel))
        {
            await realtimeNotificationService.NotifyLowStockAsync(stockItem.PharmacyId, new LowStockAlertResponse
            {
                StockItemId = stockItem.Id,
                PharmacyId = stockItem.PharmacyId,
                PharmacyName = stockItem.Pharmacy?.Name ?? string.Empty,
                MedicineId = stockItem.MedicineId,
                MedicineName = stockItem.Medicine?.BrandName ?? string.Empty,
                GenericName = stockItem.Medicine?.GenericName ?? string.Empty,
                BatchNumber = stockItem.BatchNumber,
                ExpirationDate = stockItem.ExpirationDate,
                Quantity = stockItem.Quantity,
                ReservedQuantity = stockItem.ReservedQuantity,
                AvailableQuantity = stockItem.AvailableQuantity,
                ReorderLevel = stockItem.ReorderLevel,
                Deficit = stockItem.ReorderLevel - stockItem.AvailableQuantity,
                RetailPrice = stockItem.RetailPrice
            }, cancellationToken);
        }
    }

    private static bool IsTransientConcurrencyConflict(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException &&
               (postgresException.SqlState == PostgresErrorCodes.SerializationFailure ||
                postgresException.SqlState == PostgresErrorCodes.DeadlockDetected);
    }

    private sealed class ReservationRequestException(int statusCode, string message) : Exception(message)
    {
        public int StatusCode { get; } = statusCode;
    }
}

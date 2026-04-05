using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PharmaGo.Api.Realtime;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Reservations.Commands.CreateReservation;
using PharmaGo.Application.Reservations.Commands.UpdateReservationStatus;
using PharmaGo.Application.Reservations.Queries.GetReservation;
using PharmaGo.Application.Reservations.Queries.GetReservationTimeline;
using PharmaGo.Application.Stocks.Queries.GetLowStockAlerts;
using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.Infrastructure.Caching;
using PharmaGo.Infrastructure.Auth;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
public class ReservationsController(
    IApplicationDbContext context,
    IAppCacheService cacheService,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    IReservationNotificationService reservationNotificationService,
    IReservationStateService reservationStateService,
    IReservationTransitionPolicy reservationTransitionPolicy,
    RealtimeNotificationService realtimeNotificationService) : ApiControllerBase
{
    private const string IdempotencyKeyHeaderName = "Idempotency-Key";

    [Authorize(Policy = PolicyNames.ReadOwnReservations)]
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ReservationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ReservationResponse>>> GetMyReservations(CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
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
    [HttpGet("active")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ReservationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ReservationResponse>>> GetActive(
        [FromQuery] Guid? pharmacyId,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var activeStatuses = new[]
        {
            ReservationStatus.Pending,
            ReservationStatus.Confirmed,
            ReservationStatus.ReadyForPickup
        };

        var now = DateTime.UtcNow;
        var isModerator = User.IsInRole(RoleNames.Moderator);
        var isPharmacist = User.IsInRole(RoleNames.Pharmacist);

        IQueryable<ReservationResponse> query = QueryReservations()
            .Where(x => activeStatuses.Contains(x.Status) && x.ReservedUntilUtc > now);

        if (isPharmacist || isModerator)
        {
            var effectivePharmacyId = pharmacyId;

            if (effectivePharmacyId.HasValue)
            {
                var accessResult = await EnsurePharmacyStaffAccessAsync(effectivePharmacyId.Value, cancellationToken);
                if (accessResult is not null)
                {
                    return accessResult;
                }
            }
            else if (isPharmacist && !isModerator)
            {
                var currentUser = await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == currentUserService.UserId.Value, cancellationToken);

                effectivePharmacyId = currentUser?.PharmacyId;
            }

            if (effectivePharmacyId.HasValue)
            {
                query = query.Where(x => x.PharmacyId == effectivePharmacyId.Value);
            }
        }
        else
        {
            query = query.Where(x => x.CustomerId == currentUserService.UserId.Value);
        }

        var reservations = await query
            .OrderBy(x => x.ReservedUntilUtc)
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
            return ApiNotFound("reservation_not_found", "Reservation was not found.");
        }

        var isStaff = User.IsInRole(RoleNames.Pharmacist) || User.IsInRole(RoleNames.Moderator);
        if (!isStaff && reservation.CustomerId != currentUserService.UserId)
        {
            return ApiForbidden("You do not have access to this reservation.");
        }

        if (isStaff && User.IsInRole(RoleNames.Pharmacist) && !User.IsInRole(RoleNames.Moderator))
        {
            var accessResult = await EnsurePharmacyStaffAccessAsync(reservation.PharmacyId, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }
        }

        return Ok(reservation);
    }

    [Authorize]
    [HttpGet("{id:guid}/timeline")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ReservationTimelineEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<ReservationTimelineEventResponse>>> GetTimeline(
        Guid id,
        CancellationToken cancellationToken)
    {
        var reservation = await context.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (reservation is null)
        {
            return ApiNotFound("reservation_not_found", "Reservation was not found.");
        }

        var accessResult = await EnsureReservationAccessAsync(reservation.CustomerId, reservation.PharmacyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var auditEvents = await context.AuditLogs
            .AsNoTracking()
            .Where(x => x.EntityName == "Reservation" && x.EntityId == id.ToString())
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Action,
                x.Description,
                x.MetadataJson,
                x.CreatedAtUtc,
                x.UserId,
                UserFullName = x.User != null ? $"{x.User.FirstName} {x.User.LastName}" : null
            })
            .ToListAsync(cancellationToken);

        var events = auditEvents
            .Select(x => new ReservationTimelineEventResponse
            {
                Action = x.Action,
                Title = ToTimelineTitle(x.Action),
                Description = x.Description,
                Status = TryReadStatus(x.Action, x.MetadataJson),
                OccurredAtUtc = x.CreatedAtUtc,
                UserId = x.UserId,
                UserFullName = x.UserFullName,
                IsSystemEvent = x.UserId == null
            })
            .ToList();

        return Ok(events);
    }

    [Authorize(Policy = PolicyNames.CreateReservations)]
    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservationResponse>> Create(
        [FromBody] CreateReservationRequest request,
        [FromHeader(Name = IdempotencyKeyHeaderName)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var dbContext = (ApplicationDbContext)context;
        idempotencyKey = idempotencyKey?.Trim();

        if (request.Items.Count == 0)
        {
            return ValidationProblemResponse("reservation_validation_error", "At least one reservation item is required.");
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            return ValidationProblemResponse("reservation_validation_error", "Reservation item quantities must be greater than zero.");
        }

        if (request.ReserveForHours <= 0 || request.ReserveForHours > 24)
        {
            return ValidationProblemResponse("reservation_validation_error", "ReserveForHours must be between 1 and 24.");
        }

        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        if (!string.IsNullOrWhiteSpace(idempotencyKey) && idempotencyKey.Length > 128)
        {
            return ValidationProblemResponse("reservation_validation_error", $"{IdempotencyKeyHeaderName} cannot exceed 128 characters.");
        }

        var requestHash = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : ComputeRequestHash(request);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var idempotentResponse = await TryResolveIdempotentCreateAsync(
                currentUserService.UserId.Value,
                idempotencyKey,
                requestHash!,
                cancellationToken);

            if (idempotentResponse is not null)
            {
                Response.Headers.Append("X-Idempotent-Replay", "true");
                return idempotentResponse;
            }
        }

        try
        {
            ReservationResponse? response = null;
            List<StockItem>? affectedStocks = null;
            Guid pharmacyId = Guid.Empty;
            Guid customerId = Guid.Empty;
            ReservationIdempotencyRecord? idempotencyRecord = null;

            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    idempotencyRecord = await context.ReservationIdempotencyRecords
                        .FirstOrDefaultAsync(
                            x => x.UserId == currentUserService.UserId.Value && x.IdempotencyKey == idempotencyKey,
                            cancellationToken);

                    if (idempotencyRecord is not null)
                    {
                        if (!string.Equals(idempotencyRecord.RequestHash, requestHash, StringComparison.Ordinal))
                        {
                            throw new ReservationRequestException(
                                StatusCodes.Status409Conflict,
                                "This idempotency key has already been used with a different reservation payload.",
                                "reservation_idempotency_conflict");
                        }

                        if (idempotencyRecord.ReservationId.HasValue)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            return;
                        }

                        throw new ReservationRequestException(
                            StatusCodes.Status409Conflict,
                            "A reservation request with the same idempotency key is already being processed.",
                            "reservation_idempotency_in_progress");
                    }

                    idempotencyRecord = new ReservationIdempotencyRecord
                    {
                        UserId = currentUserService.UserId.Value,
                        IdempotencyKey = idempotencyKey,
                        RequestHash = requestHash!
                    };

                    await context.ReservationIdempotencyRecords.AddAsync(idempotencyRecord, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }

                var pharmacy = await context.Pharmacies
                    .FirstOrDefaultAsync(x => x.Id == request.PharmacyId && x.IsActive, cancellationToken);

                if (pharmacy is null)
                {
                    throw new ReservationRequestException(StatusCodes.Status404NotFound, "Pharmacy was not found.", "reservation_pharmacy_not_found");
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
                            $"Medicine '{item.MedicineId}' is not available in the selected pharmacy.",
                            "reservation_medicine_unavailable");
                    }

                    var availableQuantity = medicineStock.Sum(stock => stock.AvailableQuantity);
                    if (availableQuantity < item.Quantity)
                    {
                        throw new ReservationRequestException(
                            StatusCodes.Status422UnprocessableEntity,
                            $"Insufficient stock for medicine '{medicineStock[0].Medicine!.BrandName}'.",
                            "reservation_insufficient_stock");
                    }
                }

                var customer = await context.Users.FirstOrDefaultAsync(
                    x => x.Id == currentUserService.UserId.Value && x.IsActive,
                    cancellationToken);

                if (customer is null)
                {
                    throw new ReservationRequestException(StatusCodes.Status401Unauthorized, "User is not authorized.", "reservation_unauthorized");
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
                            $"Stock changed while reserving '{medicine.BrandName}'. Please refresh and try again.",
                            "reservation_stock_conflict");
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

                if (idempotencyRecord is not null)
                {
                    idempotencyRecord.ReservationId = reservation.Id;
                    idempotencyRecord.CompletedAtUtc = DateTime.UtcNow;
                }

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
                    CreatedAtUtc = reservation.CreatedAtUtc,
                    ReservedUntilUtc = reservation.ReservedUntilUtc,
                    ConfirmedAtUtc = reservation.ConfirmedAtUtc,
                    ReadyForPickupAtUtc = reservation.ReadyForPickupAtUtc,
                    CompletedAtUtc = reservation.CompletedAtUtc,
                    CancelledAtUtc = reservation.CancelledAtUtc,
                    ExpiredAtUtc = reservation.ExpiredAtUtc,
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
                    Status = response.Status.ToString(),
                    response.PharmacyId,
                    response.CustomerId,
                    response.TotalAmount,
                    ItemCount = response.Items.Count
                },
                cancellationToken: cancellationToken);

            await realtimeNotificationService.NotifyReservationCreatedAsync(pharmacyId, response!, cancellationToken);
            var createdReservation = await context.Reservations
                .AsNoTracking()
                .Include(x => x.Pharmacy)
                .FirstAsync(x => x.Id == response.ReservationId, cancellationToken);
            await reservationNotificationService.DispatchStatusNotificationAsync(
                createdReservation,
                ReservationStatus.Pending,
                cancellationToken);
            await PublishLowStockNotificationsAsync(affectedStocks ?? [], cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = response!.ReservationId }, response);
        }
        catch (ReservationRequestException exception)
        {
            return ProblemResponse(exception.StatusCode, exception.Code, exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemResponse(StatusCodes.Status409Conflict, "reservation_stock_conflict", "Stock changed while creating the reservation. Please refresh and try again.");
        }
        catch (DbUpdateException exception) when (IsTransientConcurrencyConflict(exception))
        {
            return ProblemResponse(StatusCodes.Status409Conflict, "reservation_stock_conflict", "Stock changed while creating the reservation. Please refresh and try again.");
        }
        catch (DbUpdateException exception) when (IsUniqueIdempotencyViolation(exception))
        {
            var replay = await TryResolveIdempotentCreateAsync(
                currentUserService.UserId.Value,
                idempotencyKey!,
                requestHash!,
                cancellationToken);

            if (replay is not null)
            {
                Response.Headers.Append("X-Idempotent-Replay", "true");
                return replay;
            }

            return ProblemResponse(StatusCodes.Status409Conflict, "reservation_idempotency_in_progress", "A reservation request with the same idempotency key is already being processed.");
        }
    }

    [Authorize(Policy = PolicyNames.ManageOrders)]
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public Task<ActionResult<ReservationResponse>> Confirm(Guid id, CancellationToken cancellationToken)
        => TransitionAsync(id, ReservationStatus.Confirmed, cancellationToken);

    [Authorize(Policy = PolicyNames.ManageOrders)]
    [HttpPost("{id:guid}/ready-for-pickup")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public Task<ActionResult<ReservationResponse>> MarkReadyForPickup(Guid id, CancellationToken cancellationToken)
        => TransitionAsync(id, ReservationStatus.ReadyForPickup, cancellationToken);

    [Authorize(Policy = PolicyNames.ManageOrders)]
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public Task<ActionResult<ReservationResponse>> Complete(Guid id, CancellationToken cancellationToken)
        => TransitionAsync(id, ReservationStatus.Completed, cancellationToken);

    [Authorize]
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public Task<ActionResult<ReservationResponse>> Cancel(Guid id, CancellationToken cancellationToken)
        => TransitionAsync(id, ReservationStatus.Cancelled, cancellationToken);

    [Authorize(Policy = PolicyNames.ManageOrders)]
    [HttpPost("{id:guid}/expire")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public Task<ActionResult<ReservationResponse>> Expire(Guid id, CancellationToken cancellationToken)
        => TransitionAsync(id, ReservationStatus.Expired, cancellationToken);

    [Authorize]
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public Task<ActionResult<ReservationResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateReservationStatusRequest request,
        CancellationToken cancellationToken) => TransitionAsync(id, request.Status, cancellationToken);

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
                CreatedAtUtc = x.CreatedAtUtc,
                ReservedUntilUtc = x.ReservedUntilUtc,
                ConfirmedAtUtc = x.ConfirmedAtUtc,
                ReadyForPickupAtUtc = x.ReadyForPickupAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                CancelledAtUtc = x.CancelledAtUtc,
                ExpiredAtUtc = x.ExpiredAtUtc,
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

    private async Task<ActionResult<ReservationResponse>> TransitionAsync(
        Guid id,
        ReservationStatus nextStatus,
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
            return ApiNotFound("reservation_not_found", "Reservation was not found.");
        }

        if (reservation.Status == nextStatus)
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

        var initialTransitionDecision = reservationTransitionPolicy.Evaluate(
            reservation.Status,
            nextStatus,
            isOwner,
            isPharmacist,
            isModerator);

        if (!initialTransitionDecision.IsAllowed)
        {
            return ProblemResponse(
                initialTransitionDecision.StatusCode,
                initialTransitionDecision.Code,
                initialTransitionDecision.Message);
        }

        var previousStatus = reservation.Status;
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
                    throw new ReservationRequestException(
                        StatusCodes.Status404NotFound,
                        "Reservation was not found.",
                        "reservation_not_found");
                }

                if (reservation.Status == nextStatus)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return;
                }

                var transitionDecision = reservationTransitionPolicy.Evaluate(
                    reservation.Status,
                    nextStatus,
                    isOwner,
                    isPharmacist,
                    isModerator);

                if (!transitionDecision.IsAllowed)
                {
                    throw new ReservationRequestException(
                        transitionDecision.StatusCode,
                        transitionDecision.Message,
                        transitionDecision.Code);
                }

                previousStatus = reservation.Status;
                reservation.Status = nextStatus;

                ApplyLifecycleTimestamps(reservation, nextStatus);

                if (nextStatus == ReservationStatus.Cancelled || nextStatus == ReservationStatus.Expired)
                {
                    await reservationStateService.ReleaseReservedStockAsync(reservation, cancellationToken);
                }

                if (nextStatus == ReservationStatus.Completed)
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
            return ProblemResponse(exception.StatusCode, exception.Code, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return ProblemResponse(StatusCodes.Status409Conflict, "reservation_stock_conflict", exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemResponse(StatusCodes.Status409Conflict, "reservation_concurrency_conflict", "Reservation or stock changed while updating status. Please refresh and try again.");
        }
        catch (DbUpdateException exception) when (IsTransientConcurrencyConflict(exception))
        {
            return ProblemResponse(StatusCodes.Status409Conflict, "reservation_concurrency_conflict", "Reservation or stock changed while updating status. Please refresh and try again.");
        }

        if (nextStatus == ReservationStatus.Completed)
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
        await reservationNotificationService.DispatchStatusNotificationAsync(
            reservation!,
            previousStatus,
            cancellationToken);
        await auditService.WriteAsync(
            action: ToAuditAction(nextStatus),
            entityName: "Reservation",
            entityId: response.ReservationId.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: response.PharmacyId,
            description: $"Reservation {response.ReservationNumber} status changed from {previousStatus} to {response.Status}.",
            metadata: new
            {
                response.ReservationId,
                response.ReservationNumber,
                PreviousStatus = previousStatus.ToString(),
                Status = response.Status.ToString()
            },
            cancellationToken: cancellationToken);

        return Ok(response);
    }

    private async Task<ActionResult?> EnsureReservationAccessAsync(
        Guid customerId,
        Guid pharmacyId,
        CancellationToken cancellationToken)
    {
        var isStaff = User.IsInRole(RoleNames.Pharmacist) || User.IsInRole(RoleNames.Moderator);
        if (!isStaff)
        {
            if (currentUserService.UserId != customerId)
            {
                return ApiForbidden("You do not have access to this reservation.");
            }

            return null;
        }

        if (User.IsInRole(RoleNames.Pharmacist) && !User.IsInRole(RoleNames.Moderator))
        {
            return await EnsurePharmacyStaffAccessAsync(pharmacyId, cancellationToken);
        }

        return null;
    }

    private async Task<ActionResult?> EnsurePharmacyStaffAccessAsync(Guid pharmacyId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(RoleNames.Moderator))
        {
            return null;
        }

        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var currentUser = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == currentUserService.UserId.Value, cancellationToken);

        if (currentUser is null || currentUser.PharmacyId != pharmacyId)
        {
            return ApiForbidden("You do not have access to reservations for this pharmacy.");
        }

        return null;
    }

    private static void ApplyLifecycleTimestamps(Reservation reservation, ReservationStatus nextStatus)
    {
        var utcNow = DateTime.UtcNow;

        switch (nextStatus)
        {
            case ReservationStatus.Confirmed:
                reservation.ConfirmedAtUtc ??= utcNow;
                break;
            case ReservationStatus.ReadyForPickup:
                reservation.ReadyForPickupAtUtc = utcNow;
                break;
            case ReservationStatus.Completed:
                reservation.CompletedAtUtc = utcNow;
                break;
            case ReservationStatus.Cancelled:
                reservation.CancelledAtUtc = utcNow;
                break;
            case ReservationStatus.Expired:
                reservation.ExpiredAtUtc = utcNow;
                break;
        }
    }

    private static string ToAuditAction(ReservationStatus nextStatus)
    {
        return nextStatus switch
        {
            ReservationStatus.Confirmed => "reservation.confirmed",
            ReservationStatus.ReadyForPickup => "reservation.ready_for_pickup",
            ReservationStatus.Completed => "reservation.completed",
            ReservationStatus.Cancelled => "reservation.cancelled",
            ReservationStatus.Expired => "reservation.expired",
            _ => "reservation.status.updated"
        };
    }

    private static string ToTimelineTitle(string action)
    {
        return action switch
        {
            "reservation.created" => "Created",
            "reservation.confirmed" => "Confirmed",
            "reservation.ready_for_pickup" => "Ready For Pickup",
            "reservation.completed" => "Completed",
            "reservation.cancelled" => "Cancelled",
            "reservation.expired" => "Expired",
            _ => "Updated"
        };
    }

    private static ReservationStatus? TryReadStatus(string action, string? metadataJson)
    {
        if (!string.IsNullOrWhiteSpace(metadataJson))
        {
            try
            {
                using var document = JsonDocument.Parse(metadataJson);
                if (document.RootElement.TryGetProperty("Status", out var statusElement) &&
                    Enum.TryParse<ReservationStatus>(statusElement.GetString(), ignoreCase: true, out var parsedStatus))
                {
                    return parsedStatus;
                }
            }
            catch (JsonException)
            {
            }
        }

        return action switch
        {
            "reservation.created" => ReservationStatus.Confirmed,
            "reservation.confirmed" => ReservationStatus.Confirmed,
            "reservation.ready_for_pickup" => ReservationStatus.ReadyForPickup,
            "reservation.completed" => ReservationStatus.Completed,
            "reservation.cancelled" => ReservationStatus.Cancelled,
            "reservation.expired" => ReservationStatus.Expired,
            _ => null
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

    private static bool IsUniqueIdempotencyViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException &&
               postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
               string.Equals(postgresException.ConstraintName, "IX_reservation_idempotency_records_UserId_IdempotencyKey", StringComparison.Ordinal);
    }

    private async Task<ActionResult<ReservationResponse>?> TryResolveIdempotentCreateAsync(
        Guid userId,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var record = await context.ReservationIdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IdempotencyKey == idempotencyKey, cancellationToken);

        if (record is null)
        {
            return null;
        }

        if (!string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return ProblemResponse(
                StatusCodes.Status409Conflict,
                "reservation_idempotency_conflict",
                "This idempotency key has already been used with a different reservation payload.");
        }

        if (!record.ReservationId.HasValue)
        {
            return ProblemResponse(
                StatusCodes.Status409Conflict,
                "reservation_idempotency_in_progress",
                "A reservation request with the same idempotency key is already being processed.");
        }

        var response = await QueryReservations()
            .Where(x => x.ReservationId == record.ReservationId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (response is null)
        {
            return null;
        }

        return Ok(response);
    }

    private ActionResult ValidationProblemResponse(string code, string detail)
    {
        var problemDetails = new ValidationProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Reservation validation failed",
            Detail = detail,
            Type = $"https://pharmago.local/problems/{code}"
        };
        problemDetails.Extensions["code"] = code;

        return BadRequest(problemDetails);
    }

    private ObjectResult ProblemResponse(int statusCode, string code, string detail)
    {
        var title = statusCode switch
        {
            StatusCodes.Status403Forbidden => "Reservation action forbidden",
            StatusCodes.Status404NotFound => "Reservation resource not found",
            StatusCodes.Status409Conflict => "Reservation request conflicted with current state",
            StatusCodes.Status422UnprocessableEntity => "Reservation business rule violated",
            _ => "Reservation request failed"
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://pharmago.local/problems/{code}"
        };
        problemDetails.Extensions["code"] = code;

        return StatusCode(statusCode, problemDetails);
    }

    private static string ComputeRequestHash(CreateReservationRequest request)
    {
        var canonicalPayload = new
        {
            request.PharmacyId,
            request.ReserveForHours,
            Notes = request.Notes?.Trim() ?? string.Empty,
            Items = request.Items
                .OrderBy(x => x.MedicineId)
                .ThenBy(x => x.Quantity)
                .Select(x => new
                {
                    x.MedicineId,
                    x.Quantity
                })
                .ToArray()
        };

        var json = JsonSerializer.Serialize(canonicalPayload);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes);
    }

    private sealed class ReservationRequestException(int statusCode, string message, string code) : Exception(message)
    {
        public int StatusCode { get; } = statusCode;
        public string Code { get; } = code;
    }
}

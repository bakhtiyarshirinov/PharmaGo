using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Reservations.Commands.CreateReservation;
using PharmaGo.Application.Reservations.Commands.UpdateReservationStatus;
using PharmaGo.Application.Reservations.Queries.GetReservation;
using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.Infrastructure.Auth;

namespace PharmaGo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController(IApplicationDbContext context, ICurrentUserService currentUserService) : ControllerBase
{
    [Authorize]
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

    [Authorize(Policy = RoleNames.StaffPolicy)]
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

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> Create(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
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

        var pharmacy = await context.Pharmacies
            .FirstOrDefaultAsync(x => x.Id == request.PharmacyId && x.IsActive, cancellationToken);

        if (pharmacy is null)
        {
            return NotFound("Pharmacy was not found.");
        }

        var requestedMedicineIds = request.Items.Select(item => item.MedicineId).Distinct().ToArray();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var stockItems = await context.StockItems
            .Include(x => x.Medicine)
            .Where(x => x.PharmacyId == request.PharmacyId &&
                requestedMedicineIds.Contains(x.MedicineId) &&
                x.IsActive &&
                x.ExpirationDate >= today &&
                x.Quantity > x.ReservedQuantity)
            .OrderBy(x => x.ExpirationDate)
            .ToListAsync(cancellationToken);

        var stockByMedicine = stockItems.GroupBy(x => x.MedicineId).ToDictionary(group => group.Key, group => group.ToList());

        foreach (var item in request.Items)
        {
            if (!stockByMedicine.TryGetValue(item.MedicineId, out var medicineStock))
            {
                return BadRequest($"Medicine '{item.MedicineId}' is not available in the selected pharmacy.");
            }

            var availableQuantity = medicineStock.Sum(stock => stock.AvailableQuantity);
            if (availableQuantity < item.Quantity)
            {
                return BadRequest($"Insufficient stock for medicine '{medicineStock[0].Medicine!.BrandName}'.");
            }
        }

        if (!currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var customer = await context.Users.FirstOrDefaultAsync(
            x => x.Id == currentUserService.UserId.Value && x.IsActive,
            cancellationToken);

        if (customer is null)
        {
            return Unauthorized();
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

        var response = new ReservationResponse
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

        return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, response);
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

        reservation.Status = request.Status;

        if (request.Status == ReservationStatus.Cancelled)
        {
            reservation.CancelledAtUtc = DateTime.UtcNow;
            await ReleaseReservationStockAsync(reservation, cancellationToken);
        }

        if (request.Status == ReservationStatus.Expired)
        {
            await ReleaseReservationStockAsync(reservation, cancellationToken);
        }

        if (request.Status == ReservationStatus.Completed)
        {
            foreach (var item in reservation.Items)
            {
                var quantityToComplete = item.Quantity;
                var stockItems = await context.StockItems
                    .Where(x => x.PharmacyId == reservation.PharmacyId &&
                        x.MedicineId == item.MedicineId &&
                        x.ReservedQuantity > 0)
                    .OrderBy(x => x.ExpirationDate)
                    .ToListAsync(cancellationToken);

                foreach (var stockItem in stockItems)
                {
                    if (quantityToComplete == 0)
                    {
                        break;
                    }

                    var quantity = Math.Min(quantityToComplete, stockItem.ReservedQuantity);
                    stockItem.ReservedQuantity -= quantity;
                    stockItem.Quantity -= quantity;
                    quantityToComplete -= quantity;
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var response = await QueryReservations()
            .Where(x => x.ReservationId == reservation.Id)
            .FirstAsync(cancellationToken);

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

    private async Task ReleaseReservationStockAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        foreach (var item in reservation.Items)
        {
            var quantityToRelease = item.Quantity;
            var stockItems = await context.StockItems
                .Where(x => x.PharmacyId == reservation.PharmacyId &&
                    x.MedicineId == item.MedicineId &&
                    x.ReservedQuantity > 0)
                .OrderBy(x => x.ExpirationDate)
                .ToListAsync(cancellationToken);

            foreach (var stockItem in stockItems)
            {
                if (quantityToRelease == 0)
                {
                    break;
                }

                var quantity = Math.Min(quantityToRelease, stockItem.ReservedQuantity);
                stockItem.ReservedQuantity -= quantity;
                quantityToRelease -= quantity;
            }
        }
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
}

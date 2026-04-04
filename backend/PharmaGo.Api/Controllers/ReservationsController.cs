using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Reservations.Commands.CreateReservation;
using PharmaGo.Application.Reservations.Queries.GetReservation;
using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController(IApplicationDbContext context) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var reservation = await context.Reservations
            .AsNoTracking()
            .Where(x => x.Id == id)
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
            })
            .FirstOrDefaultAsync(cancellationToken);

        return reservation is null ? NotFound() : Ok(reservation);
    }

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

        var customer = await context.Users.FirstOrDefaultAsync(
            x => x.PhoneNumber == request.PhoneNumber,
            cancellationToken);

        if (customer is null)
        {
            customer = new AppUser
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                Email = request.Email?.Trim(),
                TelegramUsername = request.TelegramUsername?.Trim(),
                TelegramChatId = request.TelegramChatId?.Trim(),
                Role = UserRole.Customer,
                IsActive = true
            };

            await context.Users.AddAsync(customer, cancellationToken);
        }
        else
        {
            customer.FirstName = request.FirstName.Trim();
            customer.LastName = request.LastName.Trim();
            customer.Email = request.Email?.Trim();
            customer.TelegramUsername = request.TelegramUsername?.Trim();
            customer.TelegramChatId = request.TelegramChatId?.Trim();
            customer.IsActive = true;
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
            TelegramChatId = request.TelegramChatId?.Trim()
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

    private static string GenerateReservationNumber()
    {
        return $"RES-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
    }
}

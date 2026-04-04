using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Domain.Models;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.Infrastructure.Services;

public class ReservationStateService(ApplicationDbContext context) : IReservationStateService
{
    public async Task ReleaseReservedStockAsync(Reservation reservation, CancellationToken cancellationToken = default)
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

    public async Task CompleteReservationAsync(Reservation reservation, CancellationToken cancellationToken = default)
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
}

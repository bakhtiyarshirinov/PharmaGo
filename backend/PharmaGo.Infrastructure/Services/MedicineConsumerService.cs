using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Medicines.Queries.GetConsumerMedicineFeed;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Services;

public class MedicineConsumerService(IApplicationDbContext context) : IMedicineConsumerService
{
    public async Task<IReadOnlyCollection<ConsumerMedicineFeedItemResponse>> GetPopularAsync(
        Guid? userId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 30);
        var summaries = await GetMedicineSummaryRowsAsync(cancellationToken);
        var favoriteSet = userId.HasValue
            ? await context.UserFavoriteMedicines
                .AsNoTracking()
                .Where(x => x.UserId == userId.Value)
                .Select(x => x.MedicineId)
                .ToHashSetAsync(cancellationToken)
            : [];

        var favoriteCounts = await context.UserFavoriteMedicines
            .AsNoTracking()
            .GroupBy(x => x.MedicineId)
            .Select(group => new
            {
                MedicineId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(x => x.MedicineId, x => x.Count, cancellationToken);

        var reservationCounts = await context.ReservationItems
            .AsNoTracking()
            .GroupBy(x => x.MedicineId)
            .Select(group => new
            {
                MedicineId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(x => x.MedicineId, x => x.Count, cancellationToken);

        return summaries
            .Select(summary =>
            {
                favoriteCounts.TryGetValue(summary.MedicineId, out var favorites);
                reservationCounts.TryGetValue(summary.MedicineId, out var reservations);

                var popularityScore = reservations * 5 +
                                      favorites * 3 +
                                      summary.PharmacyCount * 2 +
                                      Math.Min(summary.TotalAvailableQuantity, 100) / 10;

                return new ConsumerMedicineFeedItemResponse
                {
                    MedicineId = summary.MedicineId,
                    BrandName = summary.BrandName,
                    GenericName = summary.GenericName,
                    DosageForm = summary.DosageForm,
                    Strength = summary.Strength,
                    Manufacturer = summary.Manufacturer,
                    RequiresPrescription = summary.RequiresPrescription,
                    CategoryId = summary.CategoryId,
                    CategoryName = summary.CategoryName,
                    PharmacyCount = summary.PharmacyCount,
                    TotalAvailableQuantity = summary.TotalAvailableQuantity,
                    MinRetailPrice = summary.MinRetailPrice,
                    HasAvailability = summary.HasAvailability,
                    IsFavorite = favoriteSet.Contains(summary.MedicineId),
                    PopularityScore = popularityScore
                };
            })
            .OrderByDescending(x => x.PopularityScore)
            .ThenByDescending(x => x.PharmacyCount)
            .ThenBy(x => x.MinRetailPrice)
            .ThenBy(x => x.BrandName)
            .Take(normalizedLimit)
            .ToList();
    }

    public async Task<IReadOnlyCollection<ConsumerMedicineFeedItemResponse>> GetFavoritesAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 50);
        var summaries = await GetMedicineSummaryRowsAsync(cancellationToken);
        var summaryByMedicine = summaries.ToDictionary(x => x.MedicineId);

        var favorites = await context.UserFavoriteMedicines
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Medicine != null && x.Medicine.IsActive)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(normalizedLimit)
            .Select(x => new
            {
                x.MedicineId,
                x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return favorites
            .Where(x => summaryByMedicine.ContainsKey(x.MedicineId))
            .Select(x => MapFeedItem(summaryByMedicine[x.MedicineId], isFavorite: true, favoritedAtUtc: x.CreatedAtUtc))
            .ToList();
    }

    public async Task<IReadOnlyCollection<ConsumerMedicineFeedItemResponse>> GetRecentAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 50);
        var summaries = await GetMedicineSummaryRowsAsync(cancellationToken);
        var summaryByMedicine = summaries.ToDictionary(x => x.MedicineId);
        var favoriteSet = await context.UserFavoriteMedicines
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.MedicineId)
            .ToHashSetAsync(cancellationToken);

        var recents = await context.UserMedicineViews
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Medicine != null && x.Medicine.IsActive)
            .OrderByDescending(x => x.LastViewedAtUtc)
            .Take(normalizedLimit)
            .Select(x => new
            {
                x.MedicineId,
                x.LastViewedAtUtc
            })
            .ToListAsync(cancellationToken);

        return recents
            .Where(x => summaryByMedicine.ContainsKey(x.MedicineId))
            .Select(x => MapFeedItem(
                summaryByMedicine[x.MedicineId],
                isFavorite: favoriteSet.Contains(x.MedicineId),
                lastViewedAtUtc: x.LastViewedAtUtc))
            .ToList();
    }

    public async Task<bool> AddFavoriteAsync(
        Guid userId,
        Guid medicineId,
        CancellationToken cancellationToken = default)
    {
        var medicineExists = await context.Medicines
            .AsNoTracking()
            .AnyAsync(x => x.Id == medicineId && x.IsActive, cancellationToken);

        if (!medicineExists)
        {
            return false;
        }

        var existingFavorite = await context.UserFavoriteMedicines
            .FirstOrDefaultAsync(x => x.UserId == userId && x.MedicineId == medicineId, cancellationToken);

        if (existingFavorite is null)
        {
            await context.UserFavoriteMedicines.AddAsync(new UserFavoriteMedicine
            {
                UserId = userId,
                MedicineId = medicineId
            }, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<bool> RemoveFavoriteAsync(
        Guid userId,
        Guid medicineId,
        CancellationToken cancellationToken = default)
    {
        var medicineExists = await context.Medicines
            .AsNoTracking()
            .AnyAsync(x => x.Id == medicineId && x.IsActive, cancellationToken);

        if (!medicineExists)
        {
            return false;
        }

        var favorite = await context.UserFavoriteMedicines
            .FirstOrDefaultAsync(x => x.UserId == userId && x.MedicineId == medicineId, cancellationToken);

        if (favorite is not null)
        {
            context.UserFavoriteMedicines.Remove(favorite);
            await context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task RecordViewAsync(
        Guid userId,
        Guid medicineId,
        CancellationToken cancellationToken = default)
    {
        var medicineExists = await context.Medicines
            .AsNoTracking()
            .AnyAsync(x => x.Id == medicineId && x.IsActive, cancellationToken);

        if (!medicineExists)
        {
            return;
        }

        var view = await context.UserMedicineViews
            .FirstOrDefaultAsync(x => x.UserId == userId && x.MedicineId == medicineId, cancellationToken);

        if (view is null)
        {
            await context.UserMedicineViews.AddAsync(new UserMedicineView
            {
                UserId = userId,
                MedicineId = medicineId,
                LastViewedAtUtc = DateTime.UtcNow,
                ViewCount = 1
            }, cancellationToken);
        }
        else
        {
            view.LastViewedAtUtc = DateTime.UtcNow;
            view.ViewCount += 1;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<MedicineSummaryRow>> GetMedicineSummaryRowsAsync(CancellationToken cancellationToken)
    {
        var availabilitySummaries = await BuildAvailabilitySummaryQuery().ToListAsync(cancellationToken);
        var availabilityByMedicine = availabilitySummaries.ToDictionary(x => x.MedicineId);

        var medicines = await context.Medicines
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.BrandName,
                x.GenericName,
                x.DosageForm,
                x.Strength,
                x.Manufacturer,
                x.RequiresPrescription,
                x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null
            })
            .ToListAsync(cancellationToken);

        return medicines
            .Select(medicine =>
            {
                availabilityByMedicine.TryGetValue(medicine.Id, out var availability);

                return new MedicineSummaryRow
                {
                    MedicineId = medicine.Id,
                    BrandName = medicine.BrandName,
                    GenericName = medicine.GenericName,
                    DosageForm = medicine.DosageForm,
                    Strength = medicine.Strength,
                    Manufacturer = medicine.Manufacturer,
                    RequiresPrescription = medicine.RequiresPrescription,
                    CategoryId = medicine.CategoryId,
                    CategoryName = medicine.CategoryName,
                    PharmacyCount = availability?.PharmacyCount ?? 0,
                    TotalAvailableQuantity = availability?.TotalAvailableQuantity ?? 0,
                    MinRetailPrice = availability?.MinRetailPrice,
                    HasAvailability = availability is not null && availability.TotalAvailableQuantity > 0
                };
            })
            .ToList();
    }

    private IQueryable<AvailabilitySummaryRow> BuildAvailabilitySummaryQuery()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return context.StockItems
            .AsNoTracking()
            .Where(x => x.IsActive &&
                x.ExpirationDate >= today &&
                x.Quantity > x.ReservedQuantity &&
                x.Medicine != null &&
                x.Medicine.IsActive &&
                x.Pharmacy != null &&
                x.Pharmacy.IsActive)
            .GroupBy(x => x.MedicineId)
            .Select(group => new AvailabilitySummaryRow
            {
                MedicineId = group.Key,
                PharmacyCount = group.Select(x => x.PharmacyId).Distinct().Count(),
                TotalAvailableQuantity = group.Sum(x => x.Quantity - x.ReservedQuantity),
                MinRetailPrice = group.Select(x => (decimal?)x.RetailPrice).Min()
            });
    }

    private static ConsumerMedicineFeedItemResponse MapFeedItem(
        MedicineSummaryRow summary,
        bool isFavorite,
        DateTime? favoritedAtUtc = null,
        DateTime? lastViewedAtUtc = null,
        int? popularityScore = null)
    {
        return new ConsumerMedicineFeedItemResponse
        {
            MedicineId = summary.MedicineId,
            BrandName = summary.BrandName,
            GenericName = summary.GenericName,
            DosageForm = summary.DosageForm,
            Strength = summary.Strength,
            Manufacturer = summary.Manufacturer,
            RequiresPrescription = summary.RequiresPrescription,
            CategoryId = summary.CategoryId,
            CategoryName = summary.CategoryName,
            PharmacyCount = summary.PharmacyCount,
            TotalAvailableQuantity = summary.TotalAvailableQuantity,
            MinRetailPrice = summary.MinRetailPrice,
            HasAvailability = summary.HasAvailability,
            IsFavorite = isFavorite,
            FavoritedAtUtc = favoritedAtUtc,
            LastViewedAtUtc = lastViewedAtUtc,
            PopularityScore = popularityScore
        };
    }

    private sealed class AvailabilitySummaryRow
    {
        public Guid MedicineId { get; init; }
        public int PharmacyCount { get; init; }
        public int TotalAvailableQuantity { get; init; }
        public decimal? MinRetailPrice { get; init; }
    }

    private sealed class MedicineSummaryRow
    {
        public Guid MedicineId { get; init; }
        public string BrandName { get; init; } = string.Empty;
        public string GenericName { get; init; } = string.Empty;
        public string DosageForm { get; init; } = string.Empty;
        public string Strength { get; init; } = string.Empty;
        public string Manufacturer { get; init; } = string.Empty;
        public bool RequiresPrescription { get; init; }
        public Guid? CategoryId { get; init; }
        public string? CategoryName { get; init; }
        public int PharmacyCount { get; init; }
        public int TotalAvailableQuantity { get; init; }
        public decimal? MinRetailPrice { get; init; }
        public bool HasAvailability { get; init; }
    }
}

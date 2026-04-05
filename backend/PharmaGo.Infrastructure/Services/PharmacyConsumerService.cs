using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Pharmacies.Queries.GetConsumerPharmacyFeed;
using PharmaGo.Domain.Models;

namespace PharmaGo.Infrastructure.Services;

public class PharmacyConsumerService(IApplicationDbContext context) : IPharmacyConsumerService
{
    public async Task<IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>> GetPopularAsync(
        Guid? userId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 30);
        var summaries = await GetPharmacySummaryRowsAsync(cancellationToken);
        var favoriteSet = userId.HasValue
            ? await context.UserFavoritePharmacies
                .AsNoTracking()
                .Where(x => x.UserId == userId.Value)
                .Select(x => x.PharmacyId)
                .ToHashSetAsync(cancellationToken)
            : [];

        var favoriteCounts = await context.UserFavoritePharmacies
            .AsNoTracking()
            .GroupBy(x => x.PharmacyId)
            .Select(group => new { PharmacyId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.PharmacyId, x => x.Count, cancellationToken);

        var reservationCounts = await context.Reservations
            .AsNoTracking()
            .GroupBy(x => x.PharmacyId)
            .Select(group => new { PharmacyId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.PharmacyId, x => x.Count, cancellationToken);

        return summaries
            .Select(summary =>
            {
                favoriteCounts.TryGetValue(summary.PharmacyId, out var favorites);
                reservationCounts.TryGetValue(summary.PharmacyId, out var reservations);
                var popularityScore = reservations * 5 +
                                      favorites * 3 +
                                      summary.AvailableMedicineCount * 2 +
                                      (summary.HasDelivery ? 2 : 0) +
                                      (summary.SupportsReservations ? 1 : 0);

                return new ConsumerPharmacyFeedItemResponse
                {
                    PharmacyId = summary.PharmacyId,
                    Name = summary.Name,
                    ChainName = summary.ChainName,
                    Address = summary.Address,
                    City = summary.City,
                    Region = summary.Region,
                    PhoneNumber = summary.PhoneNumber,
                    IsOpen24Hours = summary.IsOpen24Hours,
                    SupportsReservations = summary.SupportsReservations,
                    HasDelivery = summary.HasDelivery,
                    AvailableMedicineCount = summary.AvailableMedicineCount,
                    TotalAvailableUnits = summary.TotalAvailableUnits,
                    MinAvailablePrice = summary.MinAvailablePrice,
                    IsFavorite = favoriteSet.Contains(summary.PharmacyId),
                    PopularityScore = popularityScore
                };
            })
            .OrderByDescending(x => x.PopularityScore)
            .ThenByDescending(x => x.AvailableMedicineCount)
            .ThenBy(x => x.MinAvailablePrice)
            .ThenBy(x => x.Name)
            .Take(normalizedLimit)
            .ToList();
    }

    public async Task<IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>> GetFavoritesAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 50);
        var summaries = await GetPharmacySummaryRowsAsync(cancellationToken);
        var summaryByPharmacy = summaries.ToDictionary(x => x.PharmacyId);

        var favorites = await context.UserFavoritePharmacies
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Pharmacy != null && x.Pharmacy.IsActive)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(normalizedLimit)
            .Select(x => new { x.PharmacyId, x.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        return favorites
            .Where(x => summaryByPharmacy.ContainsKey(x.PharmacyId))
            .Select(x => MapFeedItem(summaryByPharmacy[x.PharmacyId], true, favoritedAtUtc: x.CreatedAtUtc))
            .ToList();
    }

    public async Task<IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>> GetRecentAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 50);
        var summaries = await GetPharmacySummaryRowsAsync(cancellationToken);
        var summaryByPharmacy = summaries.ToDictionary(x => x.PharmacyId);
        var favoriteSet = await context.UserFavoritePharmacies
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.PharmacyId)
            .ToHashSetAsync(cancellationToken);

        var recents = await context.UserPharmacyViews
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Pharmacy != null && x.Pharmacy.IsActive)
            .OrderByDescending(x => x.LastViewedAtUtc)
            .Take(normalizedLimit)
            .Select(x => new { x.PharmacyId, x.LastViewedAtUtc })
            .ToListAsync(cancellationToken);

        return recents
            .Where(x => summaryByPharmacy.ContainsKey(x.PharmacyId))
            .Select(x => MapFeedItem(summaryByPharmacy[x.PharmacyId], favoriteSet.Contains(x.PharmacyId), lastViewedAtUtc: x.LastViewedAtUtc))
            .ToList();
    }

    public async Task<bool> AddFavoriteAsync(Guid userId, Guid pharmacyId, CancellationToken cancellationToken = default)
    {
        var pharmacyExists = await context.Pharmacies
            .AsNoTracking()
            .AnyAsync(x => x.Id == pharmacyId && x.IsActive, cancellationToken);
        if (!pharmacyExists)
        {
            return false;
        }

        var existing = await context.UserFavoritePharmacies
            .FirstOrDefaultAsync(x => x.UserId == userId && x.PharmacyId == pharmacyId, cancellationToken);

        if (existing is null)
        {
            await context.UserFavoritePharmacies.AddAsync(new UserFavoritePharmacy
            {
                UserId = userId,
                PharmacyId = pharmacyId
            }, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<bool> RemoveFavoriteAsync(Guid userId, Guid pharmacyId, CancellationToken cancellationToken = default)
    {
        var pharmacyExists = await context.Pharmacies
            .AsNoTracking()
            .AnyAsync(x => x.Id == pharmacyId && x.IsActive, cancellationToken);
        if (!pharmacyExists)
        {
            return false;
        }

        var favorite = await context.UserFavoritePharmacies
            .FirstOrDefaultAsync(x => x.UserId == userId && x.PharmacyId == pharmacyId, cancellationToken);

        if (favorite is not null)
        {
            context.UserFavoritePharmacies.Remove(favorite);
            await context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task RecordViewAsync(Guid userId, Guid pharmacyId, CancellationToken cancellationToken = default)
    {
        var pharmacyExists = await context.Pharmacies
            .AsNoTracking()
            .AnyAsync(x => x.Id == pharmacyId && x.IsActive, cancellationToken);
        if (!pharmacyExists)
        {
            return;
        }

        var view = await context.UserPharmacyViews
            .FirstOrDefaultAsync(x => x.UserId == userId && x.PharmacyId == pharmacyId, cancellationToken);

        if (view is null)
        {
            await context.UserPharmacyViews.AddAsync(new UserPharmacyView
            {
                UserId = userId,
                PharmacyId = pharmacyId,
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

    private async Task<List<PharmacySummaryRow>> GetPharmacySummaryRowsAsync(CancellationToken cancellationToken)
    {
        var summaries = await BuildAvailabilitySummaryQuery().ToListAsync(cancellationToken);
        var summaryByPharmacy = summaries.ToDictionary(x => x.PharmacyId);

        var pharmacies = await context.Pharmacies
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.Name,
                ChainName = x.PharmacyChain != null ? x.PharmacyChain.Name : null,
                x.Address,
                x.City,
                x.Region,
                x.PhoneNumber,
                x.IsOpen24Hours,
                x.SupportsReservations,
                x.HasDelivery
            })
            .ToListAsync(cancellationToken);

        return pharmacies
            .Select(pharmacy =>
            {
                summaryByPharmacy.TryGetValue(pharmacy.Id, out var summary);

                return new PharmacySummaryRow
                {
                    PharmacyId = pharmacy.Id,
                    Name = pharmacy.Name,
                    ChainName = pharmacy.ChainName,
                    Address = pharmacy.Address,
                    City = pharmacy.City,
                    Region = pharmacy.Region,
                    PhoneNumber = pharmacy.PhoneNumber,
                    IsOpen24Hours = pharmacy.IsOpen24Hours,
                    SupportsReservations = pharmacy.SupportsReservations,
                    HasDelivery = pharmacy.HasDelivery,
                    AvailableMedicineCount = summary?.AvailableMedicineCount ?? 0,
                    TotalAvailableUnits = summary?.TotalAvailableUnits ?? 0,
                    MinAvailablePrice = summary?.MinAvailablePrice
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
                x.Pharmacy != null &&
                x.Pharmacy.IsActive)
            .GroupBy(x => x.PharmacyId)
            .Select(group => new AvailabilitySummaryRow
            {
                PharmacyId = group.Key,
                AvailableMedicineCount = group.Select(x => x.MedicineId).Distinct().Count(),
                TotalAvailableUnits = group.Sum(x => x.Quantity - x.ReservedQuantity),
                MinAvailablePrice = group.Select(x => (decimal?)x.RetailPrice).Min()
            });
    }

    private static ConsumerPharmacyFeedItemResponse MapFeedItem(
        PharmacySummaryRow summary,
        bool isFavorite,
        DateTime? favoritedAtUtc = null,
        DateTime? lastViewedAtUtc = null,
        int? popularityScore = null)
    {
        return new ConsumerPharmacyFeedItemResponse
        {
            PharmacyId = summary.PharmacyId,
            Name = summary.Name,
            ChainName = summary.ChainName,
            Address = summary.Address,
            City = summary.City,
            Region = summary.Region,
            PhoneNumber = summary.PhoneNumber,
            IsOpen24Hours = summary.IsOpen24Hours,
            SupportsReservations = summary.SupportsReservations,
            HasDelivery = summary.HasDelivery,
            AvailableMedicineCount = summary.AvailableMedicineCount,
            TotalAvailableUnits = summary.TotalAvailableUnits,
            MinAvailablePrice = summary.MinAvailablePrice,
            IsFavorite = isFavorite,
            FavoritedAtUtc = favoritedAtUtc,
            LastViewedAtUtc = lastViewedAtUtc,
            PopularityScore = popularityScore
        };
    }

    private sealed class AvailabilitySummaryRow
    {
        public Guid PharmacyId { get; init; }
        public int AvailableMedicineCount { get; init; }
        public int TotalAvailableUnits { get; init; }
        public decimal? MinAvailablePrice { get; init; }
    }

    private sealed class PharmacySummaryRow
    {
        public Guid PharmacyId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? ChainName { get; init; }
        public string Address { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string? Region { get; init; }
        public string? PhoneNumber { get; init; }
        public bool IsOpen24Hours { get; init; }
        public bool SupportsReservations { get; init; }
        public bool HasDelivery { get; init; }
        public int AvailableMedicineCount { get; init; }
        public int TotalAvailableUnits { get; init; }
        public decimal? MinAvailablePrice { get; init; }
    }
}

using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Pharmacies.Queries.SearchNearbyPharmacies;

namespace PharmaGo.Infrastructure.Services;

public class PharmacyDiscoveryService(IApplicationDbContext context) : IPharmacyDiscoveryService
{
    public async Task<PagedResponse<NearbyPharmacyResponse>> SearchAsync(
        SearchNearbyPharmaciesRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var normalizedQuery = request.Query?.Trim();
        var normalizedCity = request.City?.Trim();
        var normalizedSortBy = NormalizeSortBy(request.SortBy, request.Latitude, request.Longitude);
        var normalizedSortDirection = NormalizeSortDirection(request.SortDirection);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var pharmacies = await context.Pharmacies
            .AsNoTracking()
            .Where(x => x.IsActive &&
                (string.IsNullOrWhiteSpace(normalizedCity) || x.City == normalizedCity) &&
                (!request.SupportsReservations.HasValue || x.SupportsReservations == request.SupportsReservations.Value) &&
                (!request.HasDelivery.HasValue || x.HasDelivery == request.HasDelivery.Value) &&
                (string.IsNullOrWhiteSpace(normalizedQuery) ||
                 EF.Functions.ILike(x.Name, $"%{normalizedQuery}%") ||
                 EF.Functions.ILike(x.Address, $"%{normalizedQuery}%") ||
                 EF.Functions.ILike(x.City, $"%{normalizedQuery}%") ||
                 (x.Region != null && EF.Functions.ILike(x.Region, $"%{normalizedQuery}%"))))
            .Select(x => new PharmacySearchRow
            {
                PharmacyId = x.Id,
                Name = x.Name,
                ChainName = x.PharmacyChain != null ? x.PharmacyChain.Name : null,
                Address = x.Address,
                City = x.City,
                Region = x.Region,
                PhoneNumber = x.PhoneNumber,
                LocationLatitude = x.LocationLatitude,
                LocationLongitude = x.LocationLongitude,
                IsOpen24Hours = x.IsOpen24Hours,
                OpeningHoursJson = x.OpeningHoursJson,
                SupportsReservations = x.SupportsReservations,
                HasDelivery = x.HasDelivery,
                LastLocationVerifiedAtUtc = x.LastLocationVerifiedAtUtc
            })
            .ToListAsync(cancellationToken);

        var stockSummary = await context.StockItems
            .AsNoTracking()
            .Where(x => x.IsActive &&
                x.ExpirationDate >= today &&
                x.Quantity > x.ReservedQuantity &&
                x.Pharmacy != null &&
                x.Pharmacy.IsActive)
            .GroupBy(x => x.PharmacyId)
            .Select(group => new
            {
                PharmacyId = group.Key,
                AvailableMedicineCount = group.Select(x => x.MedicineId).Distinct().Count(),
                TotalAvailableUnits = group.Sum(x => x.Quantity - x.ReservedQuantity),
                MinAvailablePrice = group.Select(x => (decimal?)x.RetailPrice).Min()
            })
            .ToListAsync(cancellationToken);

        var summaryByPharmacy = stockSummary.ToDictionary(x => x.PharmacyId);
        var utcNow = DateTime.UtcNow;

        var filtered = pharmacies
            .Select(pharmacy =>
            {
                summaryByPharmacy.TryGetValue(pharmacy.PharmacyId, out var summary);
                var distanceKm = PharmacyDiscoverySupport.CalculateDistanceKm(
                    request.Latitude,
                    request.Longitude,
                    pharmacy.LocationLatitude,
                    pharmacy.LocationLongitude);

                return new NearbyPharmacyResponse
                {
                    PharmacyId = pharmacy.PharmacyId,
                    Name = pharmacy.Name,
                    ChainName = pharmacy.ChainName,
                    Address = pharmacy.Address,
                    City = pharmacy.City,
                    Region = pharmacy.Region,
                    PhoneNumber = pharmacy.PhoneNumber,
                    LocationLatitude = pharmacy.LocationLatitude,
                    LocationLongitude = pharmacy.LocationLongitude,
                    DistanceKm = distanceKm,
                    IsOpen24Hours = pharmacy.IsOpen24Hours,
                    IsOpenNow = PharmacyDiscoverySupport.IsOpenNow(pharmacy.IsOpen24Hours, pharmacy.OpeningHoursJson, utcNow),
                    SupportsReservations = pharmacy.SupportsReservations,
                    HasDelivery = pharmacy.HasDelivery,
                    AvailableMedicineCount = summary?.AvailableMedicineCount ?? 0,
                    TotalAvailableUnits = summary?.TotalAvailableUnits ?? 0,
                    MinAvailablePrice = summary?.MinAvailablePrice,
                    LastLocationVerifiedAtUtc = pharmacy.LastLocationVerifiedAtUtc
                };
            })
            .Where(x => !request.OpenNow.HasValue || x.IsOpenNow == request.OpenNow.Value)
            .Where(x =>
                !request.Latitude.HasValue ||
                !request.Longitude.HasValue ||
                (x.DistanceKm.HasValue && x.DistanceKm.Value <= request.RadiusKm));

        var ordered = ApplySorting(filtered, normalizedSortBy, normalizedSortDirection);
        var totalCount = ordered.Count();
        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponse<NearbyPharmacyResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
            SortBy = normalizedSortBy,
            SortDirection = normalizedSortDirection
        };
    }

    private static IOrderedEnumerable<NearbyPharmacyResponse> ApplySorting(
        IEnumerable<NearbyPharmacyResponse> source,
        string sortBy,
        string sortDirection)
    {
        var descending = sortDirection == "desc";

        return (sortBy, descending) switch
        {
            ("name", true) => source.OrderByDescending(x => x.Name).ThenBy(x => x.DistanceKm ?? double.MaxValue),
            ("name", false) => source.OrderBy(x => x.Name).ThenBy(x => x.DistanceKm ?? double.MaxValue),
            (_, true) => source.OrderByDescending(x => x.DistanceKm.HasValue)
                .ThenByDescending(x => x.DistanceKm ?? double.MinValue)
                .ThenBy(x => x.Name),
            _ => source.OrderByDescending(x => x.DistanceKm.HasValue)
                .ThenBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenBy(x => x.Name)
        };
    }

    private static string NormalizeSortBy(string? value, double? latitude, double? longitude)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "name" => "name",
            "distance" when latitude.HasValue && longitude.HasValue => "distance",
            _ when latitude.HasValue && longitude.HasValue => "distance",
            _ => "name"
        };
    }

    private static string NormalizeSortDirection(string? value)
    {
        return string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
    }

    private sealed class PharmacySearchRow
    {
        public Guid PharmacyId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? ChainName { get; init; }
        public string Address { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string? Region { get; init; }
        public string? PhoneNumber { get; init; }
        public decimal? LocationLatitude { get; init; }
        public decimal? LocationLongitude { get; init; }
        public bool IsOpen24Hours { get; init; }
        public string? OpeningHoursJson { get; init; }
        public bool SupportsReservations { get; init; }
        public bool HasDelivery { get; init; }
        public DateTime? LastLocationVerifiedAtUtc { get; init; }
    }
}

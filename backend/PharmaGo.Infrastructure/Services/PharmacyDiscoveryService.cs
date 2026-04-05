using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Pharmacies.Queries.GetNearbyPharmacyMap;
using PharmaGo.Application.Pharmacies.Queries.SearchNearbyPharmacies;
using PharmaGo.Application.Pharmacies.Queries.SuggestPharmacies;

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

    public async Task<IReadOnlyCollection<PharmacySuggestionResponse>> SuggestAsync(
        string query,
        int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();
        var normalizedLimit = Math.Clamp(limit, 1, 20);
        var utcNow = DateTime.UtcNow;

        var suggestions = await context.Pharmacies
            .AsNoTracking()
            .Where(x => x.IsActive &&
                (EF.Functions.ILike(x.Name, $"%{normalizedQuery}%") ||
                 EF.Functions.ILike(x.Address, $"%{normalizedQuery}%") ||
                 EF.Functions.ILike(x.City, $"%{normalizedQuery}%") ||
                 (x.Region != null && EF.Functions.ILike(x.Region, $"%{normalizedQuery}%"))))
            .Select(x => new PharmacySuggestionRow
            {
                PharmacyId = x.Id,
                Name = x.Name,
                ChainName = x.PharmacyChain != null ? x.PharmacyChain.Name : null,
                Address = x.Address,
                City = x.City,
                IsOpen24Hours = x.IsOpen24Hours,
                OpeningHoursJson = x.OpeningHoursJson,
                SupportsReservations = x.SupportsReservations,
                HasDelivery = x.HasDelivery
            })
            .ToListAsync(cancellationToken);

        return suggestions
            .OrderBy(x => GetSuggestionRank(x.Name, x.City, normalizedQuery))
            .ThenBy(x => x.Name)
            .Take(normalizedLimit)
            .Select(x => new PharmacySuggestionResponse
            {
                PharmacyId = x.PharmacyId,
                Name = x.Name,
                ChainName = x.ChainName,
                Address = x.Address,
                City = x.City,
                IsOpen24Hours = x.IsOpen24Hours,
                SupportsReservations = x.SupportsReservations,
                HasDelivery = x.HasDelivery
            })
            .ToList();
    }

    public async Task<IReadOnlyCollection<NearbyPharmacyMapResponse>> GetMapAsync(
        GetNearbyPharmacyMapRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = request.Query?.Trim();
        var normalizedMedicineQuery = request.MedicineQuery?.Trim().ToLowerInvariant();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var utcNow = DateTime.UtcNow;
        var normalizedLimit = Math.Clamp(request.Limit, 1, 500);

        var pharmacies = await context.Pharmacies
            .AsNoTracking()
            .Where(x => x.IsActive &&
                x.LocationLatitude.HasValue &&
                x.LocationLongitude.HasValue &&
                (!request.SupportsReservations.HasValue || x.SupportsReservations == request.SupportsReservations.Value) &&
                (!request.HasDelivery.HasValue || x.HasDelivery == request.HasDelivery.Value) &&
                (string.IsNullOrWhiteSpace(normalizedQuery) ||
                 EF.Functions.ILike(x.Name, $"%{normalizedQuery}%") ||
                 EF.Functions.ILike(x.Address, $"%{normalizedQuery}%") ||
                 EF.Functions.ILike(x.City, $"%{normalizedQuery}%") ||
                 (x.Region != null && EF.Functions.ILike(x.Region, $"%{normalizedQuery}%"))))
            .Select(x => new PharmacyMapRow
            {
                PharmacyId = x.Id,
                Name = x.Name,
                ChainName = x.PharmacyChain != null ? x.PharmacyChain.Name : null,
                LocationLatitude = x.LocationLatitude!.Value,
                LocationLongitude = x.LocationLongitude!.Value,
                IsOpen24Hours = x.IsOpen24Hours,
                OpeningHoursJson = x.OpeningHoursJson,
                SupportsReservations = x.SupportsReservations,
                HasDelivery = x.HasDelivery
            })
            .ToListAsync(cancellationToken);

        var stockRows = await context.StockItems
            .AsNoTracking()
            .Where(x => x.IsActive &&
                x.ExpirationDate >= today &&
                x.Quantity > x.ReservedQuantity &&
                x.Pharmacy != null &&
                x.Pharmacy.IsActive &&
                (!request.SupportsReservations.HasValue || x.Pharmacy.SupportsReservations == request.SupportsReservations.Value) &&
                (!request.HasDelivery.HasValue || x.Pharmacy.HasDelivery == request.HasDelivery.Value) &&
                (string.IsNullOrWhiteSpace(normalizedMedicineQuery) ||
                 EF.Functions.ILike(x.Medicine!.BrandName, $"%{normalizedMedicineQuery}%") ||
                 EF.Functions.ILike(x.Medicine.GenericName, $"%{normalizedMedicineQuery}%") ||
                 (x.Medicine.Barcode != null && EF.Functions.ILike(x.Medicine.Barcode, $"%{normalizedMedicineQuery}%"))))
            .Select(x => new
            {
                x.PharmacyId,
                x.MedicineId,
                x.RetailPrice
            })
            .ToListAsync(cancellationToken);

        var stockSummaryByPharmacy = stockRows
            .GroupBy(x => x.PharmacyId)
            .ToDictionary(
                x => x.Key,
                x => new
                {
                    MatchingMedicineCount = x.Select(y => y.MedicineId).Distinct().Count(),
                    MinAvailablePrice = x.Select(y => (decimal?)y.RetailPrice).Min()
                });

        return pharmacies
            .Select(pharmacy =>
            {
                stockSummaryByPharmacy.TryGetValue(pharmacy.PharmacyId, out var stockSummary);
                var distanceKm = PharmacyDiscoverySupport.CalculateDistanceKm(
                    request.Latitude,
                    request.Longitude,
                    pharmacy.LocationLatitude,
                    pharmacy.LocationLongitude);

                return new NearbyPharmacyMapResponse
                {
                    PharmacyId = pharmacy.PharmacyId,
                    Name = pharmacy.Name,
                    ChainName = pharmacy.ChainName,
                    LocationLatitude = pharmacy.LocationLatitude,
                    LocationLongitude = pharmacy.LocationLongitude,
                    DistanceKm = distanceKm ?? double.MaxValue,
                    IsOpenNow = PharmacyDiscoverySupport.IsOpenNow(pharmacy.IsOpen24Hours, pharmacy.OpeningHoursJson, utcNow),
                    IsOpen24Hours = pharmacy.IsOpen24Hours,
                    SupportsReservations = pharmacy.SupportsReservations,
                    HasDelivery = pharmacy.HasDelivery,
                    MinAvailablePrice = stockSummary?.MinAvailablePrice,
                    MatchingMedicineCount = stockSummary?.MatchingMedicineCount ?? 0
                };
            })
            .Where(x => x.DistanceKm <= request.RadiusKm)
            .Where(x => !request.OpenNow.HasValue || x.IsOpenNow == request.OpenNow.Value)
            .Where(x => string.IsNullOrWhiteSpace(normalizedMedicineQuery) || x.MatchingMedicineCount > 0)
            .OrderBy(x => x.DistanceKm)
            .ThenBy(x => x.Name)
            .Take(normalizedLimit)
            .ToList();
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

    private static int GetSuggestionRank(string name, string city, string query)
    {
        if (name.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (city.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 3;
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

    private sealed class PharmacySuggestionRow
    {
        public Guid PharmacyId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? ChainName { get; init; }
        public string Address { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public bool IsOpen24Hours { get; init; }
        public string? OpeningHoursJson { get; init; }
        public bool SupportsReservations { get; init; }
        public bool HasDelivery { get; init; }
    }

    private sealed class PharmacyMapRow
    {
        public Guid PharmacyId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? ChainName { get; init; }
        public decimal LocationLatitude { get; init; }
        public decimal LocationLongitude { get; init; }
        public bool IsOpen24Hours { get; init; }
        public string? OpeningHoursJson { get; init; }
        public bool SupportsReservations { get; init; }
        public bool HasDelivery { get; init; }
    }
}

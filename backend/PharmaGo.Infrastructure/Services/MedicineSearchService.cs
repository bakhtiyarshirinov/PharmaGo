using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Medicines.Queries.SearchMedicines;
using PharmaGo.Infrastructure.Caching;

namespace PharmaGo.Infrastructure.Services;

public class MedicineSearchService(
    IApplicationDbContext context,
    IAppCacheService cacheService) : IMedicineSearchService
{
    public async Task<IReadOnlyCollection<MedicineSearchResponse>> SearchAsync(
        SearchMedicinesRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = request.Query.Trim().ToLowerInvariant();
        var normalizedCity = request.City?.Trim();
        var normalizedSortBy = NormalizeSortBy(request.SortBy, request.Latitude, request.Longitude);
        var limit = Math.Clamp(request.Limit, 1, 50);
        var availabilityLimit = Math.Clamp(request.AvailabilityLimit, 1, 20);
        var scopeVersion = await cacheService.GetScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);
        var cacheKey =
            $"medicines:search:v{scopeVersion}:query={normalizedQuery}:city={(normalizedCity?.ToLowerInvariant() ?? "all")}:lat={request.Latitude?.ToString("0.#######") ?? "none"}:lon={request.Longitude?.ToString("0.#######") ?? "none"}:radius={request.RadiusKm:0.##}:openNow={request.OpenNow?.ToString() ?? "all"}:reservable={request.OnlyReservable?.ToString() ?? "all"}:sort={normalizedSortBy}:limit={limit}:availabilityLimit={availabilityLimit}";
        var cached = await cacheService.GetAsync<IReadOnlyCollection<MedicineSearchResponse>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var stockRows = await context.StockItems
            .AsNoTracking()
            .Where(stock => stock.IsActive &&
                stock.ExpirationDate >= today &&
                stock.Quantity > stock.ReservedQuantity &&
                (!request.OnlyReservable.HasValue || stock.IsReservable == request.OnlyReservable.Value) &&
                stock.Medicine != null &&
                stock.Medicine.IsActive &&
                stock.Pharmacy != null &&
                stock.Pharmacy.IsActive &&
                (string.IsNullOrWhiteSpace(normalizedCity) || stock.Pharmacy.City == normalizedCity) &&
                (EF.Functions.ILike(stock.Medicine.BrandName, $"%{normalizedQuery}%") ||
                 EF.Functions.ILike(stock.Medicine.GenericName, $"%{normalizedQuery}%") ||
                 (stock.Medicine.Barcode != null && EF.Functions.ILike(stock.Medicine.Barcode, $"%{normalizedQuery}%"))))
            .Select(stock => new SearchRow
            {
                MedicineId = stock.MedicineId,
                BrandName = stock.Medicine!.BrandName,
                GenericName = stock.Medicine.GenericName,
                DosageForm = stock.Medicine.DosageForm,
                Strength = stock.Medicine.Strength,
                Manufacturer = stock.Medicine.Manufacturer,
                RequiresPrescription = stock.Medicine.RequiresPrescription,
                Barcode = stock.Medicine.Barcode,
                PharmacyId = stock.PharmacyId,
                PharmacyName = stock.Pharmacy!.Name,
                ChainName = stock.Pharmacy.PharmacyChain != null ? stock.Pharmacy.PharmacyChain.Name : null,
                Address = stock.Pharmacy.Address,
                City = stock.Pharmacy.City,
                Region = stock.Pharmacy.Region,
                PhoneNumber = stock.Pharmacy.PhoneNumber,
                LocationLatitude = stock.Pharmacy.LocationLatitude,
                LocationLongitude = stock.Pharmacy.LocationLongitude,
                IsOpen24Hours = stock.Pharmacy.IsOpen24Hours,
                OpeningHoursJson = stock.Pharmacy.OpeningHoursJson,
                SupportsReservations = stock.Pharmacy.SupportsReservations,
                HasDelivery = stock.Pharmacy.HasDelivery,
                IsReservable = stock.IsReservable,
                AvailableQuantity = stock.Quantity - stock.ReservedQuantity,
                RetailPrice = stock.RetailPrice,
                LastStockUpdatedAtUtc = stock.LastStockUpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        var filteredRows = stockRows
            .Select(row => new ProjectedSearchRow
            {
                Row = row,
                DistanceKm = PharmacyDiscoverySupport.CalculateDistanceKm(
                    request.Latitude,
                    request.Longitude,
                    row.LocationLatitude,
                    row.LocationLongitude),
                IsOpenNow = PharmacyDiscoverySupport.IsOpenNow(row.IsOpen24Hours, row.OpeningHoursJson, utcNow)
            })
            .Where(x => !request.OpenNow.HasValue || x.IsOpenNow == request.OpenNow.Value)
            .Where(x =>
                !request.Latitude.HasValue ||
                !request.Longitude.HasValue ||
                (x.DistanceKm.HasValue && x.DistanceKm.Value <= request.RadiusKm))
            .ToList();

        var responses = filteredRows
            .GroupBy(x => new
            {
                x.Row.MedicineId,
                x.Row.BrandName,
                x.Row.GenericName,
                x.Row.DosageForm,
                x.Row.Strength,
                x.Row.Manufacturer,
                x.Row.RequiresPrescription,
                x.Row.Barcode
            })
            .Select(group =>
            {
                var availabilityRows = group
                    .GroupBy(x => new
                    {
                        x.Row.PharmacyId,
                        x.Row.PharmacyName,
                        x.Row.ChainName,
                        x.Row.Address,
                        x.Row.City,
                        x.Row.Region,
                        x.Row.PhoneNumber,
                        x.Row.LocationLatitude,
                        x.Row.LocationLongitude,
                        x.Row.IsOpen24Hours,
                        x.IsOpenNow,
                        x.Row.SupportsReservations,
                        x.Row.HasDelivery,
                        x.Row.IsReservable
                    })
                    .Select(pharmacyGroup => new MedicineAvailabilityDto
                    {
                        PharmacyId = pharmacyGroup.Key.PharmacyId,
                        PharmacyName = pharmacyGroup.Key.PharmacyName,
                        ChainName = pharmacyGroup.Key.ChainName,
                        Address = pharmacyGroup.Key.Address,
                        City = pharmacyGroup.Key.City,
                        Region = pharmacyGroup.Key.Region,
                        PhoneNumber = pharmacyGroup.Key.PhoneNumber,
                        LocationLatitude = pharmacyGroup.Key.LocationLatitude,
                        LocationLongitude = pharmacyGroup.Key.LocationLongitude,
                        DistanceKm = pharmacyGroup.Min(x => x.DistanceKm),
                        IsOpen24Hours = pharmacyGroup.Key.IsOpen24Hours,
                        IsOpenNow = pharmacyGroup.Key.IsOpenNow,
                        SupportsReservations = pharmacyGroup.Key.SupportsReservations,
                        HasDelivery = pharmacyGroup.Key.HasDelivery,
                        IsReservable = pharmacyGroup.Key.IsReservable,
                        AvailableQuantity = pharmacyGroup.Sum(x => x.Row.AvailableQuantity),
                        RetailPrice = pharmacyGroup.Min(x => x.Row.RetailPrice),
                        LastStockUpdatedAtUtc = pharmacyGroup.Max(x => x.Row.LastStockUpdatedAtUtc)
                    })
                    .ToList();

                var nearestDistanceKm = availabilityRows
                    .Where(x => x.DistanceKm.HasValue)
                    .Select(x => x.DistanceKm!.Value)
                    .Cast<double?>()
                    .DefaultIfEmpty(null)
                    .Min();

                var orderedAvailabilities = OrderAvailabilities(availabilityRows, normalizedSortBy)
                    .Take(availabilityLimit)
                    .ToList();

                return new RankedMedicineSearchResponse(
                    GetMatchRank(group.Key.BrandName, group.Key.GenericName, group.Key.Barcode, normalizedQuery),
                    new MedicineSearchResponse
                    {
                        MedicineId = group.Key.MedicineId,
                        BrandName = group.Key.BrandName,
                        GenericName = group.Key.GenericName,
                        DosageForm = group.Key.DosageForm,
                        Strength = group.Key.Strength,
                        Manufacturer = group.Key.Manufacturer,
                        RequiresPrescription = group.Key.RequiresPrescription,
                        PharmacyCount = availabilityRows.Count,
                        TotalAvailableQuantity = availabilityRows.Sum(x => x.AvailableQuantity),
                        MinRetailPrice = availabilityRows.Select(x => (decimal?)x.RetailPrice).Min(),
                        NearestDistanceKm = nearestDistanceKm,
                        Availabilities = orderedAvailabilities
                    });
            })
            .Where(x => x.Response.TotalAvailableQuantity > 0)
            .ToList();

        var orderedResponses = OrderMedicines(responses, normalizedSortBy)
            .Take(limit)
            .Select(x => x.Response)
            .ToList();

        await cacheService.SetAsync(cacheKey, orderedResponses, TimeSpan.FromMinutes(5), cancellationToken);

        return orderedResponses;
    }

    public async Task<IReadOnlyCollection<MedicineSuggestionResponse>> SuggestAsync(
        string query,
        int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim().ToLowerInvariant();
        var normalizedLimit = Math.Clamp(limit, 1, 20);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var scopeVersion = await cacheService.GetScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);
        var cacheKey = $"medicines:suggest:v{scopeVersion}:query={normalizedQuery}:limit={normalizedLimit}";
        var cached = await cacheService.GetAsync<IReadOnlyCollection<MedicineSuggestionResponse>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var suggestions = await context.Medicines
            .AsNoTracking()
            .Where(medicine => medicine.IsActive &&
                (EF.Functions.ILike(medicine.BrandName, $"%{normalizedQuery}%") ||
                 EF.Functions.ILike(medicine.GenericName, $"%{normalizedQuery}%") ||
                 (medicine.Barcode != null && EF.Functions.ILike(medicine.Barcode, $"%{normalizedQuery}%"))))
            .Select(medicine => new MedicineSuggestionResponse
            {
                MedicineId = medicine.Id,
                BrandName = medicine.BrandName,
                GenericName = medicine.GenericName,
                Strength = medicine.Strength,
                DosageForm = medicine.DosageForm,
                RequiresPrescription = medicine.RequiresPrescription,
                MinRetailPrice = medicine.StockItems
                    .Where(stock => stock.IsActive &&
                        stock.ExpirationDate >= today &&
                        stock.Quantity > stock.ReservedQuantity &&
                        stock.Pharmacy != null &&
                        stock.Pharmacy.IsActive)
                    .Select(stock => (decimal?)stock.RetailPrice)
                    .Min(),
                PharmacyCount = medicine.StockItems
                    .Where(stock => stock.IsActive &&
                        stock.ExpirationDate >= today &&
                        stock.Quantity > stock.ReservedQuantity &&
                        stock.Pharmacy != null &&
                        stock.Pharmacy.IsActive)
                    .Select(stock => stock.PharmacyId)
                    .Distinct()
                    .Count()
            })
            .Where(x => x.PharmacyCount > 0)
            .ToListAsync(cancellationToken);

        suggestions = suggestions
            .OrderBy(x => GetSuggestionRank(x.BrandName, x.GenericName, normalizedQuery))
            .ThenBy(x => x.BrandName)
            .Take(normalizedLimit)
            .ToList();

        await cacheService.SetAsync(cacheKey, suggestions, TimeSpan.FromMinutes(5), cancellationToken);
        return suggestions;
    }

    private static IEnumerable<MedicineAvailabilityDto> OrderAvailabilities(
        IEnumerable<MedicineAvailabilityDto> source,
        string sortBy)
    {
        return sortBy switch
        {
            "price" => source
                .OrderBy(x => x.RetailPrice)
                .ThenByDescending(x => x.DistanceKm.HasValue)
                .ThenBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenBy(x => x.PharmacyName),
            "distance" => source
                .OrderByDescending(x => x.DistanceKm.HasValue)
                .ThenBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenBy(x => x.RetailPrice)
                .ThenBy(x => x.PharmacyName),
            _ => source
                .OrderByDescending(x => x.DistanceKm.HasValue)
                .ThenBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenBy(x => x.RetailPrice)
                .ThenBy(x => x.PharmacyName)
        };
    }

    private static IEnumerable<RankedMedicineSearchResponse> OrderMedicines(
        IEnumerable<RankedMedicineSearchResponse> source,
        string sortBy)
    {
        return sortBy switch
        {
            "price" => source
                .OrderBy(x => x.Response.MinRetailPrice ?? decimal.MaxValue)
                .ThenBy(x => x.Response.NearestDistanceKm ?? double.MaxValue)
                .ThenBy(x => x.Response.BrandName),
            "distance" => source
                .OrderBy(x => x.Response.NearestDistanceKm ?? double.MaxValue)
                .ThenBy(x => x.Response.MinRetailPrice ?? decimal.MaxValue)
                .ThenBy(x => x.Response.BrandName),
            _ => source
                .OrderBy(x => x.MatchRank)
                .ThenBy(x => x.Response.NearestDistanceKm ?? double.MaxValue)
                .ThenBy(x => x.Response.MinRetailPrice ?? decimal.MaxValue)
                .ThenBy(x => x.Response.BrandName)
        };
    }

    private static string NormalizeSortBy(string? value, double? latitude, double? longitude)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "price" => "price",
            "distance" when latitude.HasValue && longitude.HasValue => "distance",
            _ when latitude.HasValue && longitude.HasValue => "distance",
            _ => "relevance"
        };
    }

    private static int GetMatchRank(string brandName, string genericName, string? barcode, string query)
    {
        if (brandName.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (genericName.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (brandName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (genericName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (!string.IsNullOrWhiteSpace(barcode) && barcode.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        return 5;
    }

    private static int GetSuggestionRank(string brandName, string genericName, string query)
    {
        if (brandName.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (genericName.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (brandName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (genericName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return 4;
    }

    private sealed class SearchRow
    {
        public Guid MedicineId { get; init; }
        public string BrandName { get; init; } = string.Empty;
        public string GenericName { get; init; } = string.Empty;
        public string DosageForm { get; init; } = string.Empty;
        public string Strength { get; init; } = string.Empty;
        public string Manufacturer { get; init; } = string.Empty;
        public bool RequiresPrescription { get; init; }
        public string? Barcode { get; init; }
        public Guid PharmacyId { get; init; }
        public string PharmacyName { get; init; } = string.Empty;
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
        public bool IsReservable { get; init; }
        public int AvailableQuantity { get; init; }
        public decimal RetailPrice { get; init; }
        public DateTime? LastStockUpdatedAtUtc { get; init; }
    }

    private sealed class ProjectedSearchRow
    {
        public SearchRow Row { get; init; } = null!;
        public double? DistanceKm { get; init; }
        public bool IsOpenNow { get; init; }
    }

    private sealed record RankedMedicineSearchResponse(int MatchRank, MedicineSearchResponse Response);
}

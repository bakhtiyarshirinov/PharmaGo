using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Pharmacies.Queries.GetPharmacyDetail;
using PharmaGo.Application.Pharmacies.Queries.GetPharmacyMedicines;

namespace PharmaGo.Infrastructure.Services;

public class PharmacyCatalogService(IApplicationDbContext context) : IPharmacyCatalogService
{
    public async Task<PharmacyDetailResponse?> GetByIdAsync(
        Guid pharmacyId,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken = default)
    {
        var pharmacy = await context.Pharmacies
            .AsNoTracking()
            .Where(x => x.Id == pharmacyId && x.IsActive)
            .Select(x => new PharmacyRow
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
                SupportPhone = x.PharmacyChain != null ? x.PharmacyChain.SupportPhone : null,
                SupportEmail = x.PharmacyChain != null ? x.PharmacyChain.SupportEmail : null,
                LastLocationVerifiedAtUtc = x.LastLocationVerifiedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (pharmacy is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var summary = await context.StockItems
            .AsNoTracking()
            .Where(x => x.PharmacyId == pharmacyId &&
                x.IsActive &&
                x.ExpirationDate >= today &&
                x.Quantity > x.ReservedQuantity)
            .GroupBy(x => x.PharmacyId)
            .Select(group => new
            {
                AvailableMedicineCount = group.Select(x => x.MedicineId).Distinct().Count(),
                TotalAvailableUnits = group.Sum(x => x.Quantity - x.ReservedQuantity),
                MinAvailablePrice = group.Select(x => (decimal?)x.RetailPrice).Min()
            })
            .FirstOrDefaultAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        return new PharmacyDetailResponse
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
            DistanceKm = PharmacyDiscoverySupport.CalculateDistanceKm(
                latitude,
                longitude,
                pharmacy.LocationLatitude,
                pharmacy.LocationLongitude),
            IsOpen24Hours = pharmacy.IsOpen24Hours,
            IsOpenNow = PharmacyDiscoverySupport.IsOpenNow(pharmacy.IsOpen24Hours, pharmacy.OpeningHoursJson, utcNow),
            OpeningHoursJson = pharmacy.OpeningHoursJson,
            SupportsReservations = pharmacy.SupportsReservations,
            HasDelivery = pharmacy.HasDelivery,
            SupportPhone = pharmacy.SupportPhone,
            SupportEmail = pharmacy.SupportEmail,
            AvailableMedicineCount = summary?.AvailableMedicineCount ?? 0,
            TotalAvailableUnits = summary?.TotalAvailableUnits ?? 0,
            MinAvailablePrice = summary?.MinAvailablePrice,
            LastLocationVerifiedAtUtc = pharmacy.LastLocationVerifiedAtUtc
        };
    }

    public async Task<PagedResponse<PharmacyMedicineResponse>?> GetMedicinesAsync(
        Guid pharmacyId,
        GetPharmacyMedicinesRequest request,
        CancellationToken cancellationToken = default)
    {
        var pharmacyExists = await context.Pharmacies
            .AsNoTracking()
            .AnyAsync(x => x.Id == pharmacyId && x.IsActive, cancellationToken);

        if (!pharmacyExists)
        {
            return null;
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var normalizedQuery = request.Query?.Trim();
        var normalizedSortBy = NormalizeSortBy(request.SortBy);
        var normalizedSortDirection = NormalizeSortDirection(request.SortDirection);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var stockRows = await context.StockItems
            .AsNoTracking()
            .Where(x => x.PharmacyId == pharmacyId &&
                x.IsActive &&
                (!request.InStockOnly || x.Quantity > x.ReservedQuantity) &&
                x.ExpirationDate >= today &&
                (!request.OnlyReservable.HasValue || x.IsReservable == request.OnlyReservable.Value) &&
                x.Medicine != null &&
                x.Medicine.IsActive &&
                (!request.CategoryId.HasValue || x.Medicine.CategoryId == request.CategoryId.Value) &&
                (string.IsNullOrWhiteSpace(normalizedQuery) ||
                 EF.Functions.ILike(x.Medicine.BrandName, $"%{normalizedQuery}%") ||
                 EF.Functions.ILike(x.Medicine.GenericName, $"%{normalizedQuery}%") ||
                 (x.Medicine.Barcode != null && EF.Functions.ILike(x.Medicine.Barcode, $"%{normalizedQuery}%"))))
            .Select(x => new PharmacyMedicineRow
            {
                MedicineId = x.MedicineId,
                BrandName = x.Medicine!.BrandName,
                GenericName = x.Medicine.GenericName,
                DosageForm = x.Medicine.DosageForm,
                Strength = x.Medicine.Strength,
                Manufacturer = x.Medicine.Manufacturer,
                RequiresPrescription = x.Medicine.RequiresPrescription,
                CategoryId = x.Medicine.CategoryId,
                CategoryName = x.Medicine.Category != null ? x.Medicine.Category.Name : null,
                AvailableQuantity = Math.Max(0, x.Quantity - x.ReservedQuantity),
                RetailPrice = x.RetailPrice,
                IsReservable = x.IsReservable,
                LastStockUpdatedAtUtc = x.LastStockUpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var grouped = stockRows
            .GroupBy(x => new
            {
                x.MedicineId,
                x.BrandName,
                x.GenericName,
                x.DosageForm,
                x.Strength,
                x.Manufacturer,
                x.RequiresPrescription,
                x.CategoryId,
                x.CategoryName
            })
            .Select(group => new PharmacyMedicineResponse
            {
                MedicineId = group.Key.MedicineId,
                BrandName = group.Key.BrandName,
                GenericName = group.Key.GenericName,
                DosageForm = group.Key.DosageForm,
                Strength = group.Key.Strength,
                Manufacturer = group.Key.Manufacturer,
                RequiresPrescription = group.Key.RequiresPrescription,
                CategoryId = group.Key.CategoryId,
                CategoryName = group.Key.CategoryName,
                AvailableQuantity = group.Sum(x => x.AvailableQuantity),
                MinRetailPrice = group.Select(x => (decimal?)x.RetailPrice).Min(),
                IsReservable = group.Any(x => x.IsReservable),
                LastStockUpdatedAtUtc = group.Max(x => x.LastStockUpdatedAtUtc)
            });

        var ordered = ApplySorting(grouped, normalizedSortBy, normalizedSortDirection);
        var totalCount = ordered.Count();
        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponse<PharmacyMedicineResponse>
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

    private static IOrderedEnumerable<PharmacyMedicineResponse> ApplySorting(
        IEnumerable<PharmacyMedicineResponse> source,
        string sortBy,
        string sortDirection)
    {
        var descending = sortDirection == "desc";

        return (sortBy, descending) switch
        {
            ("price", true) => source.OrderByDescending(x => x.MinRetailPrice ?? decimal.MinValue).ThenBy(x => x.BrandName),
            ("price", false) => source.OrderBy(x => x.MinRetailPrice ?? decimal.MaxValue).ThenBy(x => x.BrandName),
            ("availability", true) => source.OrderByDescending(x => x.AvailableQuantity).ThenBy(x => x.BrandName),
            ("availability", false) => source.OrderBy(x => x.AvailableQuantity).ThenBy(x => x.BrandName),
            ("generic", true) => source.OrderByDescending(x => x.GenericName).ThenBy(x => x.BrandName),
            ("generic", false) => source.OrderBy(x => x.GenericName).ThenBy(x => x.BrandName),
            ("name", true) => source.OrderByDescending(x => x.BrandName).ThenBy(x => x.GenericName),
            _ => source.OrderBy(x => x.BrandName).ThenBy(x => x.GenericName)
        };
    }

    private static string NormalizeSortBy(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "price" => "price",
            "availability" => "availability",
            "generic" => "generic",
            _ => "name"
        };
    }

    private static string NormalizeSortDirection(string? value)
    {
        return string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
    }

    private sealed class PharmacyRow
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
        public string? SupportPhone { get; init; }
        public string? SupportEmail { get; init; }
        public DateTime? LastLocationVerifiedAtUtc { get; init; }
    }

    private sealed class PharmacyMedicineRow
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
        public int AvailableQuantity { get; init; }
        public decimal RetailPrice { get; init; }
        public bool IsReservable { get; init; }
        public DateTime? LastStockUpdatedAtUtc { get; init; }
    }
}

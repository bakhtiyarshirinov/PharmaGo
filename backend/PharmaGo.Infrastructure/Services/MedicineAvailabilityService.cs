using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Medicines.Queries.GetMedicineAvailability;

namespace PharmaGo.Infrastructure.Services;

public class MedicineAvailabilityService(IApplicationDbContext context) : IMedicineAvailabilityService
{
    public async Task<MedicineAvailabilityResponse?> GetAvailabilityAsync(
        GetMedicineAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedCity = request.City?.Trim();
        var normalizedSortBy = NormalizeSortBy(request.SortBy, request.Latitude, request.Longitude);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var medicine = await context.Medicines
            .AsNoTracking()
            .Where(x => x.Id == request.MedicineId && x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.BrandName,
                x.GenericName,
                x.DosageForm,
                x.Strength,
                x.Manufacturer,
                x.RequiresPrescription
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (medicine is null)
        {
            return null;
        }

        var stockRows = await context.StockItems
            .AsNoTracking()
            .Where(x => x.MedicineId == request.MedicineId &&
                x.IsActive &&
                x.ExpirationDate >= today &&
                x.Quantity > x.ReservedQuantity &&
                (!request.OnlyReservable.HasValue || x.IsReservable == request.OnlyReservable.Value) &&
                x.Pharmacy != null &&
                x.Pharmacy.IsActive &&
                (string.IsNullOrWhiteSpace(normalizedCity) || x.Pharmacy.City == normalizedCity))
            .Select(x => new StockAvailabilityRow
            {
                PharmacyId = x.PharmacyId,
                PharmacyName = x.Pharmacy!.Name,
                ChainName = x.Pharmacy.PharmacyChain != null ? x.Pharmacy.PharmacyChain.Name : null,
                Address = x.Pharmacy.Address,
                City = x.Pharmacy.City,
                Region = x.Pharmacy.Region,
                PhoneNumber = x.Pharmacy.PhoneNumber,
                LocationLatitude = x.Pharmacy.LocationLatitude,
                LocationLongitude = x.Pharmacy.LocationLongitude,
                IsOpen24Hours = x.Pharmacy.IsOpen24Hours,
                OpeningHoursJson = x.Pharmacy.OpeningHoursJson,
                SupportsReservations = x.Pharmacy.SupportsReservations,
                HasDelivery = x.Pharmacy.HasDelivery,
                AvailableQuantity = x.Quantity - x.ReservedQuantity,
                RetailPrice = x.RetailPrice,
                LastStockUpdatedAtUtc = x.LastStockUpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        var availabilities = stockRows
            .GroupBy(x => new
            {
                x.PharmacyId,
                x.PharmacyName,
                x.ChainName,
                x.Address,
                x.City,
                x.Region,
                x.PhoneNumber,
                x.LocationLatitude,
                x.LocationLongitude,
                x.IsOpen24Hours,
                x.OpeningHoursJson,
                x.SupportsReservations,
                x.HasDelivery
            })
            .Select(group =>
            {
                var distanceKm = PharmacyDiscoverySupport.CalculateDistanceKm(
                    request.Latitude,
                    request.Longitude,
                    group.Key.LocationLatitude,
                    group.Key.LocationLongitude);

                return new MedicineAvailabilityPharmacyResponse
                {
                    PharmacyId = group.Key.PharmacyId,
                    PharmacyName = group.Key.PharmacyName,
                    ChainName = group.Key.ChainName,
                    Address = group.Key.Address,
                    City = group.Key.City,
                    Region = group.Key.Region,
                    PhoneNumber = group.Key.PhoneNumber,
                    LocationLatitude = group.Key.LocationLatitude,
                    LocationLongitude = group.Key.LocationLongitude,
                    DistanceKm = distanceKm,
                    IsOpen24Hours = group.Key.IsOpen24Hours,
                    IsOpenNow = PharmacyDiscoverySupport.IsOpenNow(group.Key.IsOpen24Hours, group.Key.OpeningHoursJson, utcNow),
                    SupportsReservations = group.Key.SupportsReservations,
                    HasDelivery = group.Key.HasDelivery,
                    AvailableQuantity = group.Sum(x => x.AvailableQuantity),
                    RetailPrice = group.Min(x => x.RetailPrice),
                    LastStockUpdatedAtUtc = group.Max(x => x.LastStockUpdatedAtUtc)
                };
            })
            .Where(x => !request.OpenNow.HasValue || x.IsOpenNow == request.OpenNow.Value)
            .Where(x =>
                !request.Latitude.HasValue ||
                !request.Longitude.HasValue ||
                (x.DistanceKm.HasValue && x.DistanceKm.Value <= request.RadiusKm))
            .ToList();

        var orderedAvailabilities = ApplySorting(availabilities, normalizedSortBy).ToList();

        return new MedicineAvailabilityResponse
        {
            MedicineId = medicine.Id,
            BrandName = medicine.BrandName,
            GenericName = medicine.GenericName,
            DosageForm = medicine.DosageForm,
            Strength = medicine.Strength,
            Manufacturer = medicine.Manufacturer,
            RequiresPrescription = medicine.RequiresPrescription,
            PharmacyCount = orderedAvailabilities.Count,
            TotalAvailableQuantity = orderedAvailabilities.Sum(x => x.AvailableQuantity),
            MinRetailPrice = orderedAvailabilities.Select(x => (decimal?)x.RetailPrice).Min(),
            Availabilities = orderedAvailabilities
        };
    }

    private static IEnumerable<MedicineAvailabilityPharmacyResponse> ApplySorting(
        IEnumerable<MedicineAvailabilityPharmacyResponse> source,
        string sortBy)
    {
        return sortBy switch
        {
            "price" => source
                .OrderBy(x => x.RetailPrice)
                .ThenByDescending(x => x.DistanceKm.HasValue)
                .ThenBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenBy(x => x.PharmacyName),
            _ => source
                .OrderByDescending(x => x.DistanceKm.HasValue)
                .ThenBy(x => x.DistanceKm ?? double.MaxValue)
                .ThenBy(x => x.RetailPrice)
                .ThenBy(x => x.PharmacyName)
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
            _ => "price"
        };
    }

    private sealed class StockAvailabilityRow
    {
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
        public int AvailableQuantity { get; init; }
        public decimal RetailPrice { get; init; }
        public DateTime? LastStockUpdatedAtUtc { get; init; }
    }
}

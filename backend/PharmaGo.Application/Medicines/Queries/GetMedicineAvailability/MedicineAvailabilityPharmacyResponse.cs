namespace PharmaGo.Application.Medicines.Queries.GetMedicineAvailability;

public class MedicineAvailabilityPharmacyResponse
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
    public double? DistanceKm { get; init; }
    public bool IsOpen24Hours { get; init; }
    public bool IsOpenNow { get; init; }
    public bool SupportsReservations { get; init; }
    public bool HasDelivery { get; init; }
    public int AvailableQuantity { get; init; }
    public decimal RetailPrice { get; init; }
    public DateTime? LastStockUpdatedAtUtc { get; init; }
}

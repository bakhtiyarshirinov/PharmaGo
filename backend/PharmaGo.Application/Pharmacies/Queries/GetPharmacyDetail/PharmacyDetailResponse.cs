namespace PharmaGo.Application.Pharmacies.Queries.GetPharmacyDetail;

public class PharmacyDetailResponse
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
    public double? DistanceKm { get; init; }
    public bool IsOpen24Hours { get; init; }
    public bool IsOpenNow { get; init; }
    public string? OpeningHoursJson { get; init; }
    public bool SupportsReservations { get; init; }
    public bool HasDelivery { get; init; }
    public string? SupportPhone { get; init; }
    public string? SupportEmail { get; init; }
    public int AvailableMedicineCount { get; init; }
    public int TotalAvailableUnits { get; init; }
    public decimal? MinAvailablePrice { get; init; }
    public DateTime? LastLocationVerifiedAtUtc { get; init; }
}

namespace PharmaGo.Application.Pharmacies.Contracts;

public class ManagedPharmacyResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
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
    public bool IsActive { get; init; }
    public Guid? PharmacyChainId { get; init; }
    public string? PharmacyChainName { get; init; }
    public int EmployeeCount { get; init; }
    public int ActiveStockItemCount { get; init; }
    public int ActiveReservationCount { get; init; }
    public DateTime? LastLocationVerifiedAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}

namespace PharmaGo.Application.Pharmacies.Queries.GetNearbyPharmacyMap;

public class NearbyPharmacyMapResponse
{
    public Guid PharmacyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ChainName { get; init; }
    public decimal LocationLatitude { get; init; }
    public decimal LocationLongitude { get; init; }
    public double DistanceKm { get; init; }
    public bool IsOpenNow { get; init; }
    public bool IsOpen24Hours { get; init; }
    public bool SupportsReservations { get; init; }
    public bool HasDelivery { get; init; }
    public decimal? MinAvailablePrice { get; init; }
    public int MatchingMedicineCount { get; init; }
}

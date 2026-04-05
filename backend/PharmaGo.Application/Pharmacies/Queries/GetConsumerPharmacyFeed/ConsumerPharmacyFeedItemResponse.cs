namespace PharmaGo.Application.Pharmacies.Queries.GetConsumerPharmacyFeed;

public class ConsumerPharmacyFeedItemResponse
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
    public bool IsFavorite { get; init; }
    public DateTime? FavoritedAtUtc { get; init; }
    public DateTime? LastViewedAtUtc { get; init; }
    public int? PopularityScore { get; init; }
}

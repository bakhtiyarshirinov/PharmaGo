namespace PharmaGo.Application.Medicines.Queries.GetConsumerMedicineFeed;

public class ConsumerMedicineFeedItemResponse
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
    public int PharmacyCount { get; init; }
    public int TotalAvailableQuantity { get; init; }
    public decimal? MinRetailPrice { get; init; }
    public bool HasAvailability { get; init; }
    public bool IsFavorite { get; init; }
    public DateTime? FavoritedAtUtc { get; init; }
    public DateTime? LastViewedAtUtc { get; init; }
    public int? PopularityScore { get; init; }
}

namespace PharmaGo.Application.Medicines.Queries.GetMedicineRecommendations;

public class MedicineRecommendationResponse
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
    public string MatchReason { get; init; } = string.Empty;
}

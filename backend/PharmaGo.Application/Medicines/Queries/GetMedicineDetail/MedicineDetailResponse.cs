namespace PharmaGo.Application.Medicines.Queries.GetMedicineDetail;

public class MedicineDetailResponse
{
    public Guid MedicineId { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string DosageForm { get; init; } = string.Empty;
    public string Strength { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string? CountryOfOrigin { get; init; }
    public string? Barcode { get; init; }
    public bool RequiresPrescription { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public int PharmacyCount { get; init; }
    public int TotalAvailableQuantity { get; init; }
    public decimal? MinRetailPrice { get; init; }
    public bool HasAvailability { get; init; }
}

namespace PharmaGo.Application.Pharmacies.Queries.GetPharmacyMedicines;

public class PharmacyMedicineResponse
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
    public decimal? MinRetailPrice { get; init; }
    public bool IsReservable { get; init; }
    public DateTime? LastStockUpdatedAtUtc { get; init; }
}

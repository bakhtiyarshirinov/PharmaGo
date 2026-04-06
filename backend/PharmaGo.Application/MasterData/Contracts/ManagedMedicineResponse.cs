namespace PharmaGo.Application.MasterData.Contracts;

public class ManagedMedicineResponse
{
    public Guid Id { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string DosageForm { get; init; } = string.Empty;
    public string Strength { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string? CountryOfOrigin { get; init; }
    public string? Barcode { get; init; }
    public bool RequiresPrescription { get; init; }
    public bool IsActive { get; init; }
    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public int StockBatchCount { get; init; }
    public int SupplierOfferCount { get; init; }
}

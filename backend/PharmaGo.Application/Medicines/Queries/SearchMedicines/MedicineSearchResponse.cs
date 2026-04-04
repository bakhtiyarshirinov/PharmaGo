namespace PharmaGo.Application.Medicines.Queries.SearchMedicines;

public class MedicineSearchResponse
{
    public Guid MedicineId { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public string DosageForm { get; init; } = string.Empty;
    public string Strength { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public bool RequiresPrescription { get; init; }
    public int TotalAvailableQuantity { get; init; }
    public decimal? MinRetailPrice { get; init; }
    public IReadOnlyCollection<MedicineAvailabilityDto> Availabilities { get; init; } = Array.Empty<MedicineAvailabilityDto>();
}

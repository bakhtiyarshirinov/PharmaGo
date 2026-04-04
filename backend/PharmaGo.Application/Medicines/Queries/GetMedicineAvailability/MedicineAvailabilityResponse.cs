namespace PharmaGo.Application.Medicines.Queries.GetMedicineAvailability;

public class MedicineAvailabilityResponse
{
    public Guid MedicineId { get; init; }
    public string BrandName { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public string DosageForm { get; init; } = string.Empty;
    public string Strength { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public bool RequiresPrescription { get; init; }
    public int PharmacyCount { get; init; }
    public int TotalAvailableQuantity { get; init; }
    public decimal? MinRetailPrice { get; init; }
    public IReadOnlyCollection<MedicineAvailabilityPharmacyResponse> Availabilities { get; init; } =
        Array.Empty<MedicineAvailabilityPharmacyResponse>();
}

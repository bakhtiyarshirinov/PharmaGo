namespace PharmaGo.Application.Medicines.Queries.SearchMedicines;

public class MedicineAvailabilityDto
{
    public Guid PharmacyId { get; init; }
    public string PharmacyName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public bool IsOpen24Hours { get; init; }
    public int AvailableQuantity { get; init; }
    public decimal RetailPrice { get; init; }
}

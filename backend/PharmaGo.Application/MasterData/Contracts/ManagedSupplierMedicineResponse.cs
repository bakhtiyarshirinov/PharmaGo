namespace PharmaGo.Application.MasterData.Contracts;

public class ManagedSupplierMedicineResponse
{
    public Guid Id { get; init; }
    public Guid DepotId { get; init; }
    public string DepotName { get; init; } = string.Empty;
    public Guid MedicineId { get; init; }
    public string MedicineName { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public decimal WholesalePrice { get; init; }
    public int AvailableQuantity { get; init; }
    public int MinimumOrderQuantity { get; init; }
    public int EstimatedDeliveryHours { get; init; }
    public bool IsAvailable { get; init; }
}

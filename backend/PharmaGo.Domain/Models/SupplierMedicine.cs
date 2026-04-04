namespace PharmaGo.Domain.Models;

public class SupplierMedicine : BaseEntity
{
    public Guid DepotId { get; set; }
    public Depot? Depot { get; set; }

    public Guid MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    public decimal WholesalePrice { get; set; }
    public int AvailableQuantity { get; set; }
    public int MinimumOrderQuantity { get; set; }
    public int EstimatedDeliveryHours { get; set; }
    public bool IsAvailable { get; set; } = true;
}

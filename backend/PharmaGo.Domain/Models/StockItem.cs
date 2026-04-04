namespace PharmaGo.Domain.Models;

public class StockItem : BaseEntity
{
    public Guid PharmacyId { get; set; }
    public Pharmacy? Pharmacy { get; set; }

    public Guid MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly ExpirationDate { get; set; }
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal RetailPrice { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;

    public int AvailableQuantity => Math.Max(0, Quantity - ReservedQuantity);
    public bool IsLowStock => AvailableQuantity <= ReorderLevel;
    public bool IsExpired => ExpirationDate < DateOnly.FromDateTime(DateTime.UtcNow);
}

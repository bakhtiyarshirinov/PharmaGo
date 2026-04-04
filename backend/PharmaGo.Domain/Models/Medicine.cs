namespace PharmaGo.Domain.Models;

public class Medicine : BaseEntity
{
    public string BrandName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DosageForm { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string? CountryOfOrigin { get; set; }
    public string? Barcode { get; set; }
    public bool RequiresPrescription { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid? CategoryId { get; set; }
    public MedicineCategory? Category { get; set; }

    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public ICollection<ReservationItem> ReservationItems { get; set; } = new List<ReservationItem>();
    public ICollection<SupplierMedicine> SupplierMedicines { get; set; } = new List<SupplierMedicine>();
}

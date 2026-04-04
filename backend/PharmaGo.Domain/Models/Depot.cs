namespace PharmaGo.Domain.Models;

public class Depot : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SupplierMedicine> SupplierMedicines { get; set; } = new List<SupplierMedicine>();
}

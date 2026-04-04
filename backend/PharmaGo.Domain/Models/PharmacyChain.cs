namespace PharmaGo.Domain.Models;

public class PharmacyChain : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TaxNumber { get; set; }
    public string? SupportPhone { get; set; }
    public string? SupportEmail { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Pharmacy> Pharmacies { get; set; } = new List<Pharmacy>();
}

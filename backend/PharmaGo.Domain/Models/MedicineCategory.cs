namespace PharmaGo.Domain.Models;

public class MedicineCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
}

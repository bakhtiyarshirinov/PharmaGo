namespace PharmaGo.Domain.Models;

public class UserMedicineView : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    public DateTime LastViewedAtUtc { get; set; }
    public int ViewCount { get; set; }
}

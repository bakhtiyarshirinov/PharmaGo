namespace PharmaGo.Domain.Models;

public class UserPharmacyView : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid PharmacyId { get; set; }
    public Pharmacy? Pharmacy { get; set; }

    public DateTime LastViewedAtUtc { get; set; }
    public int ViewCount { get; set; }
}

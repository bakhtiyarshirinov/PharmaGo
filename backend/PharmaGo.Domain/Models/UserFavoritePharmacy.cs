namespace PharmaGo.Domain.Models;

public class UserFavoritePharmacy : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid PharmacyId { get; set; }
    public Pharmacy? Pharmacy { get; set; }
}

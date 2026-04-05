namespace PharmaGo.Domain.Models;

public class UserFavoriteMedicine : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid MedicineId { get; set; }
    public Medicine? Medicine { get; set; }
}

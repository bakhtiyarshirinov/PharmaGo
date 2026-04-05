using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Domain.Models;

public class AppUser : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? TelegramUsername { get; set; }
    public string? TelegramChatId { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;

    public Guid? PharmacyId { get; set; }
    public Pharmacy? Pharmacy { get; set; }

    public ICollection<UserFavoriteMedicine> FavoriteMedicines { get; set; } = new List<UserFavoriteMedicine>();
    public ICollection<UserFavoritePharmacy> FavoritePharmacies { get; set; } = new List<UserFavoritePharmacy>();
    public ICollection<UserMedicineView> MedicineViews { get; set; } = new List<UserMedicineView>();
    public ICollection<UserPharmacyView> PharmacyViews { get; set; } = new List<UserPharmacyView>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

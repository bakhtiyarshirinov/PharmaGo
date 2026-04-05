namespace PharmaGo.Domain.Models;

public class Pharmacy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public decimal? LocationLatitude { get; set; }
    public decimal? LocationLongitude { get; set; }
    public string? OpeningHoursJson { get; set; }
    public bool IsOpen24Hours { get; set; }
    public bool SupportsReservations { get; set; } = true;
    public bool HasDelivery { get; set; }
    public DateTime? LastLocationVerifiedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid? PharmacyChainId { get; set; }
    public PharmacyChain? PharmacyChain { get; set; }

    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<AppUser> Employees { get; set; } = new List<AppUser>();
    public ICollection<UserFavoritePharmacy> FavoritedByUsers { get; set; } = new List<UserFavoritePharmacy>();
    public ICollection<UserPharmacyView> ViewedByUsers { get; set; } = new List<UserPharmacyView>();
}

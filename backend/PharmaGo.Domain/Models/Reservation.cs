using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Domain.Models;

public class Reservation : BaseEntity
{
    public string ReservationNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public AppUser? Customer { get; set; }

    public Guid PharmacyId { get; set; }
    public Pharmacy? Pharmacy { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public string? Notes { get; set; }
    public DateTime ReservedUntilUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public string? TelegramChatId { get; set; }

    public ICollection<ReservationItem> Items { get; set; } = new List<ReservationItem>();
}

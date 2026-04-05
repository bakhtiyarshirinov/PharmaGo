namespace PharmaGo.Domain.Models;

public class NotificationPreference : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public bool InAppEnabled { get; set; } = true;
    public bool TelegramEnabled { get; set; }

    public bool ReservationConfirmedEnabled { get; set; } = true;
    public bool ReservationReadyEnabled { get; set; } = true;
    public bool ReservationCancelledEnabled { get; set; } = true;
    public bool ReservationExpiredEnabled { get; set; } = true;
    public bool ReservationExpiringSoonEnabled { get; set; } = true;
}

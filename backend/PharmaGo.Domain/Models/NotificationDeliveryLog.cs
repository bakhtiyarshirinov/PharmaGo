using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Domain.Models;

public class NotificationDeliveryLog : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public NotificationEventType EventType { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationDeliveryStatus Status { get; set; }

    public Guid? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}

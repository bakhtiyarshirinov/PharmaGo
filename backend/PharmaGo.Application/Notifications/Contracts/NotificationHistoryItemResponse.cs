using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Application.Notifications.Contracts;

public class NotificationHistoryItemResponse
{
    public Guid NotificationId { get; init; }
    public NotificationEventType EventType { get; init; }
    public NotificationChannel Channel { get; init; }
    public NotificationDeliveryStatus Status { get; init; }
    public Guid? ReservationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? DeliveredAtUtc { get; init; }
    public DateTime? ReadAtUtc { get; init; }
    public bool IsRead => ReadAtUtc.HasValue;
}

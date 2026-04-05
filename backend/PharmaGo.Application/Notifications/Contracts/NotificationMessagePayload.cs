using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Application.Notifications.Contracts;

public class NotificationMessagePayload
{
    public NotificationEventType EventType { get; init; }
    public Guid UserId { get; init; }
    public Guid? ReservationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public object? Data { get; init; }
}

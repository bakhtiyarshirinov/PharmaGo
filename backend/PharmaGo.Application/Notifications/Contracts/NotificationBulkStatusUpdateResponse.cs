namespace PharmaGo.Application.Notifications.Contracts;

public class NotificationBulkStatusUpdateResponse
{
    public int RequestedCount { get; init; }
    public int UpdatedCount { get; init; }
    public int UnreadCount { get; init; }
}

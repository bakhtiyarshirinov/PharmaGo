namespace PharmaGo.Application.Notifications.Contracts;

public class NotificationInboxSummaryResponse
{
    public int UnreadCount { get; init; }
    public NotificationHistoryItemResponse? LatestUnread { get; init; }
    public IReadOnlyCollection<NotificationHistoryItemResponse> PreviewItems { get; init; } = Array.Empty<NotificationHistoryItemResponse>();
}

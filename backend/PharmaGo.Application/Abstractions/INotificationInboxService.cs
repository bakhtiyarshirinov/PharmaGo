using PharmaGo.Application.Notifications.Contracts;

namespace PharmaGo.Application.Abstractions;

public interface INotificationInboxService
{
    Task<IReadOnlyCollection<NotificationHistoryItemResponse>> GetHistoryAsync(Guid userId, int limit, CancellationToken cancellationToken = default);
    Task<NotificationUnreadCountResponse> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
}

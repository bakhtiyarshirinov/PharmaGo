using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Notifications.Contracts;

namespace PharmaGo.Application.Abstractions;

public interface INotificationInboxService
{
    Task<PagedResponse<NotificationHistoryItemResponse>> GetHistoryAsync(Guid userId, GetNotificationHistoryRequest request, CancellationToken cancellationToken = default);
    Task<NotificationInboxSummaryResponse> GetUnreadAsync(Guid userId, int previewLimit, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<bool> MarkAsUnreadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsUnreadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationBulkStatusUpdateResponse> BulkUpdateStatusAsync(Guid userId, NotificationBulkStatusUpdateRequest request, CancellationToken cancellationToken = default);
}

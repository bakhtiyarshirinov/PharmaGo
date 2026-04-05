using PharmaGo.Application.Notifications.Contracts;

namespace PharmaGo.Application.Abstractions;

public interface INotificationPreferenceService
{
    Task<NotificationPreferencesResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationPreferencesResponse> UpdateAsync(Guid userId, UpdateNotificationPreferencesRequest request, CancellationToken cancellationToken = default);
}

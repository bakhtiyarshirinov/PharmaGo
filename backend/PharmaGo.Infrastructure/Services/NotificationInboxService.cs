using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Notifications.Contracts;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.Infrastructure.Services;

public class NotificationInboxService(ApplicationDbContext context) : INotificationInboxService
{
    public async Task<IReadOnlyCollection<NotificationHistoryItemResponse>> GetHistoryAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 100);

        return await context.NotificationDeliveryLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(safeLimit)
            .Select(x => new NotificationHistoryItemResponse
            {
                NotificationId = x.Id,
                EventType = x.EventType,
                Channel = x.Channel,
                Status = x.Status,
                ReservationId = x.ReservationId,
                Title = x.Title,
                Message = x.Message,
                PayloadJson = x.PayloadJson,
                ErrorMessage = x.ErrorMessage,
                CreatedAtUtc = x.CreatedAtUtc,
                DeliveredAtUtc = x.DeliveredAtUtc,
                ReadAtUtc = x.ReadAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationUnreadCountResponse> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = await context.NotificationDeliveryLogs
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId && x.Status == Domain.Models.Enums.NotificationDeliveryStatus.Sent && x.ReadAtUtc == null, cancellationToken);

        return new NotificationUnreadCountResponse
        {
            UnreadCount = count
        };
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await context.NotificationDeliveryLogs
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, cancellationToken);

        if (notification is null)
        {
            return false;
        }

        if (!notification.ReadAtUtc.HasValue)
        {
            notification.ReadAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await context.NotificationDeliveryLogs
            .Where(x => x.UserId == userId && x.Status == Domain.Models.Enums.NotificationDeliveryStatus.Sent && x.ReadAtUtc == null)
            .ToListAsync(cancellationToken);

        if (notifications.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in notifications)
        {
            notification.ReadAtUtc = now;
        }

        await context.SaveChangesAsync(cancellationToken);
        return notifications.Count;
    }
}

using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Notifications.Contracts;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.Infrastructure.Services;

public class NotificationInboxService(ApplicationDbContext context) : INotificationInboxService
{
    public async Task<PagedResponse<NotificationHistoryItemResponse>> GetHistoryAsync(
        Guid userId,
        GetNotificationHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var normalizedSortBy = NormalizeSortBy(request.SortBy);
        var normalizedSortDirection = NormalizeSortDirection(request.SortDirection);

        var query = context.NotificationDeliveryLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => !request.UnreadOnly.HasValue || request.UnreadOnly.Value == false || (x.Status == NotificationDeliveryStatus.Sent && x.ReadAtUtc == null))
            .Where(x => !request.EventType.HasValue || x.EventType == request.EventType.Value)
            .Where(x => !request.Status.HasValue || x.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, normalizedSortBy, normalizedSortDirection);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotificationHistoryItemResponse
            {
                NotificationId = x.Id,
                EventType = x.EventType,
                Channel = x.Channel,
                Status = x.Status,
                ReservationId = x.ReservationId,
                Title = x.Title,
                Message = x.Message,
                CreatedAtUtc = x.CreatedAtUtc,
                DeliveredAtUtc = x.DeliveredAtUtc,
                ReadAtUtc = x.ReadAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<NotificationHistoryItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            SortBy = normalizedSortBy,
            SortDirection = normalizedSortDirection
        };
    }

    public async Task<NotificationInboxSummaryResponse> GetUnreadAsync(Guid userId, int previewLimit, CancellationToken cancellationToken = default)
    {
        var unreadQuery = context.NotificationDeliveryLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == NotificationDeliveryStatus.Sent && x.ReadAtUtc == null);

        var safePreviewLimit = Math.Clamp(previewLimit, 1, 20);
        var unreadCount = await unreadQuery.CountAsync(cancellationToken);
        var previewItems = await unreadQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(safePreviewLimit)
            .Select(x => new NotificationHistoryItemResponse
            {
                NotificationId = x.Id,
                EventType = x.EventType,
                Channel = x.Channel,
                Status = x.Status,
                ReservationId = x.ReservationId,
                Title = x.Title,
                Message = x.Message,
                CreatedAtUtc = x.CreatedAtUtc,
                DeliveredAtUtc = x.DeliveredAtUtc,
                ReadAtUtc = x.ReadAtUtc
            })
            .ToListAsync(cancellationToken);

        return new NotificationInboxSummaryResponse
        {
            UnreadCount = unreadCount,
            LatestUnread = previewItems.FirstOrDefault(),
            PreviewItems = previewItems
        };
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await context.NotificationDeliveryLogs
            .FirstOrDefaultAsync(
                x => x.Id == notificationId &&
                    x.UserId == userId &&
                    x.Status == NotificationDeliveryStatus.Sent,
                cancellationToken);

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

    public async Task<bool> MarkAsUnreadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await context.NotificationDeliveryLogs
            .FirstOrDefaultAsync(
                x => x.Id == notificationId &&
                    x.UserId == userId &&
                    x.Status == NotificationDeliveryStatus.Sent,
                cancellationToken);

        if (notification is null)
        {
            return false;
        }

        if (notification.ReadAtUtc.HasValue)
        {
            notification.ReadAtUtc = null;
            await context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await context.NotificationDeliveryLogs
            .Where(x => x.UserId == userId && x.Status == NotificationDeliveryStatus.Sent && x.ReadAtUtc == null)
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

    public async Task<int> MarkAllAsUnreadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await context.NotificationDeliveryLogs
            .Where(x => x.UserId == userId && x.Status == NotificationDeliveryStatus.Sent && x.ReadAtUtc != null)
            .ToListAsync(cancellationToken);

        if (notifications.Count == 0)
        {
            return 0;
        }

        foreach (var notification in notifications)
        {
            notification.ReadAtUtc = null;
        }

        await context.SaveChangesAsync(cancellationToken);
        return notifications.Count;
    }

    public async Task<NotificationBulkStatusUpdateResponse> BulkUpdateStatusAsync(
        Guid userId,
        NotificationBulkStatusUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var ids = request.NotificationIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new NotificationBulkStatusUpdateResponse
            {
                RequestedCount = 0,
                UpdatedCount = 0,
                UnreadCount = await CountUnreadAsync(userId, cancellationToken)
            };
        }

        var notifications = await context.NotificationDeliveryLogs
            .Where(x => x.UserId == userId && x.Status == NotificationDeliveryStatus.Sent && ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var updatedCount = 0;
        var now = DateTime.UtcNow;

        foreach (var notification in notifications)
        {
            if (request.MarkAsRead)
            {
                if (!notification.ReadAtUtc.HasValue)
                {
                    notification.ReadAtUtc = now;
                    updatedCount++;
                }
            }
            else if (notification.ReadAtUtc.HasValue)
            {
                notification.ReadAtUtc = null;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return new NotificationBulkStatusUpdateResponse
        {
            RequestedCount = ids.Length,
            UpdatedCount = updatedCount,
            UnreadCount = await CountUnreadAsync(userId, cancellationToken)
        };
    }

    private async Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context.NotificationDeliveryLogs
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId && x.Status == NotificationDeliveryStatus.Sent && x.ReadAtUtc == null, cancellationToken);
    }

    private static IQueryable<Domain.Models.NotificationDeliveryLog> ApplySorting(
        IQueryable<Domain.Models.NotificationDeliveryLog> query,
        string sortBy,
        string sortDirection)
    {
        var descending = sortDirection == "desc";

        return sortBy switch
        {
            "eventType" => descending ? query.OrderByDescending(x => x.EventType).ThenByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.EventType).ThenByDescending(x => x.CreatedAtUtc),
            "status" => descending ? query.OrderByDescending(x => x.Status).ThenByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.Status).ThenByDescending(x => x.CreatedAtUtc),
            "readAt" => descending ? query.OrderByDescending(x => x.ReadAtUtc).ThenByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.ReadAtUtc).ThenByDescending(x => x.CreatedAtUtc),
            _ => descending ? query.OrderByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.CreatedAtUtc)
        };
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "eventtype" => "eventType",
            "status" => "status",
            "readat" => "readAt",
            _ => "createdAt"
        };
    }

    private static string NormalizeSortDirection(string? sortDirection)
    {
        return string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
    }
}

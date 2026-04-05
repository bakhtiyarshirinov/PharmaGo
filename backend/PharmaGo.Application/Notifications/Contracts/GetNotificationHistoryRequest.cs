using System.ComponentModel.DataAnnotations;
using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Application.Notifications.Contracts;

public class GetNotificationHistoryRequest
{
    public bool? UnreadOnly { get; init; }
    public NotificationEventType? EventType { get; init; }
    public NotificationDeliveryStatus? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [MaxLength(32)]
    public string SortBy { get; init; } = "createdAt";

    [MaxLength(4)]
    public string SortDirection { get; init; } = "desc";
}

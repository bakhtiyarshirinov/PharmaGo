using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Notifications.Contracts;

public class NotificationBulkStatusUpdateRequest
{
    [MinLength(1)]
    public IReadOnlyCollection<Guid> NotificationIds { get; init; } = Array.Empty<Guid>();

    [Required]
    public bool MarkAsRead { get; init; } = true;
}

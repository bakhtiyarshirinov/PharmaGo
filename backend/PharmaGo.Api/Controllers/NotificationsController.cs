using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Notifications.Contracts;

namespace PharmaGo.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController(
    ICurrentUserService currentUserService,
    INotificationInboxService notificationInboxService,
    INotificationPreferenceService notificationPreferenceService) : ApiControllerBase
{
    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResponse<NotificationHistoryItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<NotificationHistoryItemResponse>>> GetHistory(
        [FromQuery] GetNotificationHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var response = await notificationInboxService.GetHistoryAsync(currentUserService.UserId.Value, request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("unread")]
    [ProducesResponseType(typeof(NotificationInboxSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NotificationInboxSummaryResponse>> GetUnread(
        [FromQuery] int previewLimit = 5,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var response = await notificationInboxService.GetUnreadAsync(currentUserService.UserId.Value, previewLimit, cancellationToken);
        return Ok(response);
    }

    [HttpGet("preferences")]
    [ProducesResponseType(typeof(NotificationPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NotificationPreferencesResponse>> GetPreferences(CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var response = await notificationPreferenceService.GetAsync(currentUserService.UserId.Value, cancellationToken);
        return Ok(response);
    }

    [HttpPut("preferences")]
    [ProducesResponseType(typeof(NotificationPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NotificationPreferencesResponse>> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var response = await notificationPreferenceService.UpdateAsync(currentUserService.UserId.Value, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var updated = await notificationInboxService.MarkAsReadAsync(currentUserService.UserId.Value, id, cancellationToken);
        return updated ? NoContent() : ApiNotFound("notification_not_found", "Notification was not found.");
    }

    [HttpPost("{id:guid}/unread")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsUnread(Guid id, CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var updated = await notificationInboxService.MarkAsUnreadAsync(currentUserService.UserId.Value, id, cancellationToken);
        return updated ? NoContent() : ApiNotFound("notification_not_found", "Notification was not found.");
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        await notificationInboxService.MarkAllAsReadAsync(currentUserService.UserId.Value, cancellationToken);
        return NoContent();
    }

    [HttpPost("unread-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsUnread(CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        await notificationInboxService.MarkAllAsUnreadAsync(currentUserService.UserId.Value, cancellationToken);
        return NoContent();
    }

    [HttpPost("status/bulk")]
    [ProducesResponseType(typeof(NotificationBulkStatusUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NotificationBulkStatusUpdateResponse>> BulkUpdateStatus(
        [FromBody] NotificationBulkStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        if (request.NotificationIds.Count == 0)
        {
            return ApiValidationProblem("notifications_bulk_ids_required", "At least one notification id is required.");
        }

        var response = await notificationInboxService.BulkUpdateStatusAsync(currentUserService.UserId.Value, request, cancellationToken);
        return Ok(response);
    }
}

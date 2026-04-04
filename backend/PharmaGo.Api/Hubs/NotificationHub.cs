using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PharmaGo.Infrastructure.Auth;

namespace PharmaGo.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var principal = Context.User;
        if (principal is null)
        {
            await base.OnConnectedAsync();
            return;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        var role = principal.FindFirstValue(ClaimTypes.Role);
        if (!string.IsNullOrWhiteSpace(role))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");

            if (role is RoleNames.Pharmacist or RoleNames.Moderator)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "role:staff");
            }
        }

        var pharmacyId = principal.FindFirstValue("pharmacy_id");
        if (!string.IsNullOrWhiteSpace(pharmacyId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"pharmacy:{pharmacyId}");
        }

        await base.OnConnectedAsync();
    }
}

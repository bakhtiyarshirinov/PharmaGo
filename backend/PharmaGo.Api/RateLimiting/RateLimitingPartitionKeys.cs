using System.Security.Claims;

namespace PharmaGo.Api.RateLimiting;

public static class RateLimitingPartitionKeys
{
    public static string GetClientIdentifier(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public static string GetReservationCreateIdentifier(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        return !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : $"client:{GetClientIdentifier(context)}";
    }
}

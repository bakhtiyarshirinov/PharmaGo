using System.Threading.RateLimiting;
using PharmaGo.Api.Controllers;

namespace PharmaGo.Api.RateLimiting;

public class SelectiveRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PartitionedRateLimiter<HttpContext> _authLimiter;
    private readonly PartitionedRateLimiter<HttpContext> _searchLimiter;
    private readonly PartitionedRateLimiter<HttpContext> _reservationCreateLimiter;

    public SelectiveRateLimitingMiddleware(RequestDelegate next, RateLimitingSettings settings)
    {
        _next = next;

        _authLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"auth:{RateLimitingPartitionKeys.GetClientIdentifier(httpContext)}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.AuthPermitLimit,
                    Window = TimeSpan.FromSeconds(settings.AuthWindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));

        _searchLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"search:{RateLimitingPartitionKeys.GetClientIdentifier(httpContext)}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.SearchPermitLimit,
                    Window = TimeSpan.FromSeconds(settings.SearchWindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));

        _reservationCreateLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"reservation-create:{RateLimitingPartitionKeys.GetReservationCreateIdentifier(httpContext)}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.ReservationCreatePermitLimit,
                    Window = TimeSpan.FromSeconds(settings.ReservationCreateWindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var limiter = ResolveLimiter(context);
        if (limiter is null)
        {
            await _next(context);
            return;
        }

        using var lease = await limiter.AcquireAsync(context, 1, context.RequestAborted);
        if (lease.IsAcquired)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        await context.Response.WriteAsJsonAsync(
            ApiProblemDetailsFactory.CreateProblem(
                StatusCodes.Status429TooManyRequests,
                "rate_limit_exceeded",
                "Too many requests were sent to this endpoint. Please retry later.",
                "Rate limit exceeded"),
            context.RequestAborted);
    }

    private PartitionedRateLimiter<HttpContext>? ResolveLimiter(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = context.Request.Method;

        if (HttpMethods.IsPost(method) && IsAuthPath(path))
        {
            return _authLimiter;
        }

        if (HttpMethods.IsGet(method) && IsSearchPath(path))
        {
            return _searchLimiter;
        }

        if (HttpMethods.IsPost(method) && IsReservationCreatePath(path))
        {
            return _reservationCreateLimiter;
        }

        return null;
    }

    private static bool IsAuthPath(string path)
        => path is "/api/auth/register" or "/api/auth/login" or "/api/auth/refresh"
            or "/api/v1/auth/register" or "/api/v1/auth/login" or "/api/v1/auth/refresh";

    private static bool IsSearchPath(string path)
        => path is "/api/medicines/search" or "/api/medicines/suggestions"
            or "/api/pharmacies/search" or "/api/pharmacies/suggestions" or "/api/pharmacies/nearby-map"
            or "/api/v1/medicines/search" or "/api/v1/medicines/suggestions"
            or "/api/v1/pharmacies/search" or "/api/v1/pharmacies/suggestions" or "/api/v1/pharmacies/nearby-map";

    private static bool IsReservationCreatePath(string path)
        => path is "/api/reservations" or "/api/v1/reservations";
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using PharmaGo.Api.Controllers;

namespace PharmaGo.Api.Auth;

public class ApiAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _fallbackHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                ApiProblemDetailsFactory.CreateProblem(
                    StatusCodes.Status403Forbidden,
                    "forbidden",
                    "You do not have permission to access this resource."));
            return;
        }

        if (authorizeResult.Challenged)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(
                ApiProblemDetailsFactory.CreateProblem(
                    StatusCodes.Status401Unauthorized,
                    "unauthorized",
                    "Authentication is required to access this resource."));
            return;
        }

        await _fallbackHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}

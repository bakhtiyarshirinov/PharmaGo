using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PharmaGo.Application.Abstractions;

namespace PharmaGo.Infrastructure.Auth;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var rawValue = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(rawValue, out var userId) ? userId : null;
        }
    }

    public string? PhoneNumber => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.MobilePhone);

    public string? Role => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}

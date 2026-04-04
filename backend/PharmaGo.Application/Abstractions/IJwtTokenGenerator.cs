using PharmaGo.Domain.Models;

namespace PharmaGo.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(AppUser user, string? jwtId = null);
}

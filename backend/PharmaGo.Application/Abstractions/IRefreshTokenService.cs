using PharmaGo.Domain.Models;

namespace PharmaGo.Application.Abstractions;

public interface IRefreshTokenService
{
    Task<(string Token, RefreshToken Entity)> CreateAsync(
        AppUser user,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAsync(RefreshToken refreshToken, string reason, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
}

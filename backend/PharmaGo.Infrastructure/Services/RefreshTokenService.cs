using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmaGo.Application.Abstractions;
using PharmaGo.Domain.Models;
using PharmaGo.Infrastructure.Auth;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.Infrastructure.Services;

public class RefreshTokenService(
    ApplicationDbContext context,
    IOptions<RefreshTokenSettings> settings) : IRefreshTokenService
{
    private readonly RefreshTokenSettings _settings = settings.Value;

    public async Task<(string Token, RefreshToken Entity)> CreateAsync(
        AppUser user,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = ComputeHash(token),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_settings.ExpirationDays),
            UserAgent = NormalizeUserAgent(userAgent)
        };

        await context.RefreshTokens.AddAsync(refreshToken, cancellationToken);

        return (token, refreshToken);
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeHash(token);

        return await context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public Task RevokeAsync(RefreshToken refreshToken, string reason, CancellationToken cancellationToken = default)
    {
        if (refreshToken.RevokedAtUtc is not null)
        {
            return Task.CompletedTask;
        }

        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        refreshToken.RevocationReason = reason;

        return Task.CompletedTask;
    }

    public async Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var tokens = await context.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAtUtc = now;
            token.RevocationReason = reason;
        }
    }

    public static string ComputeHash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string? NormalizeUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        var normalized = userAgent.Trim();
        return normalized.Length <= 512 ? normalized : normalized[..512];
    }
}

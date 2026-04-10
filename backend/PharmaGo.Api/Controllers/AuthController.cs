using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.Infrastructure.Caching;
using PharmaGo.Infrastructure.Auth;

namespace PharmaGo.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
public class AuthController(
    IApplicationDbContext context,
    IAppCacheService cacheService,
    IAuditService auditService,
    IRefreshTokenService refreshTokenService,
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher<AppUser> passwordHasher,
    ICurrentUserService currentUserService,
    IOptions<JwtSettings> jwtOptions) : ApiControllerBase
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.PhoneNumber) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiValidationProblem("auth_register_required_fields", "FirstName, LastName, PhoneNumber and Password are required.");
        }

        if (request.Password.Length < 8)
        {
            return ApiValidationProblem("auth_password_too_short", "Password must be at least 8 characters long.");
        }

        if (!IsPasswordComplexEnough(request.Password))
        {
            return ApiValidationProblem(
                "auth_password_too_weak",
                "Password must include at least one uppercase letter, one lowercase letter and one digit.");
        }

        var normalizedPhone = request.PhoneNumber.Trim();
        var normalizedEmail = request.Email?.Trim().ToLowerInvariant();

        var accountExists = await context.Users.AnyAsync(
            x => x.PhoneNumber == normalizedPhone ||
                (!string.IsNullOrWhiteSpace(normalizedEmail) && x.Email == normalizedEmail),
            cancellationToken);

        if (accountExists)
        {
            return ApiConflict(
                "auth_account_already_exists",
                "An account with the provided sign-in details already exists.");
        }

        var user = new AppUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = normalizedPhone,
            Email = normalizedEmail,
            TelegramUsername = request.TelegramUsername?.Trim(),
            TelegramChatId = request.TelegramChatId?.Trim(),
            Role = UserRole.User,
            IsActive = true
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await context.Users.AddAsync(user, cancellationToken);
        var issuedTokens = await refreshTokenService.CreateAsync(
            user,
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.Users, cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.Dashboard, cancellationToken);
        await auditService.WriteAsync(
            action: "auth.register",
            entityName: "AppUser",
            entityId: user.Id.ToString(),
            userId: user.Id,
            description: $"User {user.PhoneNumber} registered.",
            metadata: new { user.Id, user.PhoneNumber, user.Role },
            cancellationToken: cancellationToken);

        return CreatedAtAction(
            nameof(Me),
            new { },
            CreateAuthResponse(user, issuedTokens.Token, issuedTokens.Entity.ExpiresAtUtc));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiValidationProblem("auth_login_required_fields", "PhoneNumber and Password are required.");
        }

        var normalizedPhone = request.PhoneNumber.Trim();

        var user = await context.Users.FirstOrDefaultAsync(
            x => x.PhoneNumber == normalizedPhone && x.IsActive,
            cancellationToken);

        if (user is null)
        {
            return ApiUnauthorized("Invalid credentials.");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult is PasswordVerificationResult.Failed)
        {
            return ApiUnauthorized("Invalid credentials.");
        }

        var issuedTokens = await refreshTokenService.CreateAsync(
            user,
            Request.Headers.UserAgent.ToString(),
            cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Ok(CreateAuthResponse(user, issuedTokens.Token, issuedTokens.Entity.ExpiresAtUtc));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> Me(CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var user = await context.Users
            .AsNoTracking()
            .Where(x => x.Id == currentUserService.UserId.Value)
            .Select(x => new UserProfileResponse
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                TelegramUsername = x.TelegramUsername,
                TelegramChatId = x.TelegramChatId,
                Role = x.Role,
                PharmacyId = x.PharmacyId
            })
            .FirstOrDefaultAsync(cancellationToken);

        return user is null ? ApiUnauthorized("User session is no longer valid.") : Ok(user);
    }

    [Authorize(Policy = PolicyNames.ManageUsers)]
    [HttpPut("users/{id:guid}/role")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> UpdateRole(
        Guid id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return ApiNotFound("user_not_found", "User was not found.");
        }

        if (request.Role == UserRole.Moderator)
        {
            return ApiValidationProblem("user_role_assignment_invalid", "Moderator role cannot be assigned from this endpoint.");
        }

        if (request.Role == UserRole.Pharmacist && !user.PharmacyId.HasValue)
        {
            return ApiValidationProblem("user_role_assignment_invalid", "Pharmacist role requires a pharmacy assignment. Use the users management endpoint.");
        }

        user.Role = request.Role;
        await context.SaveChangesAsync(cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.Users, cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.Dashboard, cancellationToken);
        await auditService.WriteAsync(
            action: "user.role.updated",
            entityName: "AppUser",
            entityId: user.Id.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: user.PharmacyId,
            description: $"User role updated to {user.Role}.",
            metadata: new { user.Id, user.PhoneNumber, Role = user.Role.ToString() },
            cancellationToken: cancellationToken);

        return Ok(new UserProfileResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            TelegramUsername = user.TelegramUsername,
            TelegramChatId = user.TelegramChatId,
            Role = user.Role,
            PharmacyId = user.PharmacyId
        });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var refreshToken = await refreshTokenService.GetByTokenAsync(request.RefreshToken.Trim(), cancellationToken);
        if (refreshToken?.User is null || !refreshToken.User.IsActive)
        {
            return ApiUnauthorized("Invalid refresh token.");
        }

        if (refreshToken.RevokedAtUtc is not null || refreshToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return ApiUnauthorized("Refresh token is no longer active.");
        }

        var replacement = await refreshTokenService.CreateAsync(
            refreshToken.User,
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        refreshToken.LastUsedAtUtc = DateTime.UtcNow;
        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        refreshToken.ReplacedByTokenHash = replacement.Entity.TokenHash;
        refreshToken.RevocationReason = "rotated";

        await context.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "auth.refresh",
            entityName: "RefreshToken",
            entityId: refreshToken.Id.ToString(),
            userId: refreshToken.UserId,
            pharmacyId: refreshToken.User.PharmacyId,
            description: "Refresh token rotated.",
            metadata: new { refreshToken.Id, refreshToken.UserId },
            cancellationToken: cancellationToken);

        return Ok(CreateAuthResponse(
            refreshToken.User,
            replacement.Token,
            replacement.Entity.ExpiresAtUtc));
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var refreshToken = await refreshTokenService.GetByTokenAsync(request.RefreshToken.Trim(), cancellationToken);
        if (refreshToken is null || refreshToken.UserId != currentUserService.UserId)
        {
            return NoContent();
        }

        await refreshTokenService.RevokeAsync(refreshToken, "logout", cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "auth.logout",
            entityName: "RefreshToken",
            entityId: refreshToken.Id.ToString(),
            userId: refreshToken.UserId,
            pharmacyId: refreshToken.User?.PharmacyId,
            description: "Refresh token revoked on logout.",
            metadata: new { refreshToken.Id, refreshToken.UserId },
            cancellationToken: cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("revoke-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAll(CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        await refreshTokenService.RevokeAllForUserAsync(currentUserService.UserId.Value, "revoke-all", cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "auth.revoke_all",
            entityName: "RefreshToken",
            entityId: currentUserService.UserId.Value.ToString(),
            userId: currentUserService.UserId.Value,
            description: "All active refresh tokens revoked.",
            cancellationToken: cancellationToken);

        return NoContent();
    }

    private AuthResponse CreateAuthResponse(AppUser user, string refreshToken, DateTime refreshTokenExpiresAtUtc)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        return new AuthResponse
        {
            AccessToken = jwtTokenGenerator.GenerateToken(user),
            ExpiresAtUtc = expiresAtUtc,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
            User = new UserProfileResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                TelegramUsername = user.TelegramUsername,
                TelegramChatId = user.TelegramChatId,
                Role = user.Role,
                PharmacyId = user.PharmacyId
            }
        };
    }

    private static bool IsPasswordComplexEnough(string password)
    {
        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;

        foreach (var character in password)
        {
            if (char.IsUpper(character))
            {
                hasUpper = true;
            }
            else if (char.IsLower(character))
            {
                hasLower = true;
            }
            else if (char.IsDigit(character))
            {
                hasDigit = true;
            }

            if (hasUpper && hasLower && hasDigit)
            {
                return true;
            }
        }

        return false;
    }
}

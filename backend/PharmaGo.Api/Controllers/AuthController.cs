using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.Infrastructure.Auth;

namespace PharmaGo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IApplicationDbContext context,
    IAuditService auditService,
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordHasher<AppUser> passwordHasher,
    ICurrentUserService currentUserService,
    IOptions<JwtSettings> jwtOptions) : ControllerBase
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
            return BadRequest("FirstName, LastName, PhoneNumber and Password are required.");
        }

        if (request.Password.Length < 8)
        {
            return BadRequest("Password must be at least 8 characters long.");
        }

        var normalizedPhone = request.PhoneNumber.Trim();
        var normalizedEmail = request.Email?.Trim().ToLowerInvariant();

        var phoneExists = await context.Users.AnyAsync(x => x.PhoneNumber == normalizedPhone, cancellationToken);
        if (phoneExists)
        {
            return BadRequest("A user with this phone number already exists.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var emailExists = await context.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
            if (emailExists)
            {
                return BadRequest("A user with this email already exists.");
            }
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
        await context.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "auth.register",
            entityName: "AppUser",
            entityId: user.Id.ToString(),
            userId: user.Id,
            description: $"User {user.PhoneNumber} registered.",
            metadata: new { user.Id, user.PhoneNumber, user.Role },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(Me), new { }, CreateAuthResponse(user));
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
            return BadRequest("PhoneNumber and Password are required.");
        }

        var normalizedPhone = request.PhoneNumber.Trim();

        var user = await context.Users.FirstOrDefaultAsync(
            x => x.PhoneNumber == normalizedPhone && x.IsActive,
            cancellationToken);

        if (user is null)
        {
            return Unauthorized("Invalid credentials.");
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult is PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid credentials.");
        }

        return Ok(CreateAuthResponse(user));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> Me(CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return Unauthorized();
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

        return user is null ? Unauthorized() : Ok(user);
    }

    [Authorize(Policy = RoleNames.ModeratorPolicy)]
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
            return NotFound();
        }

        user.Role = request.Role;
        await context.SaveChangesAsync(cancellationToken);
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

    private AuthResponse CreateAuthResponse(AppUser user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        return new AuthResponse
        {
            AccessToken = jwtTokenGenerator.GenerateToken(user),
            ExpiresAtUtc = expiresAtUtc,
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
}

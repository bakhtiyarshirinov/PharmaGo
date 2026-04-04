using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Users.Contracts;
using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.Infrastructure.Auth;

namespace PharmaGo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = RoleNames.ModeratorPolicy)]
public class UsersController(
    IApplicationDbContext context,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    IPasswordHasher<AppUser> passwordHasher,
    IRefreshTokenService refreshTokenService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserManagementResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<UserManagementResponse>>> Get(
        [FromQuery] UserRole? role,
        [FromQuery] bool? isActive,
        [FromQuery] Guid? pharmacyId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = search?.Trim();

        var users = await context.Users
            .AsNoTracking()
            .Where(x =>
                (!role.HasValue || x.Role == role.Value) &&
                (!isActive.HasValue || x.IsActive == isActive.Value) &&
                (!pharmacyId.HasValue || x.PharmacyId == pharmacyId.Value) &&
                (string.IsNullOrWhiteSpace(normalizedSearch) ||
                 EF.Functions.ILike(x.FirstName, $"%{normalizedSearch}%") ||
                 EF.Functions.ILike(x.LastName, $"%{normalizedSearch}%") ||
                 EF.Functions.ILike(x.PhoneNumber, $"%{normalizedSearch}%") ||
                 (x.Email != null && EF.Functions.ILike(x.Email, $"%{normalizedSearch}%"))))
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Take(100)
            .Select(x => new UserManagementResponse
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                TelegramUsername = x.TelegramUsername,
                TelegramChatId = x.TelegramChatId,
                Role = x.Role,
                IsActive = x.IsActive,
                PharmacyId = x.PharmacyId,
                PharmacyName = x.Pharmacy != null ? x.Pharmacy.Name : null,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserManagementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserManagementResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await ProjectUsers()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserManagementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserManagementResponse>> Create(
        [FromBody] CreateManagedUserRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateRoleAndPharmacyAsync(request.Role, request.PharmacyId, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        var normalizedPhone = request.PhoneNumber.Trim();
        var normalizedEmail = NormalizeEmail(request.Email);

        if (await context.Users.AnyAsync(x => x.PhoneNumber == normalizedPhone, cancellationToken))
        {
            return BadRequest("A user with this phone number already exists.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedEmail) &&
            await context.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            return BadRequest("A user with this email already exists.");
        }

        var user = new AppUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = normalizedPhone,
            Email = normalizedEmail,
            TelegramUsername = NormalizeOptional(request.TelegramUsername),
            TelegramChatId = NormalizeOptional(request.TelegramChatId),
            Role = request.Role,
            PharmacyId = request.PharmacyId,
            IsActive = true
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "user.created",
            entityName: "AppUser",
            entityId: user.Id.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: user.PharmacyId,
            description: $"User {user.PhoneNumber} created by moderator.",
            metadata: new { user.Id, user.PhoneNumber, Role = user.Role.ToString() },
            cancellationToken: cancellationToken);

        var response = await ProjectUsers()
            .FirstAsync(x => x.Id == user.Id, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserManagementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserManagementResponse>> Update(
        Guid id,
        [FromBody] UpdateManagedUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var validationError = await ValidateRoleAndPharmacyAsync(request.Role, request.PharmacyId, cancellationToken);
        if (validationError is not null)
        {
            return validationError;
        }

        var normalizedPhone = request.PhoneNumber.Trim();
        var normalizedEmail = NormalizeEmail(request.Email);

        if (await context.Users.AnyAsync(x => x.Id != id && x.PhoneNumber == normalizedPhone, cancellationToken))
        {
            return BadRequest("A user with this phone number already exists.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedEmail) &&
            await context.Users.AnyAsync(x => x.Id != id && x.Email == normalizedEmail, cancellationToken))
        {
            return BadRequest("A user with this email already exists.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = normalizedPhone;
        user.Email = normalizedEmail;
        user.TelegramUsername = NormalizeOptional(request.TelegramUsername);
        user.TelegramChatId = NormalizeOptional(request.TelegramChatId);
        user.Role = request.Role;
        user.PharmacyId = request.PharmacyId;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await refreshTokenService.RevokeAllForUserAsync(user.Id, "password-changed", cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "user.updated",
            entityName: "AppUser",
            entityId: user.Id.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: user.PharmacyId,
            description: $"User {user.PhoneNumber} updated by moderator.",
            metadata: new { user.Id, user.PhoneNumber, Role = user.Role.ToString(), PasswordChanged = !string.IsNullOrWhiteSpace(request.Password) },
            cancellationToken: cancellationToken);

        var response = await ProjectUsers()
            .FirstAsync(x => x.Id == user.Id, cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == currentUserService.UserId)
        {
            return BadRequest("Moderator cannot deactivate the current account.");
        }

        if (!user.IsActive)
        {
            return NoContent();
        }

        user.IsActive = false;
        await refreshTokenService.RevokeAllForUserAsync(user.Id, "soft-delete", cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "user.deactivated",
            entityName: "AppUser",
            entityId: user.Id.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: user.PharmacyId,
            description: $"User {user.PhoneNumber} deactivated by moderator.",
            metadata: new { user.Id, user.PhoneNumber, Role = user.Role.ToString() },
            cancellationToken: cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(typeof(UserManagementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserManagementResponse>> Restore(Guid id, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = true;
        await context.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "user.restored",
            entityName: "AppUser",
            entityId: user.Id.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: user.PharmacyId,
            description: $"User {user.PhoneNumber} restored by moderator.",
            metadata: new { user.Id, user.PhoneNumber, Role = user.Role.ToString() },
            cancellationToken: cancellationToken);

        var response = await ProjectUsers()
            .FirstAsync(x => x.Id == user.Id, cancellationToken);

        return Ok(response);
    }

    private IQueryable<UserManagementResponse> ProjectUsers()
    {
        return context.Users
            .AsNoTracking()
            .Select(x => new UserManagementResponse
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                TelegramUsername = x.TelegramUsername,
                TelegramChatId = x.TelegramChatId,
                Role = x.Role,
                IsActive = x.IsActive,
                PharmacyId = x.PharmacyId,
                PharmacyName = x.Pharmacy != null ? x.Pharmacy.Name : null,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            });
    }

    private async Task<ActionResult?> ValidateRoleAndPharmacyAsync(
        UserRole role,
        Guid? pharmacyId,
        CancellationToken cancellationToken)
    {
        if (role == UserRole.Moderator)
        {
            return BadRequest("Moderator accounts cannot be created or updated from this endpoint.");
        }

        if (role == UserRole.Pharmacist)
        {
            if (!pharmacyId.HasValue)
            {
                return BadRequest("PharmacyId is required for pharmacist accounts.");
            }

            var pharmacyExists = await context.Pharmacies.AnyAsync(x => x.Id == pharmacyId.Value && x.IsActive, cancellationToken);
            if (!pharmacyExists)
            {
                return NotFound("Pharmacy was not found.");
            }
        }

        if (role == UserRole.User && pharmacyId.HasValue)
        {
            return BadRequest("Regular users cannot be assigned to a pharmacy.");
        }

        return null;
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

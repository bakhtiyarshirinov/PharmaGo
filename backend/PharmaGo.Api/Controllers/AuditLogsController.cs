using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Audit.Queries.GetAuditLogs;
using PharmaGo.Infrastructure.Auth;

namespace PharmaGo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PolicyNames.ReadAuditLogs)]
public class AuditLogsController(IApplicationDbContext context, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<AuditLogResponse>>> Get(
        [FromQuery] Guid? pharmacyId,
        [FromQuery] string? entityName,
        [FromQuery] string? action,
        CancellationToken cancellationToken)
    {
        if (pharmacyId.HasValue)
        {
            var accessResult = await EnsurePharmacyAccessAsync(pharmacyId.Value, cancellationToken);
            if (accessResult is not null)
            {
                return accessResult;
            }
        }

        var effectivePharmacyId = pharmacyId;
        if (!effectivePharmacyId.HasValue && User.IsInRole(RoleNames.Pharmacist) && !User.IsInRole(RoleNames.Moderator))
        {
            var currentUser = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == currentUserService.UserId, cancellationToken);

            effectivePharmacyId = currentUser?.PharmacyId;
        }

        var auditLogs = await context.AuditLogs
            .AsNoTracking()
            .Where(x =>
                (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value) &&
                (string.IsNullOrWhiteSpace(entityName) || x.EntityName == entityName) &&
                (string.IsNullOrWhiteSpace(action) || x.Action == action))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .Select(x => new AuditLogResponse
            {
                Id = x.Id,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Description = x.Description,
                MetadataJson = x.MetadataJson,
                UserId = x.UserId,
                UserFullName = x.User != null ? $"{x.User.FirstName} {x.User.LastName}" : null,
                PharmacyId = x.PharmacyId,
                PharmacyName = x.Pharmacy != null ? x.Pharmacy.Name : null,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(auditLogs);
    }

    private async Task<ActionResult?> EnsurePharmacyAccessAsync(Guid pharmacyId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(RoleNames.Moderator))
        {
            return null;
        }

        var currentUser = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == currentUserService.UserId, cancellationToken);

        if (currentUser?.PharmacyId != pharmacyId)
        {
            return Forbid();
        }

        return null;
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Dashboard.Queries.GetDashboardSummary;
using PharmaGo.Application.Dashboard.Queries.GetRecentReservations;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.Infrastructure.Caching;
using PharmaGo.Infrastructure.Auth;

namespace PharmaGo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PolicyNames.ViewDashboard)]
public class DashboardController(
    IApplicationDbContext context,
    IAppCacheService cacheService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(
        [FromQuery] Guid? pharmacyId,
        CancellationToken cancellationToken)
    {
        var effectivePharmacyId = await GetEffectivePharmacyIdAsync(pharmacyId, cancellationToken);
        if (effectivePharmacyId == Guid.Empty)
        {
            return Forbid();
        }

        var scopeVersion = await cacheService.GetScopeVersionAsync(CacheScopes.Dashboard, cancellationToken);
        var cacheKey = $"dashboard:summary:v{scopeVersion}:pharmacy={effectivePharmacyId?.ToString() ?? "all"}";
        var cached = await cacheService.GetAsync<DashboardSummaryResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Ok(cached);
        }

        var pharmacyName = effectivePharmacyId.HasValue
            ? await context.Pharmacies.Where(x => x.Id == effectivePharmacyId.Value).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        var activeStatuses = new[]
        {
            ReservationStatus.Pending,
            ReservationStatus.Confirmed,
            ReservationStatus.ReadyForPickup
        };

        var today = DateTime.UtcNow.Date;

        var response = new DashboardSummaryResponse
        {
            PharmacyId = effectivePharmacyId,
            PharmacyName = pharmacyName,
            TotalMedicines = await context.Medicines.CountAsync(x => x.IsActive, cancellationToken),
            TotalPharmacies = effectivePharmacyId.HasValue
                ? 1
                : await context.Pharmacies.CountAsync(x => x.IsActive, cancellationToken),
            TotalUsers = await context.Users.CountAsync(
                x => x.IsActive && (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value || x.PharmacyId == null),
                cancellationToken),
            TotalStockItems = await context.StockItems.CountAsync(
                x => x.IsActive && (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value),
                cancellationToken),
            TotalAvailableUnits = await context.StockItems
                .Where(x => x.IsActive && (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value))
                .SumAsync(x => x.Quantity - x.ReservedQuantity, cancellationToken),
            TotalReservedUnits = await context.StockItems
                .Where(x => x.IsActive && (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value))
                .SumAsync(x => x.ReservedQuantity, cancellationToken),
            ActiveReservations = await context.Reservations.CountAsync(
                x => activeStatuses.Contains(x.Status) && (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value),
                cancellationToken),
            ReadyForPickupReservations = await context.Reservations.CountAsync(
                x => x.Status == ReservationStatus.ReadyForPickup && (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value),
                cancellationToken),
            LowStockAlerts = await context.StockItems.CountAsync(
                x => x.IsActive &&
                    (x.Quantity - x.ReservedQuantity) <= x.ReorderLevel &&
                    (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value),
                cancellationToken),
            CompletedToday = await context.Reservations.CountAsync(
                x => x.Status == ReservationStatus.Completed &&
                    x.UpdatedAtUtc.HasValue &&
                    x.UpdatedAtUtc.Value >= today &&
                    (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value),
                cancellationToken),
            ReservedValue = await context.Reservations
                .Where(x => activeStatuses.Contains(x.Status) && (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value))
                .SumAsync(x => x.TotalAmount, cancellationToken)
        };

        await cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(1), cancellationToken);

        return Ok(response);
    }

    [HttpGet("recent-reservations")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DashboardRecentReservationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DashboardRecentReservationResponse>>> GetRecentReservations(
        [FromQuery] Guid? pharmacyId,
        CancellationToken cancellationToken)
    {
        var effectivePharmacyId = await GetEffectivePharmacyIdAsync(pharmacyId, cancellationToken);
        if (effectivePharmacyId == Guid.Empty)
        {
            return Forbid();
        }

        var scopeVersion = await cacheService.GetScopeVersionAsync(CacheScopes.Dashboard, cancellationToken);
        var cacheKey = $"dashboard:recent:v{scopeVersion}:pharmacy={effectivePharmacyId?.ToString() ?? "all"}";
        var cached = await cacheService.GetAsync<IReadOnlyCollection<DashboardRecentReservationResponse>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Ok(cached);
        }

        var reservations = await context.Reservations
            .AsNoTracking()
            .Where(x => !effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(10)
            .Select(x => new DashboardRecentReservationResponse
            {
                ReservationId = x.Id,
                ReservationNumber = x.ReservationNumber,
                Status = x.Status,
                CustomerFullName = $"{x.Customer!.FirstName} {x.Customer.LastName}",
                PharmacyName = x.Pharmacy!.Name,
                TotalAmount = x.TotalAmount,
                ReservedUntilUtc = x.ReservedUntilUtc,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        await cacheService.SetAsync(cacheKey, reservations, TimeSpan.FromMinutes(1), cancellationToken);

        return Ok(reservations);
    }

    private async Task<Guid?> GetEffectivePharmacyIdAsync(Guid? requestedPharmacyId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(RoleNames.Moderator))
        {
            return requestedPharmacyId;
        }

        var currentUser = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == currentUserService.UserId, cancellationToken);

        if (currentUser?.PharmacyId is null)
        {
            return Guid.Empty;
        }

        if (requestedPharmacyId.HasValue && requestedPharmacyId.Value != currentUser.PharmacyId.Value)
        {
            return Guid.Empty;
        }

        return currentUser.PharmacyId.Value;
    }
}

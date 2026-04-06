using System.Globalization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Pharmacies.Contracts;
using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.Infrastructure.Auth;
using PharmaGo.Infrastructure.Caching;
using PharmaGo.Infrastructure.Services;

namespace PharmaGo.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/pharmacies")]
[Route("api/admin/pharmacies")]
[Authorize(Policy = PolicyNames.ManagePharmacies)]
public class AdminPharmaciesController(
    IApplicationDbContext context,
    IAppCacheService cacheService,
    IAuditService auditService,
    ICurrentUserService currentUserService) : ApiControllerBase
{
    private static readonly ReservationStatus[] ActiveReservationStatuses =
    [
        ReservationStatus.Pending,
        ReservationStatus.Confirmed,
        ReservationStatus.ReadyForPickup
    ];

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ManagedPharmacyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ManagedPharmacyResponse>>> Get(
        [FromQuery] string? search,
        [FromQuery] string? city,
        [FromQuery] bool? isActive,
        [FromQuery] bool? supportsReservations,
        [FromQuery] bool? hasDelivery,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var normalizedSearch = search?.Trim();
        var normalizedCity = city?.Trim();
        var normalizedSortBy = NormalizeSortBy(sortBy);
        var normalizedSortDirection = NormalizeSortDirection(sortDirection);

        var query = ProjectPharmacies()
            .Where(x =>
                (string.IsNullOrWhiteSpace(normalizedSearch) ||
                 EF.Functions.ILike(x.Name, $"%{normalizedSearch}%") ||
                 EF.Functions.ILike(x.Address, $"%{normalizedSearch}%") ||
                 (x.PhoneNumber != null && EF.Functions.ILike(x.PhoneNumber, $"%{normalizedSearch}%")) ||
                 (x.PharmacyChainName != null && EF.Functions.ILike(x.PharmacyChainName, $"%{normalizedSearch}%"))) &&
                (string.IsNullOrWhiteSpace(normalizedCity) || EF.Functions.ILike(x.City, normalizedCity)) &&
                (!isActive.HasValue || x.IsActive == isActive.Value) &&
                (!supportsReservations.HasValue || x.SupportsReservations == supportsReservations.Value) &&
                (!hasDelivery.HasValue || x.HasDelivery == hasDelivery.Value));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await ApplySorting(query, normalizedSortBy, normalizedSortDirection)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<ManagedPharmacyResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
            SortBy = normalizedSortBy,
            SortDirection = normalizedSortDirection
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ManagedPharmacyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagedPharmacyResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var pharmacy = await ProjectPharmacies()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return pharmacy is null
            ? ApiNotFound("pharmacy_not_found", "Pharmacy was not found.")
            : Ok(pharmacy);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ManagedPharmacyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagedPharmacyResponse>> Create(
        [FromBody] CreateManagedPharmacyRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await ValidatePharmacyRequestAsync(
            request.Name,
            request.Address,
            request.City,
            request.LocationLatitude,
            request.LocationLongitude,
            request.IsOpen24Hours,
            request.OpeningHoursJson,
            request.PharmacyChainId,
            cancellationToken);

        if (validationResult.ErrorResult is not null)
        {
            return validationResult.ErrorResult;
        }

        var normalizedName = request.Name.Trim();
        var normalizedAddress = request.Address.Trim();
        var normalizedCity = request.City.Trim();

        if (await context.Pharmacies.AnyAsync(
                x => x.IsActive &&
                     x.Name == normalizedName &&
                     x.Address == normalizedAddress &&
                     x.City == normalizedCity,
                cancellationToken))
        {
            return ApiConflict("pharmacy_already_exists", "An active pharmacy with the same name and address already exists.");
        }

        var now = DateTime.UtcNow;
        var pharmacy = new Pharmacy
        {
            Name = normalizedName,
            Address = normalizedAddress,
            City = normalizedCity,
            Region = NormalizeOptional(request.Region),
            PhoneNumber = NormalizeOptional(request.PhoneNumber),
            LocationLatitude = request.LocationLatitude,
            LocationLongitude = request.LocationLongitude,
            IsOpen24Hours = request.IsOpen24Hours,
            OpeningHoursJson = validationResult.NormalizedOpeningHoursJson,
            SupportsReservations = request.SupportsReservations,
            HasDelivery = request.HasDelivery,
            PharmacyChainId = request.PharmacyChainId,
            LastLocationVerifiedAtUtc = request.LocationLatitude.HasValue && request.LocationLongitude.HasValue ? now : null,
            IsActive = true
        };

        SyncLegacyCoordinates(pharmacy);

        await context.Pharmacies.AddAsync(pharmacy, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await BumpRelevantCachesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "pharmacy.created",
            entityName: nameof(Pharmacy),
            entityId: pharmacy.Id.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: pharmacy.Id,
            description: $"Pharmacy {pharmacy.Name} created by moderator.",
            metadata: new
            {
                pharmacy.Id,
                pharmacy.Name,
                pharmacy.City,
                pharmacy.IsOpen24Hours,
                pharmacy.SupportsReservations,
                pharmacy.HasDelivery
            },
            cancellationToken: cancellationToken);

        var response = await ProjectPharmacies()
            .FirstAsync(x => x.Id == pharmacy.Id, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = pharmacy.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ManagedPharmacyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagedPharmacyResponse>> Update(
        Guid id,
        [FromBody] UpdateManagedPharmacyRequest request,
        CancellationToken cancellationToken)
    {
        var pharmacy = await context.Pharmacies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (pharmacy is null)
        {
            return ApiNotFound("pharmacy_not_found", "Pharmacy was not found.");
        }

        var validationResult = await ValidatePharmacyRequestAsync(
            request.Name,
            request.Address,
            request.City,
            request.LocationLatitude,
            request.LocationLongitude,
            request.IsOpen24Hours,
            request.OpeningHoursJson,
            request.PharmacyChainId,
            cancellationToken);

        if (validationResult.ErrorResult is not null)
        {
            return validationResult.ErrorResult;
        }

        var normalizedName = request.Name.Trim();
        var normalizedAddress = request.Address.Trim();
        var normalizedCity = request.City.Trim();

        if (await context.Pharmacies.AnyAsync(
                x => x.Id != id &&
                     x.IsActive &&
                     x.Name == normalizedName &&
                     x.Address == normalizedAddress &&
                     x.City == normalizedCity,
                cancellationToken))
        {
            return ApiConflict("pharmacy_already_exists", "An active pharmacy with the same name and address already exists.");
        }

        pharmacy.Name = normalizedName;
        pharmacy.Address = normalizedAddress;
        pharmacy.City = normalizedCity;
        pharmacy.Region = NormalizeOptional(request.Region);
        pharmacy.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        pharmacy.LocationLatitude = request.LocationLatitude;
        pharmacy.LocationLongitude = request.LocationLongitude;
        pharmacy.IsOpen24Hours = request.IsOpen24Hours;
        pharmacy.OpeningHoursJson = validationResult.NormalizedOpeningHoursJson;
        pharmacy.SupportsReservations = request.SupportsReservations;
        pharmacy.HasDelivery = request.HasDelivery;
        pharmacy.PharmacyChainId = request.PharmacyChainId;
        pharmacy.LastLocationVerifiedAtUtc = request.LocationLatitude.HasValue && request.LocationLongitude.HasValue
            ? DateTime.UtcNow
            : null;

        SyncLegacyCoordinates(pharmacy);

        await context.SaveChangesAsync(cancellationToken);
        await BumpRelevantCachesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "pharmacy.updated",
            entityName: nameof(Pharmacy),
            entityId: pharmacy.Id.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: pharmacy.Id,
            description: $"Pharmacy {pharmacy.Name} updated by moderator.",
            metadata: new
            {
                pharmacy.Id,
                pharmacy.Name,
                pharmacy.City,
                pharmacy.IsOpen24Hours,
                pharmacy.SupportsReservations,
                pharmacy.HasDelivery,
                pharmacy.IsActive
            },
            cancellationToken: cancellationToken);

        var response = await ProjectPharmacies()
            .FirstAsync(x => x.Id == pharmacy.Id, cancellationToken);

        return Ok(response);
    }

    [HttpPut("{id:guid}/schedule")]
    [ProducesResponseType(typeof(ManagedPharmacyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagedPharmacyResponse>> UpdateSchedule(
        Guid id,
        [FromBody] UpdatePharmacyScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var pharmacy = await context.Pharmacies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (pharmacy is null)
        {
            return ApiNotFound("pharmacy_not_found", "Pharmacy was not found.");
        }

        if (!request.IsOpen24Hours && string.IsNullOrWhiteSpace(request.OpeningHoursJson))
        {
            return ApiValidationProblem("pharmacy_schedule_required", "Opening hours are required when the pharmacy is not open 24 hours.");
        }

        if (!PharmacyDiscoverySupport.TryNormalizeOpeningHoursJson(
                request.OpeningHoursJson,
                out var normalizedOpeningHoursJson,
                out var scheduleError))
        {
            return ApiValidationProblem("pharmacy_schedule_invalid", scheduleError ?? "Opening hours are invalid.");
        }

        pharmacy.IsOpen24Hours = request.IsOpen24Hours;
        pharmacy.OpeningHoursJson = request.IsOpen24Hours ? normalizedOpeningHoursJson : normalizedOpeningHoursJson;

        await context.SaveChangesAsync(cancellationToken);
        await BumpRelevantCachesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "pharmacy.schedule.updated",
            entityName: nameof(Pharmacy),
            entityId: pharmacy.Id.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: pharmacy.Id,
            description: $"Schedule updated for pharmacy {pharmacy.Name}.",
            metadata: new
            {
                pharmacy.Id,
                pharmacy.IsOpen24Hours,
                pharmacy.OpeningHoursJson
            },
            cancellationToken: cancellationToken);

        var response = await ProjectPharmacies()
            .FirstAsync(x => x.Id == pharmacy.Id, cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var pharmacy = await context.Pharmacies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (pharmacy is null)
        {
            return ApiNotFound("pharmacy_not_found", "Pharmacy was not found.");
        }

        var now = DateTime.UtcNow;
        var hasActiveReservations = await context.Reservations.AnyAsync(
            x => x.PharmacyId == id &&
                 ActiveReservationStatuses.Contains(x.Status) &&
                 x.ReservedUntilUtc > now,
            cancellationToken);

        if (hasActiveReservations)
        {
            return ApiConflict("pharmacy_has_active_reservations", "The pharmacy cannot be deactivated while it has active reservations.");
        }

        pharmacy.IsActive = false;

        await context.SaveChangesAsync(cancellationToken);
        await BumpRelevantCachesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "pharmacy.deactivated",
            entityName: nameof(Pharmacy),
            entityId: pharmacy.Id.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: pharmacy.Id,
            description: $"Pharmacy {pharmacy.Name} deactivated by moderator.",
            metadata: new { pharmacy.Id, pharmacy.Name },
            cancellationToken: cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(typeof(ManagedPharmacyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagedPharmacyResponse>> Restore(Guid id, CancellationToken cancellationToken)
    {
        var pharmacy = await context.Pharmacies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (pharmacy is null)
        {
            return ApiNotFound("pharmacy_not_found", "Pharmacy was not found.");
        }

        pharmacy.IsActive = true;
        await context.SaveChangesAsync(cancellationToken);
        await BumpRelevantCachesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "pharmacy.restored",
            entityName: nameof(Pharmacy),
            entityId: pharmacy.Id.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: pharmacy.Id,
            description: $"Pharmacy {pharmacy.Name} restored by moderator.",
            metadata: new { pharmacy.Id, pharmacy.Name },
            cancellationToken: cancellationToken);

        var response = await ProjectPharmacies()
            .FirstAsync(x => x.Id == pharmacy.Id, cancellationToken);

        return Ok(response);
    }

    private IQueryable<ManagedPharmacyResponse> ProjectPharmacies()
    {
        var now = DateTime.UtcNow;

        return context.Pharmacies
            .AsNoTracking()
            .Select(x => new ManagedPharmacyResponse
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                City = x.City,
                Region = x.Region,
                PhoneNumber = x.PhoneNumber,
                LocationLatitude = x.LocationLatitude,
                LocationLongitude = x.LocationLongitude,
                IsOpen24Hours = x.IsOpen24Hours,
                OpeningHoursJson = x.OpeningHoursJson,
                SupportsReservations = x.SupportsReservations,
                HasDelivery = x.HasDelivery,
                IsActive = x.IsActive,
                PharmacyChainId = x.PharmacyChainId,
                PharmacyChainName = x.PharmacyChain != null ? x.PharmacyChain.Name : null,
                EmployeeCount = x.Employees.Count(e => e.IsActive),
                ActiveStockItemCount = x.StockItems.Count(s => s.IsActive),
                ActiveReservationCount = x.Reservations.Count(r =>
                    ActiveReservationStatuses.Contains(r.Status) &&
                    r.ReservedUntilUtc > now),
                LastLocationVerifiedAtUtc = x.LastLocationVerifiedAtUtc,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            });
    }

    private static IQueryable<ManagedPharmacyResponse> ApplySorting(
        IQueryable<ManagedPharmacyResponse> query,
        string sortBy,
        string sortDirection)
    {
        var descending = sortDirection == "desc";

        return (sortBy, descending) switch
        {
            ("city", true) => query.OrderByDescending(x => x.City).ThenBy(x => x.Name),
            ("city", false) => query.OrderBy(x => x.City).ThenBy(x => x.Name),
            ("createdAt", true) => query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Name),
            ("createdAt", false) => query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Name),
            ("updatedAt", true) => query.OrderByDescending(x => x.UpdatedAtUtc ?? DateTime.MinValue).ThenBy(x => x.Name),
            ("updatedAt", false) => query.OrderBy(x => x.UpdatedAtUtc ?? DateTime.MaxValue).ThenBy(x => x.Name),
            ("active", true) => query.OrderByDescending(x => x.IsActive).ThenBy(x => x.Name),
            ("active", false) => query.OrderBy(x => x.IsActive).ThenBy(x => x.Name),
            ("name", true) => query.OrderByDescending(x => x.Name).ThenBy(x => x.City),
            _ => query.OrderBy(x => x.Name).ThenBy(x => x.City)
        };
    }

    private async Task<(ActionResult? ErrorResult, string? NormalizedOpeningHoursJson)> ValidatePharmacyRequestAsync(
        string name,
        string address,
        string city,
        decimal? locationLatitude,
        decimal? locationLongitude,
        bool isOpen24Hours,
        string? openingHoursJson,
        Guid? pharmacyChainId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (ApiValidationProblem("pharmacy_name_required", "Pharmacy name is required."), null);
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            return (ApiValidationProblem("pharmacy_address_required", "Pharmacy address is required."), null);
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return (ApiValidationProblem("pharmacy_city_required", "Pharmacy city is required."), null);
        }

        if ((locationLatitude.HasValue && !locationLongitude.HasValue) ||
            (!locationLatitude.HasValue && locationLongitude.HasValue))
        {
            return (ApiValidationProblem("pharmacy_coordinates_incomplete", "Latitude and longitude must be provided together."), null);
        }

        if (!isOpen24Hours && string.IsNullOrWhiteSpace(openingHoursJson))
        {
            return (ApiValidationProblem("pharmacy_schedule_required", "Opening hours are required when the pharmacy is not open 24 hours."), null);
        }

        if (!PharmacyDiscoverySupport.TryNormalizeOpeningHoursJson(
                openingHoursJson,
                out var normalizedOpeningHoursJson,
                out var scheduleError))
        {
            return (ApiValidationProblem("pharmacy_schedule_invalid", scheduleError ?? "Opening hours are invalid."), null);
        }

        if (pharmacyChainId.HasValue &&
            !await context.PharmacyChains.AnyAsync(x => x.Id == pharmacyChainId.Value, cancellationToken))
        {
            return (ApiNotFound("pharmacy_chain_not_found", "Pharmacy chain was not found."), null);
        }

        return (null, normalizedOpeningHoursJson);
    }

    private static void SyncLegacyCoordinates(Pharmacy pharmacy)
    {
        pharmacy.Latitude = pharmacy.LocationLatitude?.ToString("0.######", CultureInfo.InvariantCulture);
        pharmacy.Longitude = pharmacy.LocationLongitude?.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private async Task BumpRelevantCachesAsync(CancellationToken cancellationToken)
    {
        await cacheService.BumpScopeVersionAsync(CacheScopes.Dashboard, cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.Users, cancellationToken);
    }

    private static string NormalizeSortBy(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "city" => "city",
            "createdat" => "createdAt",
            "updatedat" => "updatedAt",
            "active" => "active",
            _ => "name"
        };
    }

    private static string NormalizeSortDirection(string? value)
        => string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

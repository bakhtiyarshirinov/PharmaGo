using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaGo.Api.Realtime;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Stocks.Commands.CreateStockItem;
using PharmaGo.Application.Stocks.Commands.UpdateStockItem;
using PharmaGo.Application.Stocks.Queries.GetLowStockAlerts;
using PharmaGo.Application.Stocks.Queries.GetRestockSuggestions;
using PharmaGo.Application.Stocks.Queries.GetStocks;
using PharmaGo.Domain.Models;
using PharmaGo.Infrastructure.Caching;
using PharmaGo.Infrastructure.Auth;

namespace PharmaGo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PolicyNames.ManageInventory)]
public class StocksController(
    IApplicationDbContext context,
    IAppCacheService cacheService,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    RealtimeNotificationService realtimeNotificationService) : ControllerBase
{
    [HttpGet("alerts/low-stock")]
    [ProducesResponseType(typeof(IReadOnlyCollection<LowStockAlertResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<LowStockAlertResponse>>> GetLowStockAlerts(
        [FromQuery] Guid? pharmacyId,
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

        var lowStockItems = await context.StockItems
            .AsNoTracking()
            .Where(x => x.IsActive &&
                (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value) &&
                (x.Quantity - x.ReservedQuantity) <= x.ReorderLevel)
            .OrderBy(x => x.Pharmacy!.Name)
            .ThenBy(x => x.Medicine!.BrandName)
            .Select(x => new LowStockAlertResponse
            {
                StockItemId = x.Id,
                PharmacyId = x.PharmacyId,
                PharmacyName = x.Pharmacy!.Name,
                MedicineId = x.MedicineId,
                MedicineName = x.Medicine!.BrandName,
                GenericName = x.Medicine.GenericName,
                BatchNumber = x.BatchNumber,
                ExpirationDate = x.ExpirationDate,
                Quantity = x.Quantity,
                ReservedQuantity = x.ReservedQuantity,
                AvailableQuantity = x.Quantity - x.ReservedQuantity,
                ReorderLevel = x.ReorderLevel,
                Deficit = x.ReorderLevel - (x.Quantity - x.ReservedQuantity),
                RetailPrice = x.RetailPrice
            })
            .ToListAsync(cancellationToken);

        return Ok(lowStockItems);
    }

    [HttpGet("alerts/restock-suggestions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RestockSuggestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<RestockSuggestionResponse>>> GetRestockSuggestions(
        [FromQuery] Guid? pharmacyId,
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

        var lowStockItems = await context.StockItems
            .AsNoTracking()
            .Where(x => x.IsActive &&
                (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value) &&
                (x.Quantity - x.ReservedQuantity) <= x.ReorderLevel)
            .Select(x => new
            {
                x.Id,
                x.PharmacyId,
                PharmacyName = x.Pharmacy!.Name,
                x.MedicineId,
                MedicineName = x.Medicine!.BrandName,
                x.Medicine.GenericName,
                AvailableQuantity = x.Quantity - x.ReservedQuantity,
                x.ReorderLevel
            })
            .ToListAsync(cancellationToken);

        if (lowStockItems.Count == 0)
        {
            return Ok(Array.Empty<RestockSuggestionResponse>());
        }

        var medicineIds = lowStockItems
            .Select(x => x.MedicineId)
            .Distinct()
            .ToList();

        var suppliers = await context.SupplierMedicines
            .AsNoTracking()
            .Where(x => x.IsAvailable &&
                x.AvailableQuantity > 0 &&
                medicineIds.Contains(x.MedicineId))
            .Select(x => new
            {
                x.MedicineId,
                x.DepotId,
                DepotName = x.Depot!.Name,
                x.AvailableQuantity,
                x.MinimumOrderQuantity,
                x.EstimatedDeliveryHours,
                x.WholesalePrice
            })
            .ToListAsync(cancellationToken);

        var preferredSuppliers = suppliers
            .GroupBy(x => x.MedicineId)
            .ToDictionary(
                x => x.Key,
                x => x
                    .OrderBy(supplier => supplier.WholesalePrice)
                    .ThenBy(supplier => supplier.EstimatedDeliveryHours)
                    .ThenByDescending(supplier => supplier.AvailableQuantity)
                    .First());

        var suggestions = lowStockItems
            .Where(x => preferredSuppliers.ContainsKey(x.MedicineId))
            .Select(x =>
            {
                var supplier = preferredSuppliers[x.MedicineId];
                var deficit = Math.Max(0, x.ReorderLevel - x.AvailableQuantity);
                var requestedQuantity = Math.Max(deficit, supplier.MinimumOrderQuantity);
                var suggestedQuantity = Math.Min(requestedQuantity, supplier.AvailableQuantity);

                return new RestockSuggestionResponse
                {
                    StockItemId = x.Id,
                    PharmacyId = x.PharmacyId,
                    PharmacyName = x.PharmacyName,
                    MedicineId = x.MedicineId,
                    MedicineName = x.MedicineName,
                    GenericName = x.GenericName,
                    AvailableQuantity = x.AvailableQuantity,
                    ReorderLevel = x.ReorderLevel,
                    Deficit = deficit,
                    SuggestedOrderQuantity = suggestedQuantity,
                    DepotId = supplier.DepotId,
                    DepotName = supplier.DepotName,
                    SupplierAvailableQuantity = supplier.AvailableQuantity,
                    MinimumOrderQuantity = supplier.MinimumOrderQuantity,
                    EstimatedDeliveryHours = supplier.EstimatedDeliveryHours,
                    WholesalePrice = supplier.WholesalePrice,
                    EstimatedWholesaleCost = supplier.WholesalePrice * suggestedQuantity
                };
            })
            .Where(x => x.SuggestedOrderQuantity > 0)
            .OrderByDescending(x => x.Deficit)
            .ThenBy(x => x.PharmacyName)
            .ThenBy(x => x.MedicineName)
            .ToList();

        return Ok(suggestions);
    }

    [HttpGet("pharmacy/{pharmacyId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<StockItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<StockItemResponse>>> GetByPharmacy(
        Guid pharmacyId,
        [FromQuery] bool lowStockOnly,
        CancellationToken cancellationToken)
    {
        var accessResult = await EnsurePharmacyAccessAsync(pharmacyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var stockItems = await context.StockItems
            .AsNoTracking()
            .Where(x => x.PharmacyId == pharmacyId &&
                (!lowStockOnly || (x.Quantity - x.ReservedQuantity) <= x.ReorderLevel))
            .OrderBy(x => x.Medicine!.BrandName)
            .ThenBy(x => x.ExpirationDate)
            .Select(x => new StockItemResponse
            {
                Id = x.Id,
                PharmacyId = x.PharmacyId,
                PharmacyName = x.Pharmacy!.Name,
                MedicineId = x.MedicineId,
                MedicineName = x.Medicine!.BrandName,
                GenericName = x.Medicine.GenericName,
                BatchNumber = x.BatchNumber,
                ExpirationDate = x.ExpirationDate,
                Quantity = x.Quantity,
                ReservedQuantity = x.ReservedQuantity,
                AvailableQuantity = x.Quantity - x.ReservedQuantity,
                PurchasePrice = x.PurchasePrice,
                RetailPrice = x.RetailPrice,
                ReorderLevel = x.ReorderLevel,
                IsLowStock = (x.Quantity - x.ReservedQuantity) <= x.ReorderLevel,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(stockItems);
    }

    [HttpPost]
    [ProducesResponseType(typeof(StockItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockItemResponse>> Create(
        [FromBody] CreateStockItemRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateStockRequest(
            request.BatchNumber,
            request.Quantity,
            request.PurchasePrice,
            request.RetailPrice,
            request.ReorderLevel);

        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var accessResult = await EnsurePharmacyAccessAsync(request.PharmacyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var pharmacy = await context.Pharmacies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.PharmacyId && x.IsActive, cancellationToken);
        if (pharmacy is null)
        {
            return NotFound("Pharmacy was not found.");
        }

        var medicine = await context.Medicines
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.MedicineId && x.IsActive, cancellationToken);
        if (medicine is null)
        {
            return NotFound("Medicine was not found.");
        }

        var duplicateBatch = await context.StockItems.AnyAsync(
            x => x.PharmacyId == request.PharmacyId &&
                x.MedicineId == request.MedicineId &&
                x.BatchNumber == request.BatchNumber.Trim(),
            cancellationToken);

        if (duplicateBatch)
        {
            return BadRequest("A stock item with the same pharmacy, medicine and batch number already exists.");
        }

        var stockItem = new StockItem
        {
            PharmacyId = request.PharmacyId,
            MedicineId = request.MedicineId,
            BatchNumber = request.BatchNumber.Trim(),
            ExpirationDate = request.ExpirationDate,
            Quantity = request.Quantity,
            PurchasePrice = request.PurchasePrice,
            RetailPrice = request.RetailPrice,
            ReorderLevel = request.ReorderLevel,
            IsActive = true
        };

        await context.StockItems.AddAsync(stockItem, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.Dashboard, cancellationToken);

        var response = new StockItemResponse
        {
            Id = stockItem.Id,
            PharmacyId = stockItem.PharmacyId,
            PharmacyName = pharmacy.Name,
            MedicineId = stockItem.MedicineId,
            MedicineName = medicine.BrandName,
            GenericName = medicine.GenericName,
            BatchNumber = stockItem.BatchNumber,
            ExpirationDate = stockItem.ExpirationDate,
            Quantity = stockItem.Quantity,
            ReservedQuantity = stockItem.ReservedQuantity,
            AvailableQuantity = stockItem.AvailableQuantity,
            PurchasePrice = stockItem.PurchasePrice,
            RetailPrice = stockItem.RetailPrice,
            ReorderLevel = stockItem.ReorderLevel,
            IsLowStock = stockItem.IsLowStock,
            IsActive = stockItem.IsActive
        };

        await PublishStockAlertIfNeededAsync(response, cancellationToken);
        await auditService.WriteAsync(
            action: "stock.created",
            entityName: "StockItem",
            entityId: stockItem.Id.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: stockItem.PharmacyId,
            description: $"Stock item {stockItem.BatchNumber} created for {medicine.BrandName}.",
            metadata: new
            {
                stockItem.Id,
                stockItem.PharmacyId,
                stockItem.MedicineId,
                stockItem.BatchNumber,
                stockItem.Quantity,
                stockItem.RetailPrice
            },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetByPharmacy), new { pharmacyId = stockItem.PharmacyId }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StockItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockItemResponse>> Update(
        Guid id,
        [FromBody] UpdateStockItemRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateStockRequest(
            request.BatchNumber,
            request.Quantity,
            request.PurchasePrice,
            request.RetailPrice,
            request.ReorderLevel);

        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var stockItem = await context.StockItems
            .Include(x => x.Medicine)
            .Include(x => x.Pharmacy)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (stockItem is null)
        {
            return NotFound();
        }

        var accessResult = await EnsurePharmacyAccessAsync(stockItem.PharmacyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        if (request.Quantity < stockItem.ReservedQuantity)
        {
            return BadRequest("Quantity cannot be lower than the currently reserved quantity.");
        }

        var wasLowStock = stockItem.IsLowStock;

        stockItem.BatchNumber = request.BatchNumber.Trim();
        stockItem.ExpirationDate = request.ExpirationDate;
        stockItem.Quantity = request.Quantity;
        stockItem.PurchasePrice = request.PurchasePrice;
        stockItem.RetailPrice = request.RetailPrice;
        stockItem.ReorderLevel = request.ReorderLevel;
        stockItem.IsActive = request.IsActive;

        await context.SaveChangesAsync(cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.Dashboard, cancellationToken);

        var response = new StockItemResponse
        {
            Id = stockItem.Id,
            PharmacyId = stockItem.PharmacyId,
            PharmacyName = stockItem.Pharmacy!.Name,
            MedicineId = stockItem.MedicineId,
            MedicineName = stockItem.Medicine!.BrandName,
            GenericName = stockItem.Medicine.GenericName,
            BatchNumber = stockItem.BatchNumber,
            ExpirationDate = stockItem.ExpirationDate,
            Quantity = stockItem.Quantity,
            ReservedQuantity = stockItem.ReservedQuantity,
            AvailableQuantity = stockItem.AvailableQuantity,
            PurchasePrice = stockItem.PurchasePrice,
            RetailPrice = stockItem.RetailPrice,
            ReorderLevel = stockItem.ReorderLevel,
            IsLowStock = stockItem.IsLowStock,
            IsActive = stockItem.IsActive
        };

        await PublishStockTransitionAsync(response, wasLowStock, cancellationToken);
        await auditService.WriteAsync(
            action: "stock.updated",
            entityName: "StockItem",
            entityId: stockItem.Id.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: stockItem.PharmacyId,
            description: $"Stock item {stockItem.BatchNumber} updated.",
            metadata: new
            {
                stockItem.Id,
                stockItem.Quantity,
                stockItem.ReservedQuantity,
                stockItem.RetailPrice,
                stockItem.ReorderLevel,
                stockItem.IsActive
            },
            cancellationToken: cancellationToken);

        return Ok(response);
    }

    private async Task<ActionResult?> EnsurePharmacyAccessAsync(Guid pharmacyId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(RoleNames.Moderator))
        {
            return null;
        }

        if (!currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var currentUser = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == currentUserService.UserId.Value, cancellationToken);

        if (currentUser is null || currentUser.PharmacyId != pharmacyId)
        {
            return Forbid();
        }

        return null;
    }

    private static string? ValidateStockRequest(
        string batchNumber,
        int quantity,
        decimal purchasePrice,
        decimal retailPrice,
        int reorderLevel)
    {
        if (string.IsNullOrWhiteSpace(batchNumber))
        {
            return "BatchNumber is required.";
        }

        if (quantity < 0)
        {
            return "Quantity cannot be negative.";
        }

        if (purchasePrice < 0 || retailPrice < 0)
        {
            return "Prices cannot be negative.";
        }

        if (reorderLevel < 0)
        {
            return "ReorderLevel cannot be negative.";
        }

        return null;
    }

    private async Task PublishStockAlertIfNeededAsync(StockItemResponse stockItem, CancellationToken cancellationToken)
    {
        if (!stockItem.IsLowStock)
        {
            return;
        }

        await realtimeNotificationService.NotifyLowStockAsync(stockItem.PharmacyId, new LowStockAlertResponse
        {
            StockItemId = stockItem.Id,
            PharmacyId = stockItem.PharmacyId,
            PharmacyName = stockItem.PharmacyName,
            MedicineId = stockItem.MedicineId,
            MedicineName = stockItem.MedicineName,
            GenericName = stockItem.GenericName,
            BatchNumber = stockItem.BatchNumber,
            ExpirationDate = stockItem.ExpirationDate,
            Quantity = stockItem.Quantity,
            ReservedQuantity = stockItem.ReservedQuantity,
            AvailableQuantity = stockItem.AvailableQuantity,
            ReorderLevel = stockItem.ReorderLevel,
            Deficit = stockItem.ReorderLevel - stockItem.AvailableQuantity,
            RetailPrice = stockItem.RetailPrice
        }, cancellationToken);
    }

    private async Task PublishStockTransitionAsync(
        StockItemResponse stockItem,
        bool wasLowStock,
        CancellationToken cancellationToken)
    {
        if (stockItem.IsLowStock)
        {
            await PublishStockAlertIfNeededAsync(stockItem, cancellationToken);
            return;
        }

        if (wasLowStock)
        {
            await realtimeNotificationService.NotifyStockRestoredAsync(stockItem.PharmacyId, stockItem, cancellationToken);
        }
    }
}

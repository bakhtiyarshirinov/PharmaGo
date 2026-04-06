using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaGo.Api.Realtime;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Stocks.Commands.AdjustStockQuantity;
using PharmaGo.Application.Stocks.Commands.CreateStockItem;
using PharmaGo.Application.Stocks.Commands.ReceiveStock;
using PharmaGo.Application.Stocks.Commands.UpdateStockItem;
using PharmaGo.Application.Stocks.Commands.WriteOffStock;
using PharmaGo.Application.Stocks.Queries.GetExpiringStockAlerts;
using PharmaGo.Application.Stocks.Queries.GetLowStockAlerts;
using PharmaGo.Application.Stocks.Queries.GetOutOfStockAlerts;
using PharmaGo.Application.Stocks.Queries.GetRestockSuggestions;
using PharmaGo.Application.Stocks.Queries.GetStocks;
using PharmaGo.Domain.Models;
using PharmaGo.Infrastructure.Caching;
using PharmaGo.Infrastructure.Auth;

namespace PharmaGo.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize(Policy = PolicyNames.ManageInventory)]
public class StocksController(
    IApplicationDbContext context,
    IAppCacheService cacheService,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    RealtimeNotificationService realtimeNotificationService) : ApiControllerBase
{
    [HttpGet("alerts/low-stock")]
    [ProducesResponseType(typeof(IReadOnlyCollection<LowStockAlertResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<LowStockAlertResponse>>> GetLowStockAlerts(
        [FromQuery] Guid? pharmacyId,
        CancellationToken cancellationToken)
    {
        var scopeResult = await ResolveEffectivePharmacyScopeAsync(pharmacyId, cancellationToken);
        if (scopeResult.Error is not null)
        {
            return scopeResult.Error;
        }

        var effectivePharmacyId = scopeResult.PharmacyId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var lowStockItems = await context.StockItems
            .AsNoTracking()
            .Where(x => x.IsActive &&
                x.ExpirationDate >= today &&
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

    [HttpGet("alerts/out-of-stock")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OutOfStockAlertResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<OutOfStockAlertResponse>>> GetOutOfStockAlerts(
        [FromQuery] Guid? pharmacyId,
        CancellationToken cancellationToken)
    {
        var scopeResult = await ResolveEffectivePharmacyScopeAsync(pharmacyId, cancellationToken);
        if (scopeResult.Error is not null)
        {
            return scopeResult.Error;
        }

        var effectivePharmacyId = scopeResult.PharmacyId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var outOfStockItems = await context.StockItems
            .AsNoTracking()
            .Where(x => x.IsActive &&
                x.ExpirationDate >= today &&
                (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value))
            .GroupBy(x => new
            {
                x.PharmacyId,
                PharmacyName = x.Pharmacy!.Name,
                x.MedicineId,
                MedicineName = x.Medicine!.BrandName,
                x.Medicine.GenericName
            })
            .Select(group => new OutOfStockAlertResponse
            {
                PharmacyId = group.Key.PharmacyId,
                PharmacyName = group.Key.PharmacyName,
                MedicineId = group.Key.MedicineId,
                MedicineName = group.Key.MedicineName,
                GenericName = group.Key.GenericName,
                BatchCount = group.Count(),
                TotalQuantity = group.Sum(x => x.Quantity),
                TotalReservedQuantity = group.Sum(x => x.ReservedQuantity),
                TotalAvailableQuantity = group.Sum(x => x.Quantity - x.ReservedQuantity),
                ReorderLevel = group.Max(x => x.ReorderLevel),
                NearestExpirationDate = group.Min(x => (DateOnly?)x.ExpirationDate),
                LastStockUpdatedAtUtc = group.Max(x => x.LastStockUpdatedAtUtc)
            })
            .Where(x => x.TotalAvailableQuantity <= 0)
            .OrderBy(x => x.PharmacyName)
            .ThenBy(x => x.MedicineName)
            .ToListAsync(cancellationToken);

        return Ok(outOfStockItems);
    }

    [HttpGet("alerts/expiring")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ExpiringStockAlertResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<ExpiringStockAlertResponse>>> GetExpiringAlerts(
        [FromQuery] Guid? pharmacyId,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (days <= 0 || days > 180)
        {
            return ApiValidationProblem("stock_expiring_days_invalid", "Days must be between 1 and 180.");
        }

        var scopeResult = await ResolveEffectivePharmacyScopeAsync(pharmacyId, cancellationToken);
        if (scopeResult.Error is not null)
        {
            return scopeResult.Error;
        }

        var effectivePharmacyId = scopeResult.PharmacyId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var maxDate = today.AddDays(days);

        var expiringRows = await context.StockItems
            .AsNoTracking()
            .Where(x => x.IsActive &&
                x.Quantity > 0 &&
                x.ExpirationDate >= today &&
                x.ExpirationDate <= maxDate &&
                (!effectivePharmacyId.HasValue || x.PharmacyId == effectivePharmacyId.Value))
            .OrderBy(x => x.ExpirationDate)
            .ThenBy(x => x.Pharmacy!.Name)
            .ThenBy(x => x.Medicine!.BrandName)
            .Select(x => new
            {
                x.Id,
                x.PharmacyId,
                PharmacyName = x.Pharmacy!.Name,
                x.MedicineId,
                MedicineName = x.Medicine!.BrandName,
                x.Medicine.GenericName,
                x.BatchNumber,
                x.ExpirationDate,
                x.Quantity,
                x.ReservedQuantity,
                AvailableQuantity = x.Quantity - x.ReservedQuantity,
                x.RetailPrice,
                x.LastStockUpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var expiringItems = expiringRows
            .Select(x => new ExpiringStockAlertResponse
            {
                StockItemId = x.Id,
                PharmacyId = x.PharmacyId,
                PharmacyName = x.PharmacyName,
                MedicineId = x.MedicineId,
                MedicineName = x.MedicineName,
                GenericName = x.GenericName,
                BatchNumber = x.BatchNumber,
                ExpirationDate = x.ExpirationDate,
                DaysUntilExpiration = x.ExpirationDate.DayNumber - today.DayNumber,
                Quantity = x.Quantity,
                ReservedQuantity = x.ReservedQuantity,
                AvailableQuantity = x.AvailableQuantity,
                RetailPrice = x.RetailPrice,
                LastStockUpdatedAtUtc = x.LastStockUpdatedAtUtc
            })
            .ToList();

        return Ok(expiringItems);
    }

    [HttpGet("alerts/restock-suggestions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RestockSuggestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<RestockSuggestionResponse>>> GetRestockSuggestions(
        [FromQuery] Guid? pharmacyId,
        CancellationToken cancellationToken)
    {
        var scopeResult = await ResolveEffectivePharmacyScopeAsync(pharmacyId, cancellationToken);
        if (scopeResult.Error is not null)
        {
            return scopeResult.Error;
        }

        var effectivePharmacyId = scopeResult.PharmacyId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var lowStockItems = await context.StockItems
            .AsNoTracking()
            .Where(x => x.IsActive &&
                x.ExpirationDate >= today &&
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
                IsReservable = x.IsReservable,
                IsLowStock = (x.Quantity - x.ReservedQuantity) <= x.ReorderLevel,
                IsActive = x.IsActive,
                LastStockUpdatedAtUtc = x.LastStockUpdatedAtUtc
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
            request.ExpirationDate,
            request.Quantity,
            request.PurchasePrice,
            request.RetailPrice,
            request.ReorderLevel);

        if (validationError is not null)
        {
            return ApiValidationProblem("stock_validation_error", validationError);
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
            return ApiNotFound("pharmacy_not_found", "Pharmacy was not found.");
        }

        var medicine = await context.Medicines
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.MedicineId && x.IsActive, cancellationToken);
        if (medicine is null)
        {
            return ApiNotFound("medicine_not_found", "Medicine was not found.");
        }

        var duplicateBatch = await context.StockItems.AnyAsync(
            x => x.PharmacyId == request.PharmacyId &&
                x.MedicineId == request.MedicineId &&
                x.BatchNumber == request.BatchNumber.Trim(),
            cancellationToken);

        if (duplicateBatch)
        {
            return ApiConflict("stock_batch_duplicate", "A stock item with the same pharmacy, medicine and batch number already exists.");
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

        stockItem.Pharmacy = pharmacy;
        stockItem.Medicine = medicine;

        var response = MapStockItemResponse(stockItem);

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
                Reason = "Initial stock creation",
                After = CreateStockAuditSnapshot(stockItem),
                Change = new
                {
                    QuantityDelta = stockItem.Quantity,
                    PurchasePriceDelta = stockItem.PurchasePrice,
                    RetailPriceDelta = stockItem.RetailPrice,
                    ReorderLevelDelta = stockItem.ReorderLevel
                }
            },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetByPharmacy), new { pharmacyId = stockItem.PharmacyId }, response);
    }

    [HttpPost("{id:guid}/adjust")]
    [ProducesResponseType(typeof(StockItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockItemResponse>> Adjust(
        Guid id,
        [FromBody] AdjustStockQuantityRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedReason = NormalizeReason(request.Reason);
        if (request.QuantityDelta == 0)
        {
            return ApiValidationProblem("stock_adjustment_invalid", "QuantityDelta must not be zero.");
        }

        if (normalizedReason is null)
        {
            return ApiValidationProblem("stock_adjustment_reason_required", "Reason is required for stock adjustments.");
        }

        var stockItem = await LoadStockItemAsync(id, cancellationToken);
        if (stockItem is null)
        {
            return ApiNotFound("stock_item_not_found", "Stock item was not found.");
        }

        var accessResult = await EnsurePharmacyAccessAsync(stockItem.PharmacyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        var updatedQuantity = stockItem.Quantity + request.QuantityDelta;
        if (updatedQuantity < 0)
        {
            return ApiValidationProblem("stock_quantity_invalid", "Quantity cannot become negative.");
        }

        if (updatedQuantity < stockItem.ReservedQuantity)
        {
            return ApiValidationProblem("stock_quantity_reserved_conflict", "Adjusted quantity cannot be lower than the currently reserved quantity.");
        }

        var wasLowStock = stockItem.IsLowStock;
        var beforeSnapshot = CreateStockAuditSnapshot(stockItem);
        stockItem.Quantity = updatedQuantity;

        return await SaveStockMutationAsync(
            stockItem,
            wasLowStock,
            "stock.adjusted",
            $"Stock item {stockItem.BatchNumber} adjusted by {request.QuantityDelta}.",
            new
            {
                stockItem.Id,
                Reason = normalizedReason,
                Before = beforeSnapshot,
                After = CreateStockAuditSnapshot(stockItem),
                Change = new
                {
                    QuantityDelta = request.QuantityDelta,
                    PurchasePriceDelta = 0m,
                    RetailPriceDelta = 0m,
                    ReorderLevelDelta = 0
                }
            },
            cancellationToken);
    }

    [HttpPost("{id:guid}/receive")]
    [ProducesResponseType(typeof(StockItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockItemResponse>> Receive(
        Guid id,
        [FromBody] ReceiveStockRequest request,
        CancellationToken cancellationToken)
    {
        if (request.QuantityReceived <= 0)
        {
            return ApiValidationProblem("stock_receive_invalid", "QuantityReceived must be greater than zero.");
        }

        var stockItem = await LoadStockItemAsync(id, cancellationToken);
        if (stockItem is null)
        {
            return ApiNotFound("stock_item_not_found", "Stock item was not found.");
        }

        var accessResult = await EnsurePharmacyAccessAsync(stockItem.PharmacyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        if (stockItem.ExpirationDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return ApiValidationProblem("stock_receive_expired_batch", "Cannot receive stock into an expired batch.");
        }

        var wasLowStock = stockItem.IsLowStock;
        var beforeSnapshot = CreateStockAuditSnapshot(stockItem);
        var previousQuantity = stockItem.Quantity;
        var previousPurchasePrice = stockItem.PurchasePrice;
        var previousRetailPrice = stockItem.RetailPrice;
        var previousReorderLevel = stockItem.ReorderLevel;
        var normalizedReason = NormalizeReason(request.Reason);

        stockItem.Quantity += request.QuantityReceived;

        if (request.PurchasePrice.HasValue)
        {
            stockItem.PurchasePrice = request.PurchasePrice.Value;
        }

        if (request.RetailPrice.HasValue)
        {
            stockItem.RetailPrice = request.RetailPrice.Value;
        }

        if (request.ReorderLevel.HasValue)
        {
            stockItem.ReorderLevel = request.ReorderLevel.Value;
        }

        return await SaveStockMutationAsync(
            stockItem,
            wasLowStock,
            "stock.received",
            $"Stock item {stockItem.BatchNumber} received {request.QuantityReceived} units.",
            new
            {
                stockItem.Id,
                Reason = normalizedReason,
                Before = beforeSnapshot,
                After = CreateStockAuditSnapshot(stockItem),
                Change = new
                {
                    QuantityDelta = stockItem.Quantity - previousQuantity,
                    PurchasePriceDelta = stockItem.PurchasePrice - previousPurchasePrice,
                    RetailPriceDelta = stockItem.RetailPrice - previousRetailPrice,
                    ReorderLevelDelta = stockItem.ReorderLevel - previousReorderLevel
                }
            },
            cancellationToken);
    }

    [HttpPost("{id:guid}/writeoff")]
    [ProducesResponseType(typeof(StockItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockItemResponse>> WriteOff(
        Guid id,
        [FromBody] WriteOffStockRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedReason = NormalizeReason(request.Reason);
        if (request.Quantity <= 0)
        {
            return ApiValidationProblem("stock_writeoff_invalid", "Quantity must be greater than zero.");
        }

        if (normalizedReason is null)
        {
            return ApiValidationProblem("stock_writeoff_reason_required", "Reason is required for stock write-off.");
        }

        var stockItem = await LoadStockItemAsync(id, cancellationToken);
        if (stockItem is null)
        {
            return ApiNotFound("stock_item_not_found", "Stock item was not found.");
        }

        var accessResult = await EnsurePharmacyAccessAsync(stockItem.PharmacyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        if (request.Quantity > stockItem.AvailableQuantity)
        {
            return ApiValidationProblem("stock_writeoff_exceeds_available", "Write-off quantity cannot exceed currently available stock.");
        }

        var wasLowStock = stockItem.IsLowStock;
        var beforeSnapshot = CreateStockAuditSnapshot(stockItem);
        var previousQuantity = stockItem.Quantity;
        stockItem.Quantity -= request.Quantity;

        return await SaveStockMutationAsync(
            stockItem,
            wasLowStock,
            "stock.written_off",
            $"Stock item {stockItem.BatchNumber} write-off recorded for {request.Quantity} units.",
            new
            {
                stockItem.Id,
                Reason = normalizedReason,
                Before = beforeSnapshot,
                After = CreateStockAuditSnapshot(stockItem),
                Change = new
                {
                    QuantityDelta = stockItem.Quantity - previousQuantity,
                    PurchasePriceDelta = 0m,
                    RetailPriceDelta = 0m,
                    ReorderLevelDelta = 0
                }
            },
            cancellationToken);
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
            request.ExpirationDate,
            request.Quantity,
            request.PurchasePrice,
            request.RetailPrice,
            request.ReorderLevel);

        if (validationError is not null)
        {
            return ApiValidationProblem("stock_validation_error", validationError);
        }

        var stockItem = await context.StockItems
            .Include(x => x.Medicine)
            .Include(x => x.Pharmacy)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (stockItem is null)
        {
            return ApiNotFound("stock_item_not_found", "Stock item was not found.");
        }

        var accessResult = await EnsurePharmacyAccessAsync(stockItem.PharmacyId, cancellationToken);
        if (accessResult is not null)
        {
            return accessResult;
        }

        if (request.Quantity < stockItem.ReservedQuantity)
        {
            return ApiValidationProblem("stock_quantity_reserved_conflict", "Quantity cannot be lower than the currently reserved quantity.");
        }

        if (!request.IsActive && stockItem.ReservedQuantity > 0)
        {
            return ApiValidationProblem("stock_active_reserved_conflict", "Stock item with reserved quantity cannot be deactivated.");
        }

        var wasLowStock = stockItem.IsLowStock;
        var beforeSnapshot = CreateStockAuditSnapshot(stockItem);

        stockItem.BatchNumber = request.BatchNumber.Trim();
        stockItem.ExpirationDate = request.ExpirationDate;
        stockItem.Quantity = request.Quantity;
        stockItem.PurchasePrice = request.PurchasePrice;
        stockItem.RetailPrice = request.RetailPrice;
        stockItem.ReorderLevel = request.ReorderLevel;
        stockItem.IsActive = request.IsActive;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiConflict("stock_concurrency_conflict", "Stock item changed while updating inventory. Please refresh and try again.");
        }

        await cacheService.BumpScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.Dashboard, cancellationToken);

        var response = MapStockItemResponse(stockItem);

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
                Reason = "Manual stock item update",
                Before = beforeSnapshot,
                After = CreateStockAuditSnapshot(stockItem),
                Change = new
                {
                    QuantityDelta = stockItem.Quantity - beforeSnapshot.Quantity,
                    PurchasePriceDelta = stockItem.PurchasePrice - beforeSnapshot.PurchasePrice,
                    RetailPriceDelta = stockItem.RetailPrice - beforeSnapshot.RetailPrice,
                    ReorderLevelDelta = stockItem.ReorderLevel - beforeSnapshot.ReorderLevel,
                    BatchNumberChanged = !string.Equals(beforeSnapshot.BatchNumber, stockItem.BatchNumber, StringComparison.Ordinal),
                    ExpirationDateChanged = beforeSnapshot.ExpirationDate != stockItem.ExpirationDate,
                    IsActiveChanged = beforeSnapshot.IsActive != stockItem.IsActive
                }
            },
            cancellationToken: cancellationToken);

        return Ok(response);
    }

    private async Task<StockItem?> LoadStockItemAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.StockItems
            .Include(x => x.Medicine)
            .Include(x => x.Pharmacy)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private async Task<ActionResult?> EnsurePharmacyAccessAsync(Guid pharmacyId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(RoleNames.Moderator))
        {
            return null;
        }

        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var currentUser = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == currentUserService.UserId.Value, cancellationToken);

        if (currentUser is null || currentUser.PharmacyId != pharmacyId)
        {
            return ApiForbidden("You do not have access to this pharmacy inventory.");
        }

        return null;
    }

    private async Task<(Guid? PharmacyId, ActionResult? Error)> ResolveEffectivePharmacyScopeAsync(
        Guid? pharmacyId,
        CancellationToken cancellationToken)
    {
        if (pharmacyId.HasValue)
        {
            var accessResult = await EnsurePharmacyAccessAsync(pharmacyId.Value, cancellationToken);
            if (accessResult is not null)
            {
                return (null, accessResult);
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

        return (effectivePharmacyId, null);
    }

    private static string? ValidateStockRequest(
        string batchNumber,
        DateOnly expirationDate,
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

        if (expirationDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return "ExpirationDate cannot be in the past.";
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

    private async Task<ActionResult<StockItemResponse>> SaveStockMutationAsync(
        StockItem stockItem,
        bool wasLowStock,
        string auditAction,
        string description,
        object metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApiConflict("stock_concurrency_conflict", "Stock item changed while updating inventory. Please refresh and try again.");
        }

        await cacheService.BumpScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.Dashboard, cancellationToken);

        var response = MapStockItemResponse(stockItem);

        await PublishStockTransitionAsync(response, wasLowStock, cancellationToken);
        await auditService.WriteAsync(
            action: auditAction,
            entityName: "StockItem",
            entityId: stockItem.Id.ToString(),
            userId: currentUserService.UserId,
            pharmacyId: stockItem.PharmacyId,
            description: description,
            metadata: metadata,
            cancellationToken: cancellationToken);

        return Ok(response);
    }

    private static string? NormalizeReason(string? reason)
    {
        return string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    private static StockAuditSnapshot CreateStockAuditSnapshot(StockItem stockItem)
    {
        return new StockAuditSnapshot(
            stockItem.BatchNumber,
            stockItem.ExpirationDate,
            stockItem.Quantity,
            stockItem.ReservedQuantity,
            stockItem.AvailableQuantity,
            stockItem.PurchasePrice,
            stockItem.RetailPrice,
            stockItem.ReorderLevel,
            stockItem.IsReservable,
            stockItem.IsActive);
    }

    private sealed record StockAuditSnapshot(
        string BatchNumber,
        DateOnly ExpirationDate,
        int Quantity,
        int ReservedQuantity,
        int AvailableQuantity,
        decimal PurchasePrice,
        decimal RetailPrice,
        int ReorderLevel,
        bool IsReservable,
        bool IsActive);

    private static StockItemResponse MapStockItemResponse(StockItem stockItem)
    {
        return new StockItemResponse
        {
            Id = stockItem.Id,
            PharmacyId = stockItem.PharmacyId,
            PharmacyName = stockItem.Pharmacy?.Name ?? string.Empty,
            MedicineId = stockItem.MedicineId,
            MedicineName = stockItem.Medicine?.BrandName ?? string.Empty,
            GenericName = stockItem.Medicine?.GenericName ?? string.Empty,
            BatchNumber = stockItem.BatchNumber,
            ExpirationDate = stockItem.ExpirationDate,
            Quantity = stockItem.Quantity,
            ReservedQuantity = stockItem.ReservedQuantity,
            AvailableQuantity = stockItem.AvailableQuantity,
            PurchasePrice = stockItem.PurchasePrice,
            RetailPrice = stockItem.RetailPrice,
            ReorderLevel = stockItem.ReorderLevel,
            IsReservable = stockItem.IsReservable,
            IsLowStock = stockItem.IsLowStock,
            IsActive = stockItem.IsActive,
            LastStockUpdatedAtUtc = stockItem.LastStockUpdatedAtUtc
        };
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

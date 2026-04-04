using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Medicines.Queries.SearchMedicines;
using PharmaGo.Infrastructure.Caching;

namespace PharmaGo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicinesController(
    IApplicationDbContext context,
    IAppCacheService cacheService) : ControllerBase
{
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MedicineSearchResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MedicineSearchResponse>>> Search(
        [FromQuery] string query,
        [FromQuery] string? city,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Search query is required.");
        }

        var normalizedQuery = query.Trim().ToLower();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var scopeVersion = await cacheService.GetScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);
        var cacheKey = $"medicines:search:v{scopeVersion}:query={normalizedQuery}:city={(city?.Trim().ToLowerInvariant() ?? "all")}";
        var cached = await cacheService.GetAsync<IReadOnlyCollection<MedicineSearchResponse>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Ok(cached);
        }

        var medicines = await context.Medicines
            .AsNoTracking()
            .Where(medicine => medicine.IsActive &&
                (EF.Functions.ILike(medicine.BrandName, $"%{normalizedQuery}%") ||
                 EF.Functions.ILike(medicine.GenericName, $"%{normalizedQuery}%") ||
                 (medicine.Barcode != null && EF.Functions.ILike(medicine.Barcode, $"%{normalizedQuery}%"))))
            .Select(medicine => new MedicineSearchResponse
            {
                MedicineId = medicine.Id,
                BrandName = medicine.BrandName,
                GenericName = medicine.GenericName,
                DosageForm = medicine.DosageForm,
                Strength = medicine.Strength,
                Manufacturer = medicine.Manufacturer,
                RequiresPrescription = medicine.RequiresPrescription,
                TotalAvailableQuantity = medicine.StockItems
                    .Where(stock => stock.IsActive &&
                        stock.ExpirationDate >= today &&
                        stock.Pharmacy != null &&
                        stock.Pharmacy.IsActive &&
                        (city == null || stock.Pharmacy.City == city))
                    .Sum(stock => stock.Quantity - stock.ReservedQuantity),
                MinRetailPrice = medicine.StockItems
                    .Where(stock => stock.IsActive &&
                        stock.ExpirationDate >= today &&
                        stock.Pharmacy != null &&
                        stock.Pharmacy.IsActive &&
                        (city == null || stock.Pharmacy.City == city))
                    .Select(stock => (decimal?)stock.RetailPrice)
                    .Min(),
                Availabilities = medicine.StockItems
                    .Where(stock => stock.IsActive &&
                        stock.ExpirationDate >= today &&
                        stock.Quantity > stock.ReservedQuantity &&
                        stock.Pharmacy != null &&
                        stock.Pharmacy.IsActive &&
                        (city == null || stock.Pharmacy.City == city))
                    .OrderBy(stock => stock.RetailPrice)
                    .Select(stock => new MedicineAvailabilityDto
                    {
                        PharmacyId = stock.PharmacyId,
                        PharmacyName = stock.Pharmacy!.Name,
                        Address = stock.Pharmacy.Address,
                        City = stock.Pharmacy.City,
                        IsOpen24Hours = stock.Pharmacy.IsOpen24Hours,
                        AvailableQuantity = stock.Quantity - stock.ReservedQuantity,
                        RetailPrice = stock.RetailPrice
                    })
                    .ToList()
            })
            .Where(medicine => medicine.TotalAvailableQuantity > 0)
            .OrderBy(medicine => medicine.BrandName)
            .Take(20)
            .ToListAsync(cancellationToken);

        await cacheService.SetAsync(cacheKey, medicines, TimeSpan.FromMinutes(5), cancellationToken);

        return Ok(medicines);
    }
}

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.MasterData.Contracts;
using PharmaGo.Domain.Models;
using PharmaGo.Infrastructure.Auth;
using PharmaGo.Infrastructure.Caching;

namespace PharmaGo.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/master-data")]
[Route("api/admin/master-data")]
[Authorize(Policy = PolicyNames.ManageMasterData)]
public class AdminMasterDataController(
    IApplicationDbContext context,
    IAuditService auditService,
    IAppCacheService cacheService,
    ICurrentUserService currentUserService) : ApiControllerBase
{
    [HttpGet("categories")]
    [ProducesResponseType(typeof(PagedResponse<ManagedMedicineCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ManagedMedicineCategoryResponse>>> GetCategories(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = search?.Trim();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedSortDirection = NormalizeSortDirection(sortDirection);

        var query = context.MedicineCategories
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(normalizedSearch) || EF.Functions.ILike(x.Name, $"%{normalizedSearch}%"))
            .Select(x => new ManagedMedicineCategoryResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                MedicinesCount = x.Medicines.Count
            });

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await (normalizedSortDirection == "desc"
                ? query.OrderByDescending(x => x.Name)
                : query.OrderBy(x => x.Name))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(ToPagedResponse(items, page, pageSize, totalCount, "name", normalizedSortDirection));
    }

    [HttpGet("categories/{id:guid}")]
    [ProducesResponseType(typeof(ManagedMedicineCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagedMedicineCategoryResponse>> GetCategoryById(Guid id, CancellationToken cancellationToken)
    {
        var response = await context.MedicineCategories
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ManagedMedicineCategoryResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                MedicinesCount = x.Medicines.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        return response is null
            ? ApiNotFound("medicine_category_not_found", "Medicine category was not found.")
            : Ok(response);
    }

    [HttpPost("categories")]
    [ProducesResponseType(typeof(ManagedMedicineCategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagedMedicineCategoryResponse>> CreateCategory(
        [FromBody] CreateManagedMedicineCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();
        if (await context.MedicineCategories.AnyAsync(x => x.Name == normalizedName, cancellationToken))
        {
            return ApiConflict("medicine_category_already_exists", "A medicine category with the same name already exists.");
        }

        var category = new MedicineCategory
        {
            Name = normalizedName,
            Description = NormalizeOptional(request.Description)
        };

        await context.MedicineCategories.AddAsync(category, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            action: "medicine_category.created",
            entityName: nameof(MedicineCategory),
            description: $"Medicine category {category.Name} created by moderator.",
            entityId: category.Id.ToString(),
            userId: currentUserService.UserId,
            metadata: new { category.Id, category.Name },
            cancellationToken: cancellationToken);

        var response = new ManagedMedicineCategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            MedicinesCount = 0
        };

        return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, response);
    }

    [HttpPut("categories/{id:guid}")]
    [ProducesResponseType(typeof(ManagedMedicineCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagedMedicineCategoryResponse>> UpdateCategory(
        Guid id,
        [FromBody] UpdateManagedMedicineCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await context.MedicineCategories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null)
        {
            return ApiNotFound("medicine_category_not_found", "Medicine category was not found.");
        }

        var normalizedName = request.Name.Trim();
        if (await context.MedicineCategories.AnyAsync(x => x.Id != id && x.Name == normalizedName, cancellationToken))
        {
            return ApiConflict("medicine_category_already_exists", "A medicine category with the same name already exists.");
        }

        category.Name = normalizedName;
        category.Description = NormalizeOptional(request.Description);

        await context.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            action: "medicine_category.updated",
            entityName: nameof(MedicineCategory),
            description: $"Medicine category {category.Name} updated by moderator.",
            entityId: category.Id.ToString(),
            userId: currentUserService.UserId,
            metadata: new { category.Id, category.Name },
            cancellationToken: cancellationToken);

        var response = await context.MedicineCategories
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ManagedMedicineCategoryResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                MedicinesCount = x.Medicines.Count
            })
            .FirstAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("medicines")]
    [ProducesResponseType(typeof(PagedResponse<ManagedMedicineResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ManagedMedicineResponse>>> GetMedicines(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "brandName",
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = search?.Trim();
        var normalizedSortBy = NormalizeMedicineSortBy(sortBy);
        var normalizedSortDirection = NormalizeSortDirection(sortDirection);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Medicines
            .AsNoTracking()
            .Where(x =>
                (string.IsNullOrWhiteSpace(normalizedSearch) ||
                 EF.Functions.ILike(x.BrandName, $"%{normalizedSearch}%") ||
                 EF.Functions.ILike(x.GenericName, $"%{normalizedSearch}%") ||
                 EF.Functions.ILike(x.Manufacturer, $"%{normalizedSearch}%") ||
                 (x.Barcode != null && EF.Functions.ILike(x.Barcode, $"%{normalizedSearch}%"))) &&
                (!categoryId.HasValue || x.CategoryId == categoryId.Value) &&
                (!isActive.HasValue || x.IsActive == isActive.Value))
            .Select(x => new ManagedMedicineResponse
            {
                Id = x.Id,
                BrandName = x.BrandName,
                GenericName = x.GenericName,
                Description = x.Description,
                DosageForm = x.DosageForm,
                Strength = x.Strength,
                Manufacturer = x.Manufacturer,
                CountryOfOrigin = x.CountryOfOrigin,
                Barcode = x.Barcode,
                RequiresPrescription = x.RequiresPrescription,
                IsActive = x.IsActive,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null,
                StockBatchCount = x.StockItems.Count,
                SupplierOfferCount = x.SupplierMedicines.Count
            });

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await ApplyMedicineSorting(query, normalizedSortBy, normalizedSortDirection)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(ToPagedResponse(items, page, pageSize, totalCount, normalizedSortBy, normalizedSortDirection));
    }

    [HttpGet("medicines/{id:guid}")]
    [ProducesResponseType(typeof(ManagedMedicineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagedMedicineResponse>> GetMedicineById(Guid id, CancellationToken cancellationToken)
    {
        var response = await context.Medicines
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ManagedMedicineResponse
            {
                Id = x.Id,
                BrandName = x.BrandName,
                GenericName = x.GenericName,
                Description = x.Description,
                DosageForm = x.DosageForm,
                Strength = x.Strength,
                Manufacturer = x.Manufacturer,
                CountryOfOrigin = x.CountryOfOrigin,
                Barcode = x.Barcode,
                RequiresPrescription = x.RequiresPrescription,
                IsActive = x.IsActive,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null,
                StockBatchCount = x.StockItems.Count,
                SupplierOfferCount = x.SupplierMedicines.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        return response is null
            ? ApiNotFound("medicine_not_found", "Medicine was not found.")
            : Ok(response);
    }

    [HttpPost("medicines")]
    [ProducesResponseType(typeof(ManagedMedicineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagedMedicineResponse>> CreateMedicine(
        [FromBody] CreateManagedMedicineRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await ValidateMedicineRequestAsync(
            request.CategoryId,
            request.Barcode,
            request.BrandName,
            request.GenericName,
            request.DosageForm,
            request.Strength,
            request.Manufacturer,
            null,
            cancellationToken);

        if (validationResult is not null)
        {
            return validationResult;
        }

        var medicine = new Medicine
        {
            BrandName = request.BrandName.Trim(),
            GenericName = request.GenericName.Trim(),
            Description = NormalizeOptional(request.Description),
            DosageForm = request.DosageForm.Trim(),
            Strength = request.Strength.Trim(),
            Manufacturer = request.Manufacturer.Trim(),
            CountryOfOrigin = NormalizeOptional(request.CountryOfOrigin),
            Barcode = NormalizeOptional(request.Barcode),
            RequiresPrescription = request.RequiresPrescription,
            IsActive = request.IsActive,
            CategoryId = request.CategoryId
        };

        await context.Medicines.AddAsync(medicine, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);

        await auditService.WriteAsync(
            action: "medicine.created",
            entityName: nameof(Medicine),
            description: $"Medicine {medicine.BrandName} created by moderator.",
            entityId: medicine.Id.ToString(),
            userId: currentUserService.UserId,
            metadata: new { medicine.Id, medicine.BrandName, medicine.GenericName, medicine.CategoryId },
            cancellationToken: cancellationToken);

        var response = await context.Medicines
            .AsNoTracking()
            .Where(x => x.Id == medicine.Id)
            .Select(x => new ManagedMedicineResponse
            {
                Id = x.Id,
                BrandName = x.BrandName,
                GenericName = x.GenericName,
                Description = x.Description,
                DosageForm = x.DosageForm,
                Strength = x.Strength,
                Manufacturer = x.Manufacturer,
                CountryOfOrigin = x.CountryOfOrigin,
                Barcode = x.Barcode,
                RequiresPrescription = x.RequiresPrescription,
                IsActive = x.IsActive,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null,
                StockBatchCount = x.StockItems.Count,
                SupplierOfferCount = x.SupplierMedicines.Count
            })
            .FirstAsync(cancellationToken);

        return CreatedAtAction(nameof(GetMedicineById), new { id = medicine.Id }, response);
    }

    [HttpPut("medicines/{id:guid}")]
    [ProducesResponseType(typeof(ManagedMedicineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagedMedicineResponse>> UpdateMedicine(
        Guid id,
        [FromBody] UpdateManagedMedicineRequest request,
        CancellationToken cancellationToken)
    {
        var medicine = await context.Medicines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (medicine is null)
        {
            return ApiNotFound("medicine_not_found", "Medicine was not found.");
        }

        var validationResult = await ValidateMedicineRequestAsync(
            request.CategoryId,
            request.Barcode,
            request.BrandName,
            request.GenericName,
            request.DosageForm,
            request.Strength,
            request.Manufacturer,
            id,
            cancellationToken);

        if (validationResult is not null)
        {
            return validationResult;
        }

        medicine.BrandName = request.BrandName.Trim();
        medicine.GenericName = request.GenericName.Trim();
        medicine.Description = NormalizeOptional(request.Description);
        medicine.DosageForm = request.DosageForm.Trim();
        medicine.Strength = request.Strength.Trim();
        medicine.Manufacturer = request.Manufacturer.Trim();
        medicine.CountryOfOrigin = NormalizeOptional(request.CountryOfOrigin);
        medicine.Barcode = NormalizeOptional(request.Barcode);
        medicine.RequiresPrescription = request.RequiresPrescription;
        medicine.IsActive = request.IsActive;
        medicine.CategoryId = request.CategoryId;

        await context.SaveChangesAsync(cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);

        await auditService.WriteAsync(
            action: "medicine.updated",
            entityName: nameof(Medicine),
            description: $"Medicine {medicine.BrandName} updated by moderator.",
            entityId: medicine.Id.ToString(),
            userId: currentUserService.UserId,
            metadata: new { medicine.Id, medicine.BrandName, medicine.GenericName, medicine.CategoryId, medicine.IsActive },
            cancellationToken: cancellationToken);

        var response = await context.Medicines
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ManagedMedicineResponse
            {
                Id = x.Id,
                BrandName = x.BrandName,
                GenericName = x.GenericName,
                Description = x.Description,
                DosageForm = x.DosageForm,
                Strength = x.Strength,
                Manufacturer = x.Manufacturer,
                CountryOfOrigin = x.CountryOfOrigin,
                Barcode = x.Barcode,
                RequiresPrescription = x.RequiresPrescription,
                IsActive = x.IsActive,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null,
                StockBatchCount = x.StockItems.Count,
                SupplierOfferCount = x.SupplierMedicines.Count
            })
            .FirstAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("pharmacy-chains")]
    [ProducesResponseType(typeof(PagedResponse<ManagedPharmacyChainResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ManagedPharmacyChainResponse>>> GetPharmacyChains(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = search?.Trim();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var normalizedSortDirection = NormalizeSortDirection(sortDirection);

        var query = context.PharmacyChains
            .AsNoTracking()
            .Where(x =>
                (string.IsNullOrWhiteSpace(normalizedSearch) ||
                 EF.Functions.ILike(x.Name, $"%{normalizedSearch}%") ||
                 (x.LegalName != null && EF.Functions.ILike(x.LegalName, $"%{normalizedSearch}%"))) &&
                (!isActive.HasValue || x.IsActive == isActive.Value))
            .Select(x => new ManagedPharmacyChainResponse
            {
                Id = x.Id,
                Name = x.Name,
                LegalName = x.LegalName,
                TaxNumber = x.TaxNumber,
                SupportPhone = x.SupportPhone,
                SupportEmail = x.SupportEmail,
                IsActive = x.IsActive,
                PharmacyCount = x.Pharmacies.Count
            });

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await (normalizedSortDirection == "desc"
                ? query.OrderByDescending(x => x.Name)
                : query.OrderBy(x => x.Name))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(ToPagedResponse(items, page, pageSize, totalCount, "name", normalizedSortDirection));
    }

    [HttpGet("pharmacy-chains/{id:guid}")]
    [ProducesResponseType(typeof(ManagedPharmacyChainResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagedPharmacyChainResponse>> GetPharmacyChainById(Guid id, CancellationToken cancellationToken)
    {
        var response = await context.PharmacyChains
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ManagedPharmacyChainResponse
            {
                Id = x.Id,
                Name = x.Name,
                LegalName = x.LegalName,
                TaxNumber = x.TaxNumber,
                SupportPhone = x.SupportPhone,
                SupportEmail = x.SupportEmail,
                IsActive = x.IsActive,
                PharmacyCount = x.Pharmacies.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        return response is null
            ? ApiNotFound("pharmacy_chain_not_found", "Pharmacy chain was not found.")
            : Ok(response);
    }

    [HttpPost("pharmacy-chains")]
    [ProducesResponseType(typeof(ManagedPharmacyChainResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagedPharmacyChainResponse>> CreatePharmacyChain(
        [FromBody] CreateManagedPharmacyChainRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();
        if (await context.PharmacyChains.AnyAsync(x => x.Name == normalizedName, cancellationToken))
        {
            return ApiConflict("pharmacy_chain_already_exists", "A pharmacy chain with the same name already exists.");
        }

        var chain = new PharmacyChain
        {
            Name = normalizedName,
            LegalName = NormalizeOptional(request.LegalName),
            TaxNumber = NormalizeOptional(request.TaxNumber),
            SupportPhone = NormalizeOptional(request.SupportPhone),
            SupportEmail = NormalizeOptional(request.SupportEmail),
            IsActive = request.IsActive
        };

        await context.PharmacyChains.AddAsync(chain, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            action: "pharmacy_chain.created",
            entityName: nameof(PharmacyChain),
            description: $"Pharmacy chain {chain.Name} created by moderator.",
            entityId: chain.Id.ToString(),
            userId: currentUserService.UserId,
            metadata: new { chain.Id, chain.Name, chain.IsActive },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetPharmacyChainById), new { id = chain.Id }, new ManagedPharmacyChainResponse
        {
            Id = chain.Id,
            Name = chain.Name,
            LegalName = chain.LegalName,
            TaxNumber = chain.TaxNumber,
            SupportPhone = chain.SupportPhone,
            SupportEmail = chain.SupportEmail,
            IsActive = chain.IsActive,
            PharmacyCount = 0
        });
    }

    [HttpPut("pharmacy-chains/{id:guid}")]
    [ProducesResponseType(typeof(ManagedPharmacyChainResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagedPharmacyChainResponse>> UpdatePharmacyChain(
        Guid id,
        [FromBody] UpdateManagedPharmacyChainRequest request,
        CancellationToken cancellationToken)
    {
        var chain = await context.PharmacyChains.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (chain is null)
        {
            return ApiNotFound("pharmacy_chain_not_found", "Pharmacy chain was not found.");
        }

        var normalizedName = request.Name.Trim();
        if (await context.PharmacyChains.AnyAsync(x => x.Id != id && x.Name == normalizedName, cancellationToken))
        {
            return ApiConflict("pharmacy_chain_already_exists", "A pharmacy chain with the same name already exists.");
        }

        chain.Name = normalizedName;
        chain.LegalName = NormalizeOptional(request.LegalName);
        chain.TaxNumber = NormalizeOptional(request.TaxNumber);
        chain.SupportPhone = NormalizeOptional(request.SupportPhone);
        chain.SupportEmail = NormalizeOptional(request.SupportEmail);
        chain.IsActive = request.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            action: "pharmacy_chain.updated",
            entityName: nameof(PharmacyChain),
            description: $"Pharmacy chain {chain.Name} updated by moderator.",
            entityId: chain.Id.ToString(),
            userId: currentUserService.UserId,
            metadata: new { chain.Id, chain.Name, chain.IsActive },
            cancellationToken: cancellationToken);

        var response = await context.PharmacyChains
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ManagedPharmacyChainResponse
            {
                Id = x.Id,
                Name = x.Name,
                LegalName = x.LegalName,
                TaxNumber = x.TaxNumber,
                SupportPhone = x.SupportPhone,
                SupportEmail = x.SupportEmail,
                IsActive = x.IsActive,
                PharmacyCount = x.Pharmacies.Count
            })
            .FirstAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("depots")]
    [ProducesResponseType(typeof(PagedResponse<ManagedDepotResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ManagedDepotResponse>>> GetDepots(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = search?.Trim();
        var normalizedSortBy = NormalizeDepotSortBy(sortBy);
        var normalizedSortDirection = NormalizeSortDirection(sortDirection);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Depots
            .AsNoTracking()
            .Where(x =>
                (string.IsNullOrWhiteSpace(normalizedSearch) ||
                 EF.Functions.ILike(x.Name, $"%{normalizedSearch}%") ||
                 EF.Functions.ILike(x.City, $"%{normalizedSearch}%") ||
                 EF.Functions.ILike(x.Address, $"%{normalizedSearch}%")) &&
                (!isActive.HasValue || x.IsActive == isActive.Value))
            .Select(x => new ManagedDepotResponse
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                City = x.City,
                ContactPhone = x.ContactPhone,
                ContactEmail = x.ContactEmail,
                IsActive = x.IsActive,
                SupplierOfferCount = x.SupplierMedicines.Count
            });

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await ApplyDepotSorting(query, normalizedSortBy, normalizedSortDirection)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(ToPagedResponse(items, page, pageSize, totalCount, normalizedSortBy, normalizedSortDirection));
    }

    [HttpGet("depots/{id:guid}")]
    [ProducesResponseType(typeof(ManagedDepotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagedDepotResponse>> GetDepotById(Guid id, CancellationToken cancellationToken)
    {
        var response = await context.Depots
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ManagedDepotResponse
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                City = x.City,
                ContactPhone = x.ContactPhone,
                ContactEmail = x.ContactEmail,
                IsActive = x.IsActive,
                SupplierOfferCount = x.SupplierMedicines.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        return response is null
            ? ApiNotFound("depot_not_found", "Depot was not found.")
            : Ok(response);
    }

    [HttpPost("depots")]
    [ProducesResponseType(typeof(ManagedDepotResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagedDepotResponse>> CreateDepot(
        [FromBody] CreateManagedDepotRequest request,
        CancellationToken cancellationToken)
    {
        var fieldValidationError = ValidateDepotFields(request.Name, request.Address, request.City, request.ContactPhone);
        if (fieldValidationError is not null)
        {
            return fieldValidationError;
        }

        var normalizedName = request.Name.Trim();
        var normalizedCity = request.City.Trim();
        if (await context.Depots.AnyAsync(x => x.Name == normalizedName && x.City == normalizedCity, cancellationToken))
        {
            return ApiConflict("depot_already_exists", "A depot with the same name and city already exists.");
        }

        var depot = new Depot
        {
            Name = normalizedName,
            Address = request.Address.Trim(),
            City = normalizedCity,
            ContactPhone = NormalizeOptional(request.ContactPhone),
            ContactEmail = NormalizeOptional(request.ContactEmail),
            IsActive = request.IsActive
        };

        await context.Depots.AddAsync(depot, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            action: "depot.created",
            entityName: nameof(Depot),
            description: $"Depot {depot.Name} created by moderator.",
            entityId: depot.Id.ToString(),
            userId: currentUserService.UserId,
            metadata: new { depot.Id, depot.Name, depot.City, depot.IsActive },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetDepotById), new { id = depot.Id }, new ManagedDepotResponse
        {
            Id = depot.Id,
            Name = depot.Name,
            Address = depot.Address,
            City = depot.City,
            ContactPhone = depot.ContactPhone,
            ContactEmail = depot.ContactEmail,
            IsActive = depot.IsActive,
            SupplierOfferCount = 0
        });
    }

    [HttpPut("depots/{id:guid}")]
    [ProducesResponseType(typeof(ManagedDepotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagedDepotResponse>> UpdateDepot(
        Guid id,
        [FromBody] UpdateManagedDepotRequest request,
        CancellationToken cancellationToken)
    {
        var depot = await context.Depots.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (depot is null)
        {
            return ApiNotFound("depot_not_found", "Depot was not found.");
        }

        var fieldValidationError = ValidateDepotFields(request.Name, request.Address, request.City, request.ContactPhone);
        if (fieldValidationError is not null)
        {
            return fieldValidationError;
        }

        var normalizedName = request.Name.Trim();
        var normalizedCity = request.City.Trim();
        if (await context.Depots.AnyAsync(x => x.Id != id && x.Name == normalizedName && x.City == normalizedCity, cancellationToken))
        {
            return ApiConflict("depot_already_exists", "A depot with the same name and city already exists.");
        }

        depot.Name = normalizedName;
        depot.Address = request.Address.Trim();
        depot.City = normalizedCity;
        depot.ContactPhone = NormalizeOptional(request.ContactPhone);
        depot.ContactEmail = NormalizeOptional(request.ContactEmail);
        depot.IsActive = request.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            action: "depot.updated",
            entityName: nameof(Depot),
            description: $"Depot {depot.Name} updated by moderator.",
            entityId: depot.Id.ToString(),
            userId: currentUserService.UserId,
            metadata: new { depot.Id, depot.Name, depot.City, depot.IsActive },
            cancellationToken: cancellationToken);

        var response = await context.Depots
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ManagedDepotResponse
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                City = x.City,
                ContactPhone = x.ContactPhone,
                ContactEmail = x.ContactEmail,
                IsActive = x.IsActive,
                SupplierOfferCount = x.SupplierMedicines.Count
            })
            .FirstAsync(cancellationToken);

        return Ok(response);
    }

    [HttpGet("supplier-medicines")]
    [ProducesResponseType(typeof(PagedResponse<ManagedSupplierMedicineResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ManagedSupplierMedicineResponse>>> GetSupplierMedicines(
        [FromQuery] Guid? depotId,
        [FromQuery] Guid? medicineId,
        [FromQuery] bool? isAvailable,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "medicineName",
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var normalizedSortBy = NormalizeSupplierMedicineSortBy(sortBy);
        var normalizedSortDirection = NormalizeSortDirection(sortDirection);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.SupplierMedicines
            .AsNoTracking()
            .Where(x =>
                (!depotId.HasValue || x.DepotId == depotId.Value) &&
                (!medicineId.HasValue || x.MedicineId == medicineId.Value) &&
                (!isAvailable.HasValue || x.IsAvailable == isAvailable.Value))
            .Select(x => new ManagedSupplierMedicineResponse
            {
                Id = x.Id,
                DepotId = x.DepotId,
                DepotName = x.Depot != null ? x.Depot.Name : string.Empty,
                MedicineId = x.MedicineId,
                MedicineName = x.Medicine != null ? x.Medicine.BrandName : string.Empty,
                GenericName = x.Medicine != null ? x.Medicine.GenericName : string.Empty,
                WholesalePrice = x.WholesalePrice,
                AvailableQuantity = x.AvailableQuantity,
                MinimumOrderQuantity = x.MinimumOrderQuantity,
                EstimatedDeliveryHours = x.EstimatedDeliveryHours,
                IsAvailable = x.IsAvailable
            });

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await ApplySupplierMedicineSorting(query, normalizedSortBy, normalizedSortDirection)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(ToPagedResponse(items, page, pageSize, totalCount, normalizedSortBy, normalizedSortDirection));
    }

    [HttpGet("supplier-medicines/{id:guid}")]
    [ProducesResponseType(typeof(ManagedSupplierMedicineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManagedSupplierMedicineResponse>> GetSupplierMedicineById(Guid id, CancellationToken cancellationToken)
    {
        var response = await context.SupplierMedicines
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ManagedSupplierMedicineResponse
            {
                Id = x.Id,
                DepotId = x.DepotId,
                DepotName = x.Depot != null ? x.Depot.Name : string.Empty,
                MedicineId = x.MedicineId,
                MedicineName = x.Medicine != null ? x.Medicine.BrandName : string.Empty,
                GenericName = x.Medicine != null ? x.Medicine.GenericName : string.Empty,
                WholesalePrice = x.WholesalePrice,
                AvailableQuantity = x.AvailableQuantity,
                MinimumOrderQuantity = x.MinimumOrderQuantity,
                EstimatedDeliveryHours = x.EstimatedDeliveryHours,
                IsAvailable = x.IsAvailable
            })
            .FirstOrDefaultAsync(cancellationToken);

        return response is null
            ? ApiNotFound("supplier_medicine_not_found", "Supplier medicine offer was not found.")
            : Ok(response);
    }

    [HttpPost("supplier-medicines")]
    [ProducesResponseType(typeof(ManagedSupplierMedicineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagedSupplierMedicineResponse>> CreateSupplierMedicine(
        [FromBody] CreateManagedSupplierMedicineRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await ValidateSupplierMedicineRequestAsync(request.DepotId, request.MedicineId, null, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var supplierMedicine = new SupplierMedicine
        {
            DepotId = request.DepotId,
            MedicineId = request.MedicineId,
            WholesalePrice = request.WholesalePrice,
            AvailableQuantity = request.AvailableQuantity,
            MinimumOrderQuantity = request.MinimumOrderQuantity,
            EstimatedDeliveryHours = request.EstimatedDeliveryHours,
            IsAvailable = request.IsAvailable
        };

        await context.SupplierMedicines.AddAsync(supplierMedicine, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            action: "supplier_medicine.created",
            entityName: nameof(SupplierMedicine),
            description: "Supplier medicine offer created by moderator.",
            entityId: supplierMedicine.Id.ToString(),
            userId: currentUserService.UserId,
            metadata: new
            {
                supplierMedicine.Id,
                supplierMedicine.DepotId,
                supplierMedicine.MedicineId,
                supplierMedicine.WholesalePrice,
                supplierMedicine.AvailableQuantity
            },
            cancellationToken: cancellationToken);

        var response = await context.SupplierMedicines
            .AsNoTracking()
            .Where(x => x.Id == supplierMedicine.Id)
            .Select(x => new ManagedSupplierMedicineResponse
            {
                Id = x.Id,
                DepotId = x.DepotId,
                DepotName = x.Depot != null ? x.Depot.Name : string.Empty,
                MedicineId = x.MedicineId,
                MedicineName = x.Medicine != null ? x.Medicine.BrandName : string.Empty,
                GenericName = x.Medicine != null ? x.Medicine.GenericName : string.Empty,
                WholesalePrice = x.WholesalePrice,
                AvailableQuantity = x.AvailableQuantity,
                MinimumOrderQuantity = x.MinimumOrderQuantity,
                EstimatedDeliveryHours = x.EstimatedDeliveryHours,
                IsAvailable = x.IsAvailable
            })
            .FirstAsync(cancellationToken);

        return CreatedAtAction(nameof(GetSupplierMedicineById), new { id = supplierMedicine.Id }, response);
    }

    [HttpPut("supplier-medicines/{id:guid}")]
    [ProducesResponseType(typeof(ManagedSupplierMedicineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManagedSupplierMedicineResponse>> UpdateSupplierMedicine(
        Guid id,
        [FromBody] UpdateManagedSupplierMedicineRequest request,
        CancellationToken cancellationToken)
    {
        var supplierMedicine = await context.SupplierMedicines.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (supplierMedicine is null)
        {
            return ApiNotFound("supplier_medicine_not_found", "Supplier medicine offer was not found.");
        }

        var validationResult = await ValidateSupplierMedicineRequestAsync(request.DepotId, request.MedicineId, id, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        supplierMedicine.DepotId = request.DepotId;
        supplierMedicine.MedicineId = request.MedicineId;
        supplierMedicine.WholesalePrice = request.WholesalePrice;
        supplierMedicine.AvailableQuantity = request.AvailableQuantity;
        supplierMedicine.MinimumOrderQuantity = request.MinimumOrderQuantity;
        supplierMedicine.EstimatedDeliveryHours = request.EstimatedDeliveryHours;
        supplierMedicine.IsAvailable = request.IsAvailable;

        await context.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            action: "supplier_medicine.updated",
            entityName: nameof(SupplierMedicine),
            description: "Supplier medicine offer updated by moderator.",
            entityId: supplierMedicine.Id.ToString(),
            userId: currentUserService.UserId,
            metadata: new
            {
                supplierMedicine.Id,
                supplierMedicine.DepotId,
                supplierMedicine.MedicineId,
                supplierMedicine.WholesalePrice,
                supplierMedicine.AvailableQuantity,
                supplierMedicine.IsAvailable
            },
            cancellationToken: cancellationToken);

        var response = await context.SupplierMedicines
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ManagedSupplierMedicineResponse
            {
                Id = x.Id,
                DepotId = x.DepotId,
                DepotName = x.Depot != null ? x.Depot.Name : string.Empty,
                MedicineId = x.MedicineId,
                MedicineName = x.Medicine != null ? x.Medicine.BrandName : string.Empty,
                GenericName = x.Medicine != null ? x.Medicine.GenericName : string.Empty,
                WholesalePrice = x.WholesalePrice,
                AvailableQuantity = x.AvailableQuantity,
                MinimumOrderQuantity = x.MinimumOrderQuantity,
                EstimatedDeliveryHours = x.EstimatedDeliveryHours,
                IsAvailable = x.IsAvailable
            })
            .FirstAsync(cancellationToken);

        return Ok(response);
    }

    private async Task<ActionResult?> ValidateMedicineRequestAsync(
        Guid? categoryId,
        string? barcode,
        string brandName,
        string genericName,
        string dosageForm,
        string strength,
        string manufacturer,
        Guid? currentMedicineId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(brandName))
        {
            return ApiValidationProblem("medicine_brand_name_required", "Brand name is required.");
        }

        if (string.IsNullOrWhiteSpace(genericName))
        {
            return ApiValidationProblem("medicine_generic_name_required", "Generic name is required.");
        }

        if (string.IsNullOrWhiteSpace(dosageForm))
        {
            return ApiValidationProblem("medicine_dosage_form_required", "Dosage form is required.");
        }

        if (string.IsNullOrWhiteSpace(strength))
        {
            return ApiValidationProblem("medicine_strength_required", "Strength is required.");
        }

        if (string.IsNullOrWhiteSpace(manufacturer))
        {
            return ApiValidationProblem("medicine_manufacturer_required", "Manufacturer is required.");
        }

        if (categoryId.HasValue && !await context.MedicineCategories.AnyAsync(x => x.Id == categoryId.Value, cancellationToken))
        {
            return ApiNotFound("medicine_category_not_found", "Medicine category was not found.");
        }

        var normalizedBarcode = NormalizeOptional(barcode);
        if (!string.IsNullOrWhiteSpace(normalizedBarcode) &&
            await context.Medicines.AnyAsync(x => x.Id != currentMedicineId && x.Barcode == normalizedBarcode, cancellationToken))
        {
            return ApiConflict("medicine_barcode_already_exists", "Another medicine already uses the same barcode.");
        }

        var normalizedBrandName = brandName.Trim();
        var normalizedDosageForm = dosageForm.Trim();
        var normalizedStrength = strength.Trim();
        var normalizedManufacturer = manufacturer.Trim();

        if (await context.Medicines.AnyAsync(
                x => x.Id != currentMedicineId &&
                     x.BrandName == normalizedBrandName &&
                     x.DosageForm == normalizedDosageForm &&
                     x.Strength == normalizedStrength &&
                     x.Manufacturer == normalizedManufacturer,
                cancellationToken))
        {
            return ApiConflict("medicine_already_exists", "A medicine with the same brand, dosage form, strength and manufacturer already exists.");
        }

        return null;
    }

    private static ActionResult? ValidateDepotFields(
        string name,
        string address,
        string city,
        string? contactPhone)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new BadRequestObjectResult(ApiProblemDetailsFactory.CreateValidationProblem("depot_name_required", "Depot name is required."));
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            return new BadRequestObjectResult(ApiProblemDetailsFactory.CreateValidationProblem("depot_address_required", "Depot address is required."));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return new BadRequestObjectResult(ApiProblemDetailsFactory.CreateValidationProblem("depot_city_required", "Depot city is required."));
        }

        var normalizedPhone = NormalizeOptional(contactPhone);
        if (normalizedPhone is not null && normalizedPhone.Length < 7)
        {
            return new BadRequestObjectResult(ApiProblemDetailsFactory.CreateValidationProblem("depot_contact_phone_invalid", "Contact phone must be at least 7 characters long."));
        }

        return null;
    }

    private async Task<ActionResult?> ValidateSupplierMedicineRequestAsync(
        Guid depotId,
        Guid medicineId,
        Guid? currentId,
        CancellationToken cancellationToken)
    {
        if (!await context.Depots.AnyAsync(x => x.Id == depotId, cancellationToken))
        {
            return ApiNotFound("depot_not_found", "Depot was not found.");
        }

        if (!await context.Medicines.AnyAsync(x => x.Id == medicineId, cancellationToken))
        {
            return ApiNotFound("medicine_not_found", "Medicine was not found.");
        }

        if (await context.SupplierMedicines.AnyAsync(
                x => x.Id != currentId && x.DepotId == depotId && x.MedicineId == medicineId,
                cancellationToken))
        {
            return ApiConflict("supplier_medicine_already_exists", "This depot already has a supplier offer for the selected medicine.");
        }

        return null;
    }

    private static PagedResponse<T> ToPagedResponse<T>(
        IReadOnlyCollection<T> items,
        int page,
        int pageSize,
        int totalCount,
        string sortBy,
        string sortDirection)
    {
        return new PagedResponse<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize),
            SortBy = sortBy,
            SortDirection = sortDirection
        };
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeSortDirection(string sortDirection)
        => string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

    private static string NormalizeMedicineSortBy(string sortBy)
    {
        return sortBy.Trim().ToLowerInvariant() switch
        {
            "genericname" => "genericName",
            "manufacturer" => "manufacturer",
            "createdat" => "createdAt",
            _ => "brandName"
        };
    }

    private static IQueryable<ManagedMedicineResponse> ApplyMedicineSorting(
        IQueryable<ManagedMedicineResponse> query,
        string sortBy,
        string sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            ("genericName", "desc") => query.OrderByDescending(x => x.GenericName).ThenByDescending(x => x.BrandName),
            ("genericName", _) => query.OrderBy(x => x.GenericName).ThenBy(x => x.BrandName),
            ("manufacturer", "desc") => query.OrderByDescending(x => x.Manufacturer).ThenByDescending(x => x.BrandName),
            ("manufacturer", _) => query.OrderBy(x => x.Manufacturer).ThenBy(x => x.BrandName),
            ("createdAt", "desc") => query.OrderByDescending(x => x.Id),
            ("createdAt", _) => query.OrderBy(x => x.Id),
            (_, "desc") => query.OrderByDescending(x => x.BrandName).ThenByDescending(x => x.GenericName),
            _ => query.OrderBy(x => x.BrandName).ThenBy(x => x.GenericName)
        };
    }

    private static string NormalizeDepotSortBy(string sortBy)
    {
        return sortBy.Trim().ToLowerInvariant() switch
        {
            "city" => "city",
            _ => "name"
        };
    }

    private static IQueryable<ManagedDepotResponse> ApplyDepotSorting(
        IQueryable<ManagedDepotResponse> query,
        string sortBy,
        string sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            ("city", "desc") => query.OrderByDescending(x => x.City).ThenByDescending(x => x.Name),
            ("city", _) => query.OrderBy(x => x.City).ThenBy(x => x.Name),
            (_, "desc") => query.OrderByDescending(x => x.Name),
            _ => query.OrderBy(x => x.Name)
        };
    }

    private static string NormalizeSupplierMedicineSortBy(string sortBy)
    {
        return sortBy.Trim().ToLowerInvariant() switch
        {
            "depotname" => "depotName",
            "wholesaleprice" => "wholesalePrice",
            _ => "medicineName"
        };
    }

    private static IQueryable<ManagedSupplierMedicineResponse> ApplySupplierMedicineSorting(
        IQueryable<ManagedSupplierMedicineResponse> query,
        string sortBy,
        string sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            ("depotName", "desc") => query.OrderByDescending(x => x.DepotName).ThenByDescending(x => x.MedicineName),
            ("depotName", _) => query.OrderBy(x => x.DepotName).ThenBy(x => x.MedicineName),
            ("wholesalePrice", "desc") => query.OrderByDescending(x => x.WholesalePrice).ThenBy(x => x.MedicineName),
            ("wholesalePrice", _) => query.OrderBy(x => x.WholesalePrice).ThenBy(x => x.MedicineName),
            (_, "desc") => query.OrderByDescending(x => x.MedicineName).ThenByDescending(x => x.DepotName),
            _ => query.OrderBy(x => x.MedicineName).ThenBy(x => x.DepotName)
        };
    }
}

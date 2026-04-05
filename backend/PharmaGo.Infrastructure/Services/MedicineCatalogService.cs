using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Medicines.Queries.GetMedicineDetail;
using PharmaGo.Application.Medicines.Queries.GetMedicineRecommendations;
using PharmaGo.Infrastructure.Caching;

namespace PharmaGo.Infrastructure.Services;

public class MedicineCatalogService(
    IApplicationDbContext context,
    IAppCacheService cacheService) : IMedicineCatalogService
{
    public async Task<MedicineDetailResponse?> GetByIdAsync(
        Guid medicineId,
        CancellationToken cancellationToken = default)
    {
        var medicine = await context.Medicines
            .AsNoTracking()
            .Where(x => x.Id == medicineId && x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.BrandName,
                x.GenericName,
                x.Description,
                x.DosageForm,
                x.Strength,
                x.Manufacturer,
                x.CountryOfOrigin,
                x.Barcode,
                x.RequiresPrescription,
                x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (medicine is null)
        {
            return null;
        }

        var summary = await BuildAvailabilitySummaryQuery()
            .Where(x => x.MedicineId == medicineId)
            .FirstOrDefaultAsync(cancellationToken);

        return new MedicineDetailResponse
        {
            MedicineId = medicine.Id,
            BrandName = medicine.BrandName,
            GenericName = medicine.GenericName,
            Description = medicine.Description,
            DosageForm = medicine.DosageForm,
            Strength = medicine.Strength,
            Manufacturer = medicine.Manufacturer,
            CountryOfOrigin = medicine.CountryOfOrigin,
            Barcode = medicine.Barcode,
            RequiresPrescription = medicine.RequiresPrescription,
            CategoryId = medicine.CategoryId,
            CategoryName = medicine.CategoryName,
            PharmacyCount = summary?.PharmacyCount ?? 0,
            TotalAvailableQuantity = summary?.TotalAvailableQuantity ?? 0,
            MinRetailPrice = summary?.MinRetailPrice,
            HasAvailability = summary is not null && summary.TotalAvailableQuantity > 0
        };
    }

    public async Task<IReadOnlyCollection<MedicineRecommendationResponse>?> GetSubstitutionsAsync(
        Guid medicineId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetMedicineProfileAsync(medicineId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var normalizedLimit = Math.Clamp(limit, 1, 20);
        var scopeVersion = await cacheService.GetScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);
        var cacheKey = $"medicines:substitutions:v{scopeVersion}:id={medicineId}:limit={normalizedLimit}";
        var cached = await cacheService.GetAsync<IReadOnlyCollection<MedicineRecommendationResponse>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var candidates = await GetCandidateRowsAsync(
            medicineId,
            candidate => candidate.GenericName == profile.GenericName &&
                         candidate.DosageForm == profile.DosageForm &&
                         candidate.Strength == profile.Strength &&
                         candidate.RequiresPrescription == profile.RequiresPrescription,
            cancellationToken);

        var recommendations = candidates
            .Select(candidate => new RankedRecommendation(
                candidate.HasAvailability ? 100 : 0,
                candidate.PharmacyCount,
                candidate.MinRetailPrice ?? decimal.MaxValue,
                candidate.BrandName,
                new MedicineRecommendationResponse
                {
                    MedicineId = candidate.MedicineId,
                    BrandName = candidate.BrandName,
                    GenericName = candidate.GenericName,
                    DosageForm = candidate.DosageForm,
                    Strength = candidate.Strength,
                    Manufacturer = candidate.Manufacturer,
                    RequiresPrescription = candidate.RequiresPrescription,
                    CategoryId = candidate.CategoryId,
                    CategoryName = candidate.CategoryName,
                    PharmacyCount = candidate.PharmacyCount,
                    TotalAvailableQuantity = candidate.TotalAvailableQuantity,
                    MinRetailPrice = candidate.MinRetailPrice,
                    HasAvailability = candidate.HasAvailability,
                    MatchReason = "Same generic name, dosage form and strength."
                }))
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.PharmacyCount)
            .ThenBy(x => x.MinRetailPrice)
            .ThenBy(x => x.BrandName)
            .Take(normalizedLimit)
            .Select(x => x.Response)
            .ToList();

        await cacheService.SetAsync(cacheKey, recommendations, TimeSpan.FromMinutes(10), cancellationToken);
        return recommendations;
    }

    public async Task<IReadOnlyCollection<MedicineRecommendationResponse>?> GetSimilarAsync(
        Guid medicineId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetMedicineProfileAsync(medicineId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var normalizedLimit = Math.Clamp(limit, 1, 20);
        var scopeVersion = await cacheService.GetScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);
        var cacheKey = $"medicines:similar:v{scopeVersion}:id={medicineId}:limit={normalizedLimit}";
        var cached = await cacheService.GetAsync<IReadOnlyCollection<MedicineRecommendationResponse>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var candidates = await GetCandidateRowsAsync(
            medicineId,
            candidate =>
                candidate.RequiresPrescription == profile.RequiresPrescription &&
                candidate.GenericName != profile.GenericName &&
                ((profile.CategoryId.HasValue && candidate.CategoryId == profile.CategoryId.Value) ||
                 candidate.DosageForm == profile.DosageForm),
            cancellationToken);

        var recommendations = candidates
            .Select(candidate =>
            {
                var sameCategory = profile.CategoryId.HasValue && candidate.CategoryId == profile.CategoryId;
                var sameDosageForm = candidate.DosageForm == profile.DosageForm;
                var sameStrength = candidate.Strength == profile.Strength;
                var score = (sameCategory ? 6 : 0) +
                            (sameDosageForm ? 3 : 0) +
                            (sameStrength ? 1 : 0) +
                            (candidate.HasAvailability ? 2 : 0);

                return new RankedRecommendation(
                    score,
                    candidate.PharmacyCount,
                    candidate.MinRetailPrice ?? decimal.MaxValue,
                    candidate.BrandName,
                    new MedicineRecommendationResponse
                    {
                        MedicineId = candidate.MedicineId,
                        BrandName = candidate.BrandName,
                        GenericName = candidate.GenericName,
                        DosageForm = candidate.DosageForm,
                        Strength = candidate.Strength,
                        Manufacturer = candidate.Manufacturer,
                        RequiresPrescription = candidate.RequiresPrescription,
                        CategoryId = candidate.CategoryId,
                        CategoryName = candidate.CategoryName,
                        PharmacyCount = candidate.PharmacyCount,
                        TotalAvailableQuantity = candidate.TotalAvailableQuantity,
                        MinRetailPrice = candidate.MinRetailPrice,
                        HasAvailability = candidate.HasAvailability,
                        MatchReason = BuildSimilarMatchReason(sameCategory, sameDosageForm, sameStrength, candidate.CategoryName)
                    });
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.PharmacyCount)
            .ThenBy(x => x.MinRetailPrice)
            .ThenBy(x => x.BrandName)
            .Take(normalizedLimit)
            .Select(x => x.Response)
            .ToList();

        await cacheService.SetAsync(cacheKey, recommendations, TimeSpan.FromMinutes(10), cancellationToken);
        return recommendations;
    }

    private async Task<MedicineProfile?> GetMedicineProfileAsync(
        Guid medicineId,
        CancellationToken cancellationToken)
    {
        return await context.Medicines
            .AsNoTracking()
            .Where(x => x.Id == medicineId && x.IsActive)
            .Select(x => new MedicineProfile
            {
                MedicineId = x.Id,
                GenericName = x.GenericName,
                DosageForm = x.DosageForm,
                Strength = x.Strength,
                RequiresPrescription = x.RequiresPrescription,
                CategoryId = x.CategoryId
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<MedicineAvailabilitySummaryRow> BuildAvailabilitySummaryQuery()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return context.StockItems
            .AsNoTracking()
            .Where(x => x.Medicine != null &&
                x.Medicine.IsActive &&
                x.Pharmacy != null &&
                x.Pharmacy.IsActive &&
                x.IsActive &&
                x.ExpirationDate >= today &&
                x.Quantity > x.ReservedQuantity)
            .GroupBy(x => x.MedicineId)
            .Select(group => new MedicineAvailabilitySummaryRow
            {
                MedicineId = group.Key,
                PharmacyCount = group.Select(x => x.PharmacyId).Distinct().Count(),
                TotalAvailableQuantity = group.Sum(x => x.Quantity - x.ReservedQuantity),
                MinRetailPrice = group.Select(x => (decimal?)x.RetailPrice).Min()
            });
    }

    private async Task<List<CandidateRow>> GetCandidateRowsAsync(
        Guid medicineId,
        Func<CandidateFilterRow, bool> predicate,
        CancellationToken cancellationToken)
    {
        var availabilitySummaries = await BuildAvailabilitySummaryQuery().ToListAsync(cancellationToken);
        var availabilityByMedicine = availabilitySummaries.ToDictionary(x => x.MedicineId, x => x);

        var candidates = await context.Medicines
            .AsNoTracking()
            .Where(x => x.Id != medicineId && x.IsActive)
            .Select(x => new CandidateFilterRow
            {
                MedicineId = x.Id,
                BrandName = x.BrandName,
                GenericName = x.GenericName,
                DosageForm = x.DosageForm,
                Strength = x.Strength,
                Manufacturer = x.Manufacturer,
                RequiresPrescription = x.RequiresPrescription,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : null
            })
            .ToListAsync(cancellationToken);

        return candidates
            .Where(predicate)
            .Select(candidate =>
            {
                availabilityByMedicine.TryGetValue(candidate.MedicineId, out var summary);

                return new CandidateRow
                {
                    MedicineId = candidate.MedicineId,
                    BrandName = candidate.BrandName,
                    GenericName = candidate.GenericName,
                    DosageForm = candidate.DosageForm,
                    Strength = candidate.Strength,
                    Manufacturer = candidate.Manufacturer,
                    RequiresPrescription = candidate.RequiresPrescription,
                    CategoryId = candidate.CategoryId,
                    CategoryName = candidate.CategoryName,
                    PharmacyCount = summary?.PharmacyCount ?? 0,
                    TotalAvailableQuantity = summary?.TotalAvailableQuantity ?? 0,
                    MinRetailPrice = summary?.MinRetailPrice,
                    HasAvailability = summary is not null && summary.TotalAvailableQuantity > 0
                };
            })
            .ToList();
    }

    private static string BuildSimilarMatchReason(
        bool sameCategory,
        bool sameDosageForm,
        bool sameStrength,
        string? categoryName)
    {
        var reasons = new List<string>();

        if (sameCategory && !string.IsNullOrWhiteSpace(categoryName))
        {
            reasons.Add($"Same category: {categoryName}.");
        }
        else if (sameCategory)
        {
            reasons.Add("Same medicine category.");
        }

        if (sameDosageForm)
        {
            reasons.Add("Same dosage form.");
        }

        if (sameStrength)
        {
            reasons.Add("Same strength.");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("Similar therapeutic profile.");
        }

        return string.Join(" ", reasons);
    }

    private sealed class MedicineProfile
    {
        public Guid MedicineId { get; init; }
        public string GenericName { get; init; } = string.Empty;
        public string DosageForm { get; init; } = string.Empty;
        public string Strength { get; init; } = string.Empty;
        public bool RequiresPrescription { get; init; }
        public Guid? CategoryId { get; init; }
    }

    private sealed class MedicineAvailabilitySummaryRow
    {
        public Guid MedicineId { get; init; }
        public int PharmacyCount { get; init; }
        public int TotalAvailableQuantity { get; init; }
        public decimal? MinRetailPrice { get; init; }
    }

    private class CandidateFilterRow
    {
        public Guid MedicineId { get; init; }
        public string BrandName { get; init; } = string.Empty;
        public string GenericName { get; init; } = string.Empty;
        public string DosageForm { get; init; } = string.Empty;
        public string Strength { get; init; } = string.Empty;
        public string Manufacturer { get; init; } = string.Empty;
        public bool RequiresPrescription { get; init; }
        public Guid? CategoryId { get; init; }
        public string? CategoryName { get; init; }
    }

    private sealed class CandidateRow : CandidateFilterRow
    {
        public int PharmacyCount { get; init; }
        public int TotalAvailableQuantity { get; init; }
        public decimal? MinRetailPrice { get; init; }
        public bool HasAvailability { get; init; }
    }

    private sealed record RankedRecommendation(
        int Score,
        int PharmacyCount,
        decimal MinRetailPrice,
        string BrandName,
        MedicineRecommendationResponse Response);
}

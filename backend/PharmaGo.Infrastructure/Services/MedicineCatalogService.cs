using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Medicines.Queries.GetMedicineDetail;

namespace PharmaGo.Infrastructure.Services;

public class MedicineCatalogService(IApplicationDbContext context) : IMedicineCatalogService
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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var summary = await context.StockItems
            .AsNoTracking()
            .Where(x => x.MedicineId == medicineId &&
                x.IsActive &&
                x.ExpirationDate >= today &&
                x.Quantity > x.ReservedQuantity &&
                x.Pharmacy != null &&
                x.Pharmacy.IsActive)
            .GroupBy(x => x.MedicineId)
            .Select(group => new
            {
                PharmacyCount = group.Select(x => x.PharmacyId).Distinct().Count(),
                TotalAvailableQuantity = group.Sum(x => x.Quantity - x.ReservedQuantity),
                MinRetailPrice = group.Select(x => (decimal?)x.RetailPrice).Min()
            })
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
}

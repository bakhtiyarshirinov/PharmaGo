using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Infrastructure.Persistence;

public static class ApplicationDbContextSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var passwordHasher = new PasswordHasher<AppUser>();

        var hasMedicines = await context.Medicines.AnyAsync(cancellationToken);
        if (!hasMedicines)
        {
            var analgesicsCategory = new MedicineCategory
            {
                Name = "Analgesics",
                Description = "Pain relief and fever reducing medicines."
            };

            var antibioticsCategory = new MedicineCategory
            {
                Name = "Antibiotics",
                Description = "Prescription medicines for bacterial infections."
            };

            var chain = new PharmacyChain
            {
                Name = "PharmaGo Care",
                LegalName = "PharmaGo Care LLC",
                SupportPhone = "+994501112233",
                SupportEmail = "support@pharmago.local"
            };

            var centralPharmacy = new Pharmacy
            {
                Name = "PharmaGo Central",
                Address = "28 May Street 15",
                City = "Baku",
                Region = "Nasimi",
                PhoneNumber = "+994501001122",
                Latitude = "40.3777",
                Longitude = "49.8920",
                LocationLatitude = 40.377700m,
                LocationLongitude = 49.892000m,
                OpeningHoursJson = "{\"timeZone\":\"Asia/Baku\",\"weekly\":[{\"day\":\"Mon\",\"open\":\"00:00\",\"close\":\"23:59\"},{\"day\":\"Tue\",\"open\":\"00:00\",\"close\":\"23:59\"},{\"day\":\"Wed\",\"open\":\"00:00\",\"close\":\"23:59\"},{\"day\":\"Thu\",\"open\":\"00:00\",\"close\":\"23:59\"},{\"day\":\"Fri\",\"open\":\"00:00\",\"close\":\"23:59\"},{\"day\":\"Sat\",\"open\":\"00:00\",\"close\":\"23:59\"},{\"day\":\"Sun\",\"open\":\"00:00\",\"close\":\"23:59\"}]}",
                IsOpen24Hours = true,
                SupportsReservations = true,
                HasDelivery = true,
                LastLocationVerifiedAtUtc = DateTime.UtcNow,
                PharmacyChain = chain
            };

            var northPharmacy = new Pharmacy
            {
                Name = "PharmaGo North",
                Address = "Koroglu Rahimov Street 8",
                City = "Baku",
                Region = "Narimanov",
                PhoneNumber = "+994501003344",
                Latitude = "40.4093",
                Longitude = "49.8671",
                LocationLatitude = 40.409300m,
                LocationLongitude = 49.867100m,
                OpeningHoursJson = "{\"timeZone\":\"Asia/Baku\",\"weekly\":[{\"day\":\"Mon\",\"open\":\"09:00\",\"close\":\"22:00\"},{\"day\":\"Tue\",\"open\":\"09:00\",\"close\":\"22:00\"},{\"day\":\"Wed\",\"open\":\"09:00\",\"close\":\"22:00\"},{\"day\":\"Thu\",\"open\":\"09:00\",\"close\":\"22:00\"},{\"day\":\"Fri\",\"open\":\"09:00\",\"close\":\"22:00\"},{\"day\":\"Sat\",\"open\":\"10:00\",\"close\":\"20:00\"},{\"day\":\"Sun\",\"open\":\"10:00\",\"close\":\"18:00\"}]}",
                IsOpen24Hours = false,
                SupportsReservations = true,
                HasDelivery = false,
                LastLocationVerifiedAtUtc = DateTime.UtcNow,
                PharmacyChain = chain
            };

            var depot = new Depot
            {
                Name = "PharmaGo Main Depot",
                Address = "Industrial Zone 4",
                City = "Baku",
                ContactPhone = "+994501005566",
                ContactEmail = "depot@pharmago.local"
            };

            var paracetamol = new Medicine
            {
                BrandName = "Panadol",
                GenericName = "Paracetamol",
                Description = "Pain relief and fever reducer.",
                DosageForm = "Tablet",
                Strength = "500 mg",
                Manufacturer = "GSK",
                CountryOfOrigin = "UK",
                Barcode = "1111111111111",
                RequiresPrescription = false,
                Category = analgesicsCategory
            };

            var ibuprofen = new Medicine
            {
                BrandName = "Nurofen",
                GenericName = "Ibuprofen",
                Description = "Anti-inflammatory pain relief.",
                DosageForm = "Tablet",
                Strength = "200 mg",
                Manufacturer = "Reckitt",
                CountryOfOrigin = "UK",
                Barcode = "2222222222222",
                RequiresPrescription = false,
                Category = analgesicsCategory
            };

            var amoxicillin = new Medicine
            {
                BrandName = "Amoxil",
                GenericName = "Amoxicillin",
                Description = "Broad-spectrum antibiotic.",
                DosageForm = "Capsule",
                Strength = "500 mg",
                Manufacturer = "Sandoz",
                CountryOfOrigin = "Germany",
                Barcode = "3333333333333",
                RequiresPrescription = true,
                Category = antibioticsCategory
            };

            var stockItems = new[]
            {
                new StockItem
                {
                    Pharmacy = centralPharmacy,
                    Medicine = paracetamol,
                    BatchNumber = "PAN-500-A1",
                    ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                    Quantity = 120,
                    ReservedQuantity = 0,
                    PurchasePrice = 1.20m,
                    RetailPrice = 2.50m,
                    ReorderLevel = 20,
                    IsReservable = true,
                    LastStockUpdatedAtUtc = DateTime.UtcNow
                },
                new StockItem
                {
                    Pharmacy = centralPharmacy,
                    Medicine = ibuprofen,
                    BatchNumber = "NUR-200-A1",
                    ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                    Quantity = 75,
                    ReservedQuantity = 0,
                    PurchasePrice = 1.80m,
                    RetailPrice = 3.40m,
                    ReorderLevel = 15,
                    IsReservable = true,
                    LastStockUpdatedAtUtc = DateTime.UtcNow
                },
                new StockItem
                {
                    Pharmacy = northPharmacy,
                    Medicine = paracetamol,
                    BatchNumber = "PAN-500-B1",
                    ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(10)),
                    Quantity = 40,
                    ReservedQuantity = 0,
                    PurchasePrice = 1.10m,
                    RetailPrice = 2.30m,
                    ReorderLevel = 10,
                    IsReservable = true,
                    LastStockUpdatedAtUtc = DateTime.UtcNow
                },
                new StockItem
                {
                    Pharmacy = northPharmacy,
                    Medicine = amoxicillin,
                    BatchNumber = "AMX-500-B1",
                    ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(8)),
                    Quantity = 25,
                    ReservedQuantity = 0,
                    PurchasePrice = 3.50m,
                    RetailPrice = 6.80m,
                    ReorderLevel = 8,
                    IsReservable = true,
                    LastStockUpdatedAtUtc = DateTime.UtcNow
                }
            };

            var supplierMedicines = new[]
            {
                new SupplierMedicine
                {
                    Depot = depot,
                    Medicine = paracetamol,
                    WholesalePrice = 1.00m,
                    AvailableQuantity = 500,
                    MinimumOrderQuantity = 50,
                    EstimatedDeliveryHours = 6
                },
                new SupplierMedicine
                {
                    Depot = depot,
                    Medicine = ibuprofen,
                    WholesalePrice = 1.55m,
                    AvailableQuantity = 300,
                    MinimumOrderQuantity = 30,
                    EstimatedDeliveryHours = 6
                },
                new SupplierMedicine
                {
                    Depot = depot,
                    Medicine = amoxicillin,
                    WholesalePrice = 3.10m,
                    AvailableQuantity = 200,
                    MinimumOrderQuantity = 20,
                    EstimatedDeliveryHours = 8
                }
            };

            await context.MedicineCategories.AddRangeAsync([analgesicsCategory, antibioticsCategory], cancellationToken);
            await context.PharmacyChains.AddAsync(chain, cancellationToken);
            await context.Pharmacies.AddRangeAsync([centralPharmacy, northPharmacy], cancellationToken);
            await context.Depots.AddAsync(depot, cancellationToken);
            await context.Medicines.AddRangeAsync([paracetamol, ibuprofen, amoxicillin], cancellationToken);
            await context.StockItems.AddRangeAsync(stockItems, cancellationToken);
            await context.SupplierMedicines.AddRangeAsync(supplierMedicines, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var pharmacies = await context.Pharmacies.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var central = pharmacies.FirstOrDefault(x => x.Name == "PharmaGo Central");

        var pharmacist = await context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == "+994500000001", cancellationToken);
        if (pharmacist is null)
        {
            pharmacist = new AppUser
            {
                FirstName = "Leyla",
                LastName = "Mammadova",
                PhoneNumber = "+994500000001",
                Email = "pharmacist@pharmago.local",
                Role = UserRole.Pharmacist,
                PharmacyId = central?.Id
            };
            await context.Users.AddAsync(pharmacist, cancellationToken);
        }
        else
        {
            pharmacist.FirstName = "Leyla";
            pharmacist.LastName = "Mammadova";
            pharmacist.Email = "pharmacist@pharmago.local";
            pharmacist.Role = UserRole.Pharmacist;
            pharmacist.PharmacyId = central?.Id;
            pharmacist.IsActive = true;
        }

        pharmacist.PasswordHash = passwordHasher.HashPassword(pharmacist, "Pharmacist123!");

        var moderator = await context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == "+994500000002", cancellationToken);
        if (moderator is null)
        {
            moderator = new AppUser
            {
                FirstName = "Nigar",
                LastName = "Aliyeva",
                PhoneNumber = "+994500000002",
                Email = "moderator@pharmago.local",
                Role = UserRole.Moderator
            };
            await context.Users.AddAsync(moderator, cancellationToken);
        }
        else
        {
            moderator.FirstName = "Nigar";
            moderator.LastName = "Aliyeva";
            moderator.Email = "moderator@pharmago.local";
            moderator.Role = UserRole.Moderator;
            moderator.IsActive = true;
        }

        moderator.PasswordHash = passwordHasher.HashPassword(moderator, "Moderator123!");

        await context.SaveChangesAsync(cancellationToken);
    }
}

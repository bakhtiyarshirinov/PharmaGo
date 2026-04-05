using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Infrastructure.Persistence;

public static class ApplicationDbContextSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var passwordHasher = new PasswordHasher<AppUser>();
        var utcNow = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(utcNow);

        var categoryMap = await EnsureCategoriesAsync(context, cancellationToken);
        var chain = await EnsureChainAsync(context, cancellationToken);
        var pharmacyMap = await EnsurePharmaciesAsync(context, chain, utcNow, cancellationToken);
        var depotMap = await EnsureDepotsAsync(context, cancellationToken);
        var medicineMap = await EnsureMedicinesAsync(context, categoryMap, cancellationToken);

        await EnsureStockItemsAsync(context, pharmacyMap, medicineMap, today, utcNow, cancellationToken);
        await EnsureSupplierMedicinesAsync(context, depotMap, medicineMap, cancellationToken);

        var userMap = await EnsureUsersAsync(context, pharmacyMap, passwordHasher, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        await EnsureShowcaseUserSignalsAsync(context, userMap, medicineMap, pharmacyMap, utcNow, cancellationToken);
        await EnsureShowcaseReservationsAsync(context, userMap, medicineMap, pharmacyMap, utcNow, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, MedicineCategory>> EnsureCategoriesAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new CategorySpec("Analgesics", "Pain relief and fever reducing medicines."),
            new CategorySpec("Antibiotics", "Prescription medicines for bacterial infections."),
            new CategorySpec("Cold & Flu", "Medicines for flu symptoms, congestion and fever."),
            new CategorySpec("Allergy", "Antihistamines and allergy relief products."),
            new CategorySpec("Digestive Care", "Digestive support and stomach medicines."),
            new CategorySpec("Vitamins", "Daily vitamins and immune support products."),
            new CategorySpec("Cardiovascular", "Heart and circulation related medicines.")
        };

        var existing = await context.MedicineCategories.ToDictionaryAsync(x => x.Name, cancellationToken);

        foreach (var spec in specs)
        {
            if (existing.ContainsKey(spec.Name))
            {
                continue;
            }

            var category = new MedicineCategory
            {
                Name = spec.Name,
                Description = spec.Description
            };

            await context.MedicineCategories.AddAsync(category, cancellationToken);
            existing[spec.Name] = category;
        }

        return existing;
    }

    private static async Task<PharmacyChain> EnsureChainAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var chain = await context.PharmacyChains.FirstOrDefaultAsync(x => x.Name == "PharmaGo Care", cancellationToken);
        if (chain is not null)
        {
            chain.LegalName = "PharmaGo Care LLC";
            chain.SupportPhone = "+994501112233";
            chain.SupportEmail = "support@pharmago.local";
            return chain;
        }

        chain = new PharmacyChain
        {
            Name = "PharmaGo Care",
            LegalName = "PharmaGo Care LLC",
            SupportPhone = "+994501112233",
            SupportEmail = "support@pharmago.local"
        };

        await context.PharmacyChains.AddAsync(chain, cancellationToken);
        return chain;
    }

    private static async Task<Dictionary<string, Pharmacy>> EnsurePharmaciesAsync(
        ApplicationDbContext context,
        PharmacyChain chain,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new PharmacySpec("PharmaGo Central", "28 May Street 15", "Baku", "Nasimi", "+994501001122", "40.3777", "49.8920", 40.377700m, 49.892000m, TwentyFourHoursJson(), true, true, true),
            new PharmacySpec("PharmaGo North", "Koroglu Rahimov Street 8", "Baku", "Narimanov", "+994501003344", "40.4093", "49.8671", 40.409300m, 49.867100m, StandardHoursJson("09:00", "22:00", "10:00", "20:00", "10:00", "18:00"), false, true, false),
            new PharmacySpec("PharmaGo Sahil", "Neftchilar Avenue 91", "Baku", "Sabayil", "+994501007755", "40.3695", "49.8464", 40.369500m, 49.846400m, StandardHoursJson("08:00", "23:00", "09:00", "22:00", "09:00", "21:00"), false, true, true),
            new PharmacySpec("PharmaGo Old City", "Icherisheher Gate 3", "Baku", "Sabayil", "+994501009988", "40.3660", "49.8339", 40.366000m, 49.833900m, StandardHoursJson("10:00", "21:00", "10:00", "21:00", "11:00", "20:00"), false, true, false),
            new PharmacySpec("PharmaGo Khatai Express", "Khojali Avenue 37", "Baku", "Khatai", "+994501002244", "40.3887", "49.8724", 40.388700m, 49.872400m, StandardHoursJson("08:00", "00:00", "09:00", "23:00", "10:00", "22:00"), false, true, true),
            new PharmacySpec("PharmaGo Sumqayit", "Samed Vurgun 112", "Sumqayit", "Center", "+994501004488", "40.5897", "49.6686", 40.589700m, 49.668600m, StandardHoursJson("09:00", "22:00", "09:00", "21:00", "10:00", "20:00"), false, true, true)
        };

        var existing = await context.Pharmacies
            .Include(x => x.PharmacyChain)
            .ToDictionaryAsync(x => x.Name, cancellationToken);

        foreach (var spec in specs)
        {
            if (!existing.TryGetValue(spec.Name, out var pharmacy))
            {
                pharmacy = new Pharmacy { Name = spec.Name };
                await context.Pharmacies.AddAsync(pharmacy, cancellationToken);
                existing[spec.Name] = pharmacy;
            }

            pharmacy.Address = spec.Address;
            pharmacy.City = spec.City;
            pharmacy.Region = spec.Region;
            pharmacy.PhoneNumber = spec.PhoneNumber;
            pharmacy.Latitude = spec.Latitude;
            pharmacy.Longitude = spec.Longitude;
            pharmacy.LocationLatitude = spec.LocationLatitude;
            pharmacy.LocationLongitude = spec.LocationLongitude;
            pharmacy.OpeningHoursJson = spec.OpeningHoursJson;
            pharmacy.IsOpen24Hours = spec.IsOpen24Hours;
            pharmacy.SupportsReservations = spec.SupportsReservations;
            pharmacy.HasDelivery = spec.HasDelivery;
            pharmacy.LastLocationVerifiedAtUtc = utcNow;
            pharmacy.IsActive = true;
            pharmacy.PharmacyChain = chain;
        }

        return existing;
    }

    private static async Task<Dictionary<string, Depot>> EnsureDepotsAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new DepotSpec("PharmaGo Main Depot", "Industrial Zone 4", "Baku", "+994501005566", "depot@pharmago.local"),
            new DepotSpec("Caspian Supplier Hub", "Lokbatan Warehouse 12", "Baku", "+994501008899", "caspian@pharmago.local")
        };

        var existing = await context.Depots.ToDictionaryAsync(x => x.Name, cancellationToken);

        foreach (var spec in specs)
        {
            if (!existing.TryGetValue(spec.Name, out var depot))
            {
                depot = new Depot { Name = spec.Name };
                await context.Depots.AddAsync(depot, cancellationToken);
                existing[spec.Name] = depot;
            }

            depot.Address = spec.Address;
            depot.City = spec.City;
            depot.ContactPhone = spec.ContactPhone;
            depot.ContactEmail = spec.ContactEmail;
        }

        return existing;
    }

    private static async Task<Dictionary<string, Medicine>> EnsureMedicinesAsync(
        ApplicationDbContext context,
        Dictionary<string, MedicineCategory> categoryMap,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new MedicineSpec("Panadol", "Paracetamol", "Pain relief and fever reducer.", "Tablet", "500 mg", "GSK", "UK", "1111111111111", false, "Analgesics"),
            new MedicineSpec("Nurofen", "Ibuprofen", "Anti-inflammatory pain relief.", "Tablet", "200 mg", "Reckitt", "UK", "2222222222222", false, "Analgesics"),
            new MedicineSpec("Amoxil", "Amoxicillin", "Broad-spectrum antibiotic.", "Capsule", "500 mg", "Sandoz", "Germany", "3333333333333", true, "Antibiotics"),
            new MedicineSpec("Efferalgan", "Paracetamol", "Fast dissolving pain and fever relief.", "Tablet", "500 mg", "UPSA", "France", "4444444444444", false, "Analgesics"),
            new MedicineSpec("Brufen", "Ibuprofen", "Relief for pain and inflammation.", "Tablet", "200 mg", "Abbott", "Turkey", "5555555555555", false, "Analgesics"),
            new MedicineSpec("Theraflu", "Paracetamol + Pheniramine + Phenylephrine", "Cold and flu symptom relief.", "Powder", "650 mg", "GSK", "USA", "6666666666666", false, "Cold & Flu"),
            new MedicineSpec("Claritin", "Loratadine", "Once daily allergy relief.", "Tablet", "10 mg", "Bayer", "Belgium", "7777777777777", false, "Allergy"),
            new MedicineSpec("Telfast", "Fexofenadine", "Non-drowsy antihistamine.", "Tablet", "120 mg", "Sanofi", "France", "8888888888888", false, "Allergy"),
            new MedicineSpec("Enterol", "Saccharomyces boulardii", "Digestive support for diarrhea recovery.", "Capsule", "250 mg", "Biocodex", "France", "9999999999999", false, "Digestive Care"),
            new MedicineSpec("Omez", "Omeprazole", "Acid reflux and stomach protection.", "Capsule", "20 mg", "Dr. Reddy's", "India", "1212121212121", false, "Digestive Care"),
            new MedicineSpec("Vitrum C", "Vitamin C", "Daily immune support.", "Tablet", "1000 mg", "Unipharm", "USA", "1313131313131", false, "Vitamins"),
            new MedicineSpec("Supradyn", "Multivitamin", "Daily multivitamin formula.", "Tablet", "Complex", "Bayer", "Germany", "1414141414141", false, "Vitamins"),
            new MedicineSpec("Aspirin Cardio", "Acetylsalicylic Acid", "Heart and circulation support.", "Tablet", "100 mg", "Bayer", "Germany", "1515151515151", true, "Cardiovascular")
        };

        var existing = await context.Medicines
            .Include(x => x.Category)
            .ToDictionaryAsync(x => x.BrandName, cancellationToken);

        foreach (var spec in specs)
        {
            if (!existing.TryGetValue(spec.BrandName, out var medicine))
            {
                medicine = new Medicine { BrandName = spec.BrandName };
                await context.Medicines.AddAsync(medicine, cancellationToken);
                existing[spec.BrandName] = medicine;
            }

            medicine.GenericName = spec.GenericName;
            medicine.Description = spec.Description;
            medicine.DosageForm = spec.DosageForm;
            medicine.Strength = spec.Strength;
            medicine.Manufacturer = spec.Manufacturer;
            medicine.CountryOfOrigin = spec.CountryOfOrigin;
            medicine.Barcode = spec.Barcode;
            medicine.RequiresPrescription = spec.RequiresPrescription;
            medicine.IsActive = true;
            medicine.Category = categoryMap[spec.CategoryName];
        }

        return existing;
    }

    private static async Task EnsureStockItemsAsync(
        ApplicationDbContext context,
        Dictionary<string, Pharmacy> pharmacyMap,
        Dictionary<string, Medicine> medicineMap,
        DateOnly today,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new StockSpec("PharmaGo Central", "Panadol", "PAN-500-A1", today.AddYears(1), 120, 0, 1.20m, 2.50m, 20, true),
            new StockSpec("PharmaGo Central", "Nurofen", "NUR-200-A1", today.AddYears(1), 75, 0, 1.80m, 3.40m, 15, true),
            new StockSpec("PharmaGo Central", "Efferalgan", "EFF-500-A1", today.AddMonths(13), 60, 0, 1.25m, 2.70m, 18, true),
            new StockSpec("PharmaGo Central", "Theraflu", "THF-650-A1", today.AddMonths(9), 38, 0, 2.95m, 5.90m, 12, true),
            new StockSpec("PharmaGo Central", "Claritin", "CLR-010-A1", today.AddMonths(11), 52, 0, 2.10m, 4.20m, 12, true),
            new StockSpec("PharmaGo Central", "Vitrum C", "VTC-100-A1", today.AddMonths(10), 44, 0, 2.35m, 4.90m, 12, true),

            new StockSpec("PharmaGo North", "Panadol", "PAN-500-B1", today.AddMonths(10), 40, 0, 1.10m, 2.30m, 10, true),
            new StockSpec("PharmaGo North", "Amoxil", "AMX-500-B1", today.AddMonths(8), 25, 0, 3.50m, 6.80m, 8, true),
            new StockSpec("PharmaGo North", "Brufen", "BRF-200-B1", today.AddMonths(11), 18, 0, 1.70m, 3.20m, 14, true),
            new StockSpec("PharmaGo North", "Enterol", "ENT-250-B1", today.AddMonths(7), 12, 0, 3.10m, 6.20m, 10, true),
            new StockSpec("PharmaGo North", "Omez", "OMZ-020-B1", today.AddMonths(6), 9, 0, 1.80m, 3.60m, 10, true),

            new StockSpec("PharmaGo Sahil", "Panadol", "PAN-500-C1", today.AddMonths(11), 34, 0, 1.18m, 2.45m, 10, true),
            new StockSpec("PharmaGo Sahil", "Nurofen", "NUR-200-C1", today.AddMonths(8), 28, 0, 1.75m, 3.35m, 10, true),
            new StockSpec("PharmaGo Sahil", "Theraflu", "THF-650-C1", today.AddMonths(5), 14, 0, 3.05m, 5.95m, 8, true),
            new StockSpec("PharmaGo Sahil", "Telfast", "TLF-120-C1", today.AddMonths(9), 22, 0, 2.85m, 5.10m, 10, true),
            new StockSpec("PharmaGo Sahil", "Aspirin Cardio", "ASP-100-C1", today.AddMonths(14), 26, 0, 1.40m, 2.90m, 10, true),

            new StockSpec("PharmaGo Old City", "Efferalgan", "EFF-500-D1", today.AddMonths(9), 16, 0, 1.30m, 2.80m, 8, true),
            new StockSpec("PharmaGo Old City", "Claritin", "CLR-010-D1", today.AddMonths(10), 11, 0, 2.20m, 4.30m, 8, true),
            new StockSpec("PharmaGo Old City", "Enterol", "ENT-250-D1", today.AddMonths(4), 7, 0, 3.05m, 6.10m, 8, true),
            new StockSpec("PharmaGo Old City", "Supradyn", "SPR-CPX-D1", today.AddMonths(12), 19, 0, 2.90m, 5.80m, 9, true),

            new StockSpec("PharmaGo Khatai Express", "Panadol", "PAN-500-E1", today.AddMonths(12), 55, 0, 1.15m, 2.40m, 16, true),
            new StockSpec("PharmaGo Khatai Express", "Nurofen", "NUR-200-E1", today.AddMonths(9), 46, 0, 1.78m, 3.38m, 15, true),
            new StockSpec("PharmaGo Khatai Express", "Brufen", "BRF-200-E1", today.AddMonths(9), 31, 0, 1.68m, 3.12m, 12, true),
            new StockSpec("PharmaGo Khatai Express", "Omez", "OMZ-020-E1", today.AddMonths(8), 20, 0, 1.85m, 3.65m, 8, true),
            new StockSpec("PharmaGo Khatai Express", "Vitrum C", "VTC-100-E1", today.AddMonths(6), 6, 0, 2.40m, 4.95m, 8, true),

            new StockSpec("PharmaGo Sumqayit", "Panadol", "PAN-500-F1", today.AddMonths(10), 24, 0, 1.12m, 2.35m, 10, true),
            new StockSpec("PharmaGo Sumqayit", "Theraflu", "THF-650-F1", today.AddMonths(6), 10, 0, 3.00m, 5.85m, 8, true),
            new StockSpec("PharmaGo Sumqayit", "Claritin", "CLR-010-F1", today.AddMonths(7), 8, 0, 2.18m, 4.25m, 8, true),
            new StockSpec("PharmaGo Sumqayit", "Supradyn", "SPR-CPX-F1", today.AddMonths(10), 15, 0, 2.88m, 5.70m, 8, false),
            new StockSpec("PharmaGo Sumqayit", "Aspirin Cardio", "ASP-100-F1", today.AddMonths(5), 5, 0, 1.45m, 2.95m, 8, true)
        };

        var existing = await context.StockItems
            .AsNoTracking()
            .Select(x => x.BatchNumber)
            .ToListAsync(cancellationToken);

        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            if (existingSet.Contains(spec.BatchNumber))
            {
                continue;
            }

            await context.StockItems.AddAsync(new StockItem
            {
                Pharmacy = pharmacyMap[spec.PharmacyName],
                Medicine = medicineMap[spec.MedicineBrandName],
                BatchNumber = spec.BatchNumber,
                ExpirationDate = spec.ExpirationDate,
                Quantity = spec.Quantity,
                ReservedQuantity = spec.ReservedQuantity,
                PurchasePrice = spec.PurchasePrice,
                RetailPrice = spec.RetailPrice,
                ReorderLevel = spec.ReorderLevel,
                IsReservable = spec.IsReservable,
                LastStockUpdatedAtUtc = utcNow,
                ConcurrencyToken = Guid.NewGuid()
            }, cancellationToken);
        }
    }

    private static async Task EnsureSupplierMedicinesAsync(
        ApplicationDbContext context,
        Dictionary<string, Depot> depotMap,
        Dictionary<string, Medicine> medicineMap,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new SupplierSpec("PharmaGo Main Depot", "Panadol", 1.00m, 500, 50, 6),
            new SupplierSpec("PharmaGo Main Depot", "Nurofen", 1.55m, 300, 30, 6),
            new SupplierSpec("PharmaGo Main Depot", "Amoxil", 3.10m, 200, 20, 8),
            new SupplierSpec("PharmaGo Main Depot", "Efferalgan", 1.05m, 240, 30, 6),
            new SupplierSpec("PharmaGo Main Depot", "Brufen", 1.48m, 260, 30, 6),
            new SupplierSpec("PharmaGo Main Depot", "Theraflu", 2.70m, 180, 20, 8),
            new SupplierSpec("PharmaGo Main Depot", "Claritin", 1.90m, 160, 20, 8),
            new SupplierSpec("Caspian Supplier Hub", "Telfast", 2.55m, 140, 20, 12),
            new SupplierSpec("Caspian Supplier Hub", "Enterol", 2.80m, 120, 20, 10),
            new SupplierSpec("Caspian Supplier Hub", "Omez", 1.55m, 180, 20, 10),
            new SupplierSpec("Caspian Supplier Hub", "Vitrum C", 2.05m, 150, 25, 12),
            new SupplierSpec("Caspian Supplier Hub", "Supradyn", 2.55m, 120, 20, 12),
            new SupplierSpec("Caspian Supplier Hub", "Aspirin Cardio", 1.10m, 170, 30, 10)
        };

        var existingPairs = await context.SupplierMedicines
            .AsNoTracking()
            .Select(x => new { x.DepotId, x.MedicineId })
            .ToListAsync(cancellationToken);

        var pairSet = existingPairs.Select(x => $"{x.DepotId}:{x.MedicineId}").ToHashSet();

        foreach (var spec in specs)
        {
            var depot = depotMap[spec.DepotName];
            var medicine = medicineMap[spec.MedicineBrandName];
            var key = $"{depot.Id}:{medicine.Id}";

            if (pairSet.Contains(key))
            {
                continue;
            }

            await context.SupplierMedicines.AddAsync(new SupplierMedicine
            {
                Depot = depot,
                Medicine = medicine,
                WholesalePrice = spec.WholesalePrice,
                AvailableQuantity = spec.AvailableQuantity,
                MinimumOrderQuantity = spec.MinimumOrderQuantity,
                EstimatedDeliveryHours = spec.EstimatedDeliveryHours,
                IsAvailable = true
            }, cancellationToken);
        }
    }

    private static async Task<Dictionary<string, AppUser>> EnsureUsersAsync(
        ApplicationDbContext context,
        Dictionary<string, Pharmacy> pharmacyMap,
        PasswordHasher<AppUser> passwordHasher,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new UserSpec("Leyla", "Mammadova", "+994500000001", "pharmacist@pharmago.local", UserRole.Pharmacist, "PharmaGo Central", "Pharmacist123!"),
            new UserSpec("Nigar", "Aliyeva", "+994500000002", "moderator@pharmago.local", UserRole.Moderator, null, "Moderator123!"),
            new UserSpec("Aylin", "Hasanova", "+994500000101", "aylin@pharmago.local", UserRole.User, null, "User12345!"),
            new UserSpec("Murad", "Karimov", "+994500000102", "murad@pharmago.local", UserRole.User, null, "User12345!"),
            new UserSpec("Kamran", "Jafarov", "+994500000103", "kamran@pharmago.local", UserRole.User, null, "User12345!"),
            new UserSpec("Rena", "Safarova", "+994500000104", "rena@pharmago.local", UserRole.User, null, "User12345!"),
            new UserSpec("Farid", "Abbasov", "+994500000201", "khatai.pharmacist@pharmago.local", UserRole.Pharmacist, "PharmaGo Khatai Express", "Pharmacist123!")
        };

        var existing = await context.Users.ToDictionaryAsync(x => x.PhoneNumber, cancellationToken);

        foreach (var spec in specs)
        {
            if (!existing.TryGetValue(spec.PhoneNumber, out var user))
            {
                user = new AppUser
                {
                    PhoneNumber = spec.PhoneNumber
                };
                await context.Users.AddAsync(user, cancellationToken);
                existing[spec.PhoneNumber] = user;
            }

            user.FirstName = spec.FirstName;
            user.LastName = spec.LastName;
            user.Email = spec.Email;
            user.Role = spec.Role;
            user.PharmacyId = spec.PharmacyName is not null ? pharmacyMap[spec.PharmacyName].Id : null;
            user.IsActive = true;
            user.PasswordHash = passwordHasher.HashPassword(user, spec.Password);
        }

        return existing;
    }

    private static async Task EnsureShowcaseUserSignalsAsync(
        ApplicationDbContext context,
        Dictionary<string, AppUser> userMap,
        Dictionary<string, Medicine> medicineMap,
        Dictionary<string, Pharmacy> pharmacyMap,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var favoriteMedicinePairs = new[]
        {
            (UserPhone: "+994500000101", Medicine: "Nurofen"),
            (UserPhone: "+994500000101", Medicine: "Theraflu"),
            (UserPhone: "+994500000102", Medicine: "Panadol"),
            (UserPhone: "+994500000102", Medicine: "Claritin"),
            (UserPhone: "+994500000103", Medicine: "Efferalgan"),
            (UserPhone: "+994500000104", Medicine: "Omez")
        };

        foreach (var pair in favoriteMedicinePairs)
        {
            var user = userMap[pair.UserPhone];
            var medicine = medicineMap[pair.Medicine];
            var exists = await context.UserFavoriteMedicines.AnyAsync(
                x => x.UserId == user.Id && x.MedicineId == medicine.Id,
                cancellationToken);

            if (!exists)
            {
                await context.UserFavoriteMedicines.AddAsync(new UserFavoriteMedicine
                {
                    UserId = user.Id,
                    MedicineId = medicine.Id
                }, cancellationToken);
            }
        }

        var favoritePharmacyPairs = new[]
        {
            (UserPhone: "+994500000101", Pharmacy: "PharmaGo Central"),
            (UserPhone: "+994500000102", Pharmacy: "PharmaGo Sahil"),
            (UserPhone: "+994500000103", Pharmacy: "PharmaGo Khatai Express"),
            (UserPhone: "+994500000104", Pharmacy: "PharmaGo Old City")
        };

        foreach (var pair in favoritePharmacyPairs)
        {
            var user = userMap[pair.UserPhone];
            var pharmacy = pharmacyMap[pair.Pharmacy];
            var exists = await context.UserFavoritePharmacies.AnyAsync(
                x => x.UserId == user.Id && x.PharmacyId == pharmacy.Id,
                cancellationToken);

            if (!exists)
            {
                await context.UserFavoritePharmacies.AddAsync(new UserFavoritePharmacy
                {
                    UserId = user.Id,
                    PharmacyId = pharmacy.Id
                }, cancellationToken);
            }
        }

        var medicineViews = new[]
        {
            new UserMedicineViewSpec("+994500000101", "Nurofen", utcNow.AddHours(-4), 6),
            new UserMedicineViewSpec("+994500000101", "Theraflu", utcNow.AddHours(-9), 4),
            new UserMedicineViewSpec("+994500000102", "Panadol", utcNow.AddHours(-7), 5),
            new UserMedicineViewSpec("+994500000102", "Claritin", utcNow.AddHours(-2), 3),
            new UserMedicineViewSpec("+994500000103", "Efferalgan", utcNow.AddHours(-3), 4),
            new UserMedicineViewSpec("+994500000104", "Omez", utcNow.AddHours(-6), 2)
        };

        foreach (var spec in medicineViews)
        {
            var user = userMap[spec.UserPhone];
            var medicine = medicineMap[spec.MedicineBrandName];
            var existing = await context.UserMedicineViews.FirstOrDefaultAsync(
                x => x.UserId == user.Id && x.MedicineId == medicine.Id,
                cancellationToken);

            if (existing is null)
            {
                await context.UserMedicineViews.AddAsync(new UserMedicineView
                {
                    UserId = user.Id,
                    MedicineId = medicine.Id,
                    LastViewedAtUtc = spec.LastViewedAtUtc,
                    ViewCount = spec.ViewCount
                }, cancellationToken);
            }
        }

        var pharmacyViews = new[]
        {
            new UserPharmacyViewSpec("+994500000101", "PharmaGo Central", utcNow.AddHours(-5), 4),
            new UserPharmacyViewSpec("+994500000102", "PharmaGo Sahil", utcNow.AddHours(-8), 3),
            new UserPharmacyViewSpec("+994500000103", "PharmaGo Khatai Express", utcNow.AddHours(-2), 5),
            new UserPharmacyViewSpec("+994500000104", "PharmaGo Old City", utcNow.AddHours(-10), 2)
        };

        foreach (var spec in pharmacyViews)
        {
            var user = userMap[spec.UserPhone];
            var pharmacy = pharmacyMap[spec.PharmacyName];
            var existing = await context.UserPharmacyViews.FirstOrDefaultAsync(
                x => x.UserId == user.Id && x.PharmacyId == pharmacy.Id,
                cancellationToken);

            if (existing is null)
            {
                await context.UserPharmacyViews.AddAsync(new UserPharmacyView
                {
                    UserId = user.Id,
                    PharmacyId = pharmacy.Id,
                    LastViewedAtUtc = spec.LastViewedAtUtc,
                    ViewCount = spec.ViewCount
                }, cancellationToken);
            }
        }
    }

    private static async Task EnsureShowcaseReservationsAsync(
        ApplicationDbContext context,
        Dictionary<string, AppUser> userMap,
        Dictionary<string, Medicine> medicineMap,
        Dictionary<string, Pharmacy> pharmacyMap,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new ReservationSeedSpec("PG-DEMO-1001", "+994500000101", "PharmaGo Central", ReservationStatus.Pending, utcNow.AddHours(3), null, null, null, null, 6.80m, new[] { new ReservationItemSeedSpec("Nurofen", 2, 3.40m) }),
            new ReservationSeedSpec("PG-DEMO-1002", "+994500000102", "PharmaGo Sahil", ReservationStatus.Confirmed, utcNow.AddHours(4), utcNow.AddMinutes(-40), null, null, null, 5.95m, new[] { new ReservationItemSeedSpec("Theraflu", 1, 5.95m) }),
            new ReservationSeedSpec("PG-DEMO-1003", "+994500000103", "PharmaGo Khatai Express", ReservationStatus.ReadyForPickup, utcNow.AddHours(5), utcNow.AddHours(-1), utcNow.AddMinutes(-15), null, null, 4.80m, new[] { new ReservationItemSeedSpec("Panadol", 2, 2.40m) }),
            new ReservationSeedSpec("PG-DEMO-1004", "+994500000104", "PharmaGo Old City", ReservationStatus.Completed, utcNow.AddDays(-1), utcNow.AddDays(-1).AddHours(-2), utcNow.AddDays(-1).AddHours(-1), utcNow.AddDays(-1), null, 6.10m, new[] { new ReservationItemSeedSpec("Enterol", 1, 6.10m) }),
            new ReservationSeedSpec("PG-DEMO-1005", "+994500000101", "PharmaGo Sumqayit", ReservationStatus.Cancelled, utcNow.AddHours(-6), utcNow.AddHours(-8), null, null, utcNow.AddHours(-5), 4.25m, new[] { new ReservationItemSeedSpec("Claritin", 1, 4.25m) })
        };

        foreach (var spec in specs)
        {
            var reservation = await context.Reservations
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.ReservationNumber == spec.ReservationNumber, cancellationToken);

            if (reservation is not null)
            {
                continue;
            }

            reservation = new Reservation
            {
                ReservationNumber = spec.ReservationNumber,
                CustomerId = userMap[spec.UserPhone].Id,
                PharmacyId = pharmacyMap[spec.PharmacyName].Id,
                Status = spec.Status,
                ReservedUntilUtc = spec.ReservedUntilUtc,
                ConfirmedAtUtc = spec.ConfirmedAtUtc,
                ReadyForPickupAtUtc = spec.ReadyForPickupAtUtc,
                CompletedAtUtc = spec.CompletedAtUtc,
                CancelledAtUtc = spec.CancelledAtUtc,
                ExpiredAtUtc = spec.Status == ReservationStatus.Expired ? utcNow : null,
                TotalAmount = spec.TotalAmount,
                Notes = "Mock showcase reservation",
                ConcurrencyToken = Guid.NewGuid()
            };

            foreach (var itemSpec in spec.Items)
            {
                reservation.Items.Add(new ReservationItem
                {
                    MedicineId = medicineMap[itemSpec.MedicineBrandName].Id,
                    Quantity = itemSpec.Quantity,
                    UnitPrice = itemSpec.UnitPrice
                });
            }

            await context.Reservations.AddAsync(reservation, cancellationToken);
        }
    }

    private static string TwentyFourHoursJson()
    {
        return "{\"timeZone\":\"Asia/Baku\",\"weekly\":[{\"day\":\"Mon\",\"open\":\"00:00\",\"close\":\"23:59\"},{\"day\":\"Tue\",\"open\":\"00:00\",\"close\":\"23:59\"},{\"day\":\"Wed\",\"open\":\"00:00\",\"close\":\"23:59\"},{\"day\":\"Thu\",\"open\":\"00:00\",\"close\":\"23:59\"},{\"day\":\"Fri\",\"open\":\"00:00\",\"close\":\"23:59\"},{\"day\":\"Sat\",\"open\":\"00:00\",\"close\":\"23:59\"},{\"day\":\"Sun\",\"open\":\"00:00\",\"close\":\"23:59\"}]}";
    }

    private static string StandardHoursJson(
        string weekOpen,
        string weekClose,
        string saturdayOpen,
        string saturdayClose,
        string sundayOpen,
        string sundayClose)
    {
        return $$"""
        {"timeZone":"Asia/Baku","weekly":[{"day":"Mon","open":"{{weekOpen}}","close":"{{weekClose}}"},{"day":"Tue","open":"{{weekOpen}}","close":"{{weekClose}}"},{"day":"Wed","open":"{{weekOpen}}","close":"{{weekClose}}"},{"day":"Thu","open":"{{weekOpen}}","close":"{{weekClose}}"},{"day":"Fri","open":"{{weekOpen}}","close":"{{weekClose}}"},{"day":"Sat","open":"{{saturdayOpen}}","close":"{{saturdayClose}}"},{"day":"Sun","open":"{{sundayOpen}}","close":"{{sundayClose}}"}]}
        """;
    }

    private sealed record CategorySpec(string Name, string Description);

    private sealed record PharmacySpec(
        string Name,
        string Address,
        string City,
        string Region,
        string PhoneNumber,
        string Latitude,
        string Longitude,
        decimal LocationLatitude,
        decimal LocationLongitude,
        string OpeningHoursJson,
        bool IsOpen24Hours,
        bool SupportsReservations,
        bool HasDelivery);

    private sealed record DepotSpec(string Name, string Address, string City, string ContactPhone, string ContactEmail);

    private sealed record MedicineSpec(
        string BrandName,
        string GenericName,
        string Description,
        string DosageForm,
        string Strength,
        string Manufacturer,
        string CountryOfOrigin,
        string Barcode,
        bool RequiresPrescription,
        string CategoryName);

    private sealed record StockSpec(
        string PharmacyName,
        string MedicineBrandName,
        string BatchNumber,
        DateOnly ExpirationDate,
        int Quantity,
        int ReservedQuantity,
        decimal PurchasePrice,
        decimal RetailPrice,
        int ReorderLevel,
        bool IsReservable);

    private sealed record SupplierSpec(
        string DepotName,
        string MedicineBrandName,
        decimal WholesalePrice,
        int AvailableQuantity,
        int MinimumOrderQuantity,
        int EstimatedDeliveryHours);

    private sealed record UserSpec(
        string FirstName,
        string LastName,
        string PhoneNumber,
        string Email,
        UserRole Role,
        string? PharmacyName,
        string Password);

    private sealed record UserMedicineViewSpec(string UserPhone, string MedicineBrandName, DateTime LastViewedAtUtc, int ViewCount);

    private sealed record UserPharmacyViewSpec(string UserPhone, string PharmacyName, DateTime LastViewedAtUtc, int ViewCount);

    private sealed record ReservationSeedSpec(
        string ReservationNumber,
        string UserPhone,
        string PharmacyName,
        ReservationStatus Status,
        DateTime ReservedUntilUtc,
        DateTime? ConfirmedAtUtc,
        DateTime? ReadyForPickupAtUtc,
        DateTime? CompletedAtUtc,
        DateTime? CancelledAtUtc,
        decimal TotalAmount,
        IReadOnlyCollection<ReservationItemSeedSpec> Items);

    private sealed record ReservationItemSeedSpec(string MedicineBrandName, int Quantity, decimal UnitPrice);
}

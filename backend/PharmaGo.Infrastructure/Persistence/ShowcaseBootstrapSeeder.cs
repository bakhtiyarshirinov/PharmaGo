using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Infrastructure.Persistence;

public static class ShowcaseBootstrapSeeder
{
    public const string SharedStaffPassword = "Pharmacist123!";
    public const string SharedCustomerPassword = "User12345!";
    public const string ModeratorPhoneNumber = PharmacistSmokeBootstrapSeeder.ModeratorPhoneNumber;
    public const string ModeratorPassword = PharmacistSmokeBootstrapSeeder.ModeratorPassword;

    public static async Task<ShowcaseBootstrapResult> SeedAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                context.ChangeTracker.Clear();
                return await SeedCoreAsync(context, cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                context.ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken);
            }
        }

        context.ChangeTracker.Clear();
        return await SeedCoreAsync(context, cancellationToken);
    }

    private static async Task<ShowcaseBootstrapResult> SeedCoreAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var passwordHasher = new PasswordHasher<AppUser>();
        var utcNow = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(utcNow);

        var categories = await EnsureCategoriesAsync(context, cancellationToken);
        var medicines = await EnsureMedicinesAsync(context, categories, cancellationToken);
        var pharmacies = await EnsurePharmaciesAsync(context, utcNow, cancellationToken);
        var moderator = await EnsureUserAsync(
            context,
            passwordHasher,
            "Nigar",
            "Moderatorova",
            ModeratorPhoneNumber,
            "showcase.moderator@pharmago.local",
            UserRole.Moderator,
            null,
            ModeratorPassword,
            cancellationToken);
        var pharmacists = await EnsurePharmacistsAsync(context, passwordHasher, pharmacies, cancellationToken);
        var customers = await EnsureCustomersAsync(context, passwordHasher, cancellationToken);

        foreach (var pharmacist in pharmacists.Values)
        {
            await EnsureNotificationPreferenceAsync(context, pharmacist.Id, cancellationToken);
        }

        foreach (var customer in customers.Values)
        {
            await EnsureNotificationPreferenceAsync(context, customer.Id, cancellationToken);
        }

        var stockItems = await EnsureStockItemsAsync(context, pharmacies, medicines, today, utcNow, cancellationToken);
        var reservations = await EnsureReservationsAsync(context, customers, pharmacies, medicines, utcNow, cancellationToken);

        ReconcileReservedQuantities(stockItems, reservations);
        await context.SaveChangesAsync(cancellationToken);

        await EnsureTimelineAsync(context, pharmacists, reservations, cancellationToken);
        await EnsureNotificationsAsync(context, pharmacists, customers, reservations, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new ShowcaseBootstrapResult(
            pharmacies.Values
                .OrderBy(x => x.Name)
                .Select(x => new ShowcasePharmacyCredential(
                    x.Name,
                    x.City,
                    x.Id,
                    pharmacists[x.Name].FirstName + " " + pharmacists[x.Name].LastName,
                    pharmacists[x.Name].PhoneNumber,
                    SharedStaffPassword))
                .ToArray(),
            customers.Values
                .OrderBy(x => x.FirstName)
                .Select(x => new ShowcaseCustomerCredential(
                    $"{x.FirstName} {x.LastName}",
                    x.PhoneNumber,
                    SharedCustomerPassword))
                .ToArray(),
            moderator.PhoneNumber,
            ModeratorPassword,
            medicines.Count,
            stockItems.Count,
            reservations.Count);
    }

    private static async Task<Dictionary<string, MedicineCategory>> EnsureCategoriesAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new CategorySeedSpec("Pain Relief", "Everyday pain and fever relief medicines."),
            new CategorySeedSpec("Allergy & Cold", "Seasonal allergy and common cold support."),
            new CategorySeedSpec("Prescription Care", "Prescription-only medicines for chronic and infectious conditions.")
        };

        var result = new Dictionary<string, MedicineCategory>(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            var category = await context.MedicineCategories
                .FirstOrDefaultAsync(x => x.Name == spec.Name, cancellationToken);

            if (category is null)
            {
                category = new MedicineCategory
                {
                    Name = spec.Name,
                };

                await context.MedicineCategories.AddAsync(category, cancellationToken);
            }

            category.Description = spec.Description;
            result[spec.Name] = category;
        }

        return result;
    }

    private static async Task<Dictionary<string, Medicine>> EnsureMedicinesAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, MedicineCategory> categories,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new MedicineSeedSpec("Panadol", "Paracetamol", "Tablet", "500 mg", "GSK", "Azerbaijan", "8900000000001", false, "Pain Relief"),
            new MedicineSeedSpec("Nurofen", "Ibuprofen", "Tablet", "200 mg", "Reckitt", "United Kingdom", "8900000000002", false, "Pain Relief"),
            new MedicineSeedSpec("Claritin", "Loratadine", "Tablet", "10 mg", "Bayer", "Belgium", "8900000000003", false, "Allergy & Cold"),
            new MedicineSeedSpec("Coldrex", "Paracetamol + Phenylephrine", "Powder", "5 g", "GSK", "United Kingdom", "8900000000004", false, "Allergy & Cold"),
            new MedicineSeedSpec("Amoxicillin", "Amoxicillin", "Capsule", "500 mg", "Sandoz", "Austria", "8900000000005", true, "Prescription Care"),
            new MedicineSeedSpec("Azithromycin", "Azithromycin", "Tablet", "500 mg", "Teva", "Croatia", "8900000000006", true, "Prescription Care"),
            new MedicineSeedSpec("Metformin", "Metformin", "Tablet", "850 mg", "Berlin-Chemie", "Germany", "8900000000007", true, "Prescription Care"),
            new MedicineSeedSpec("Vitamin C", "Ascorbic Acid", "Tablet", "1000 mg", "Bayer", "Germany", "8900000000008", false, "Allergy & Cold")
        };

        var medicines = new Dictionary<string, Medicine>(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            var medicine = await context.Medicines
                .FirstOrDefaultAsync(x => x.Barcode == spec.Barcode, cancellationToken);

            if (medicine is null)
            {
                medicine = new Medicine
                {
                    Barcode = spec.Barcode,
                };

                await context.Medicines.AddAsync(medicine, cancellationToken);
            }

            medicine.BrandName = spec.BrandName;
            medicine.GenericName = spec.GenericName;
            medicine.Description = $"{spec.BrandName} showcase medicine.";
            medicine.DosageForm = spec.DosageForm;
            medicine.Strength = spec.Strength;
            medicine.Manufacturer = spec.Manufacturer;
            medicine.CountryOfOrigin = spec.CountryOfOrigin;
            medicine.RequiresPrescription = spec.RequiresPrescription;
            medicine.IsActive = true;
            medicine.CategoryId = categories[spec.CategoryName].Id;

            medicines[spec.BrandName] = medicine;
        }

        return medicines;
    }

    private static async Task<Dictionary<string, Pharmacy>> EnsurePharmaciesAsync(
        ApplicationDbContext context,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new PharmacySeedSpec("PharmaGo Central", "Baku", "Nasimi", "Nizami Street 42", "+994500100001", 40.409264m, 49.867092m, true, true),
            new PharmacySeedSpec("PharmaGo Khatai Express", "Baku", "Khatai", "Babek Avenue 18", "+994500100002", 40.385102m, 49.957671m, true, true),
            new PharmacySeedSpec("PharmaGo Yasamal Care", "Baku", "Yasamal", "Inshaatchilar Avenue 55", "+994500100003", 40.395217m, 49.812923m, true, false),
            new PharmacySeedSpec("PharmaGo Sumgait Family", "Sumgait", "Sumgait", "Sahil Street 10", "+994500100004", 40.589722m, 49.668611m, true, true)
        };

        var pharmacies = new Dictionary<string, Pharmacy>(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            var pharmacy = await context.Pharmacies
                .FirstOrDefaultAsync(x => x.Name == spec.Name, cancellationToken);

            if (pharmacy is null)
            {
                pharmacy = new Pharmacy
                {
                    Name = spec.Name,
                };

                await context.Pharmacies.AddAsync(pharmacy, cancellationToken);
            }

            pharmacy.Address = spec.Address;
            pharmacy.City = spec.City;
            pharmacy.Region = spec.Region;
            pharmacy.PhoneNumber = spec.PhoneNumber;
            pharmacy.Latitude = spec.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            pharmacy.Longitude = spec.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            pharmacy.LocationLatitude = spec.Latitude;
            pharmacy.LocationLongitude = spec.Longitude;
            pharmacy.OpeningHoursJson = """
            {"timeZone":"Asia/Baku","weekly":[{"day":"Mon","open":"08:00","close":"22:00"},{"day":"Tue","open":"08:00","close":"22:00"},{"day":"Wed","open":"08:00","close":"22:00"},{"day":"Thu","open":"08:00","close":"22:00"},{"day":"Fri","open":"08:00","close":"22:00"},{"day":"Sat","open":"09:00","close":"21:00"},{"day":"Sun","open":"09:00","close":"21:00"}]}
            """;
            pharmacy.IsOpen24Hours = spec.IsOpen24Hours;
            pharmacy.SupportsReservations = spec.SupportsReservations;
            pharmacy.HasDelivery = spec.HasDelivery;
            pharmacy.IsActive = true;
            pharmacy.LastLocationVerifiedAtUtc = utcNow;

            pharmacies[spec.Name] = pharmacy;
        }

        return pharmacies;
    }

    private static async Task<Dictionary<string, AppUser>> EnsurePharmacistsAsync(
        ApplicationDbContext context,
        PasswordHasher<AppUser> passwordHasher,
        IReadOnlyDictionary<string, Pharmacy> pharmacies,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new UserSeedSpec("Farid", "Abbasov", "+994509991101", "central.pharmacist@pharmago.local", UserRole.Pharmacist, "PharmaGo Central"),
            new UserSeedSpec("Aysel", "Karimova", "+994509991102", "khatai.pharmacist@pharmago.local", UserRole.Pharmacist, "PharmaGo Khatai Express"),
            new UserSeedSpec("Murad", "Hasanli", "+994509991103", "yasamal.pharmacist@pharmago.local", UserRole.Pharmacist, "PharmaGo Yasamal Care"),
            new UserSeedSpec("Lala", "Guliyeva", "+994509991104", "sumgait.pharmacist@pharmago.local", UserRole.Pharmacist, "PharmaGo Sumgait Family")
        };

        var result = new Dictionary<string, AppUser>(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            var user = await EnsureUserAsync(
                context,
                passwordHasher,
                spec.FirstName,
                spec.LastName,
                spec.PhoneNumber,
                spec.Email,
                spec.Role,
                pharmacies[spec.PharmacyName!].Id,
                SharedStaffPassword,
                cancellationToken);

            result[spec.PharmacyName!] = user;
        }

        return result;
    }

    private static async Task<Dictionary<string, AppUser>> EnsureCustomersAsync(
        ApplicationDbContext context,
        PasswordHasher<AppUser> passwordHasher,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new UserSeedSpec("Leyla", "Aliyeva", "+994509992001", "leyla@pharmago.local", UserRole.User, null),
            new UserSeedSpec("Kamran", "Mammadov", "+994509992002", "kamran@pharmago.local", UserRole.User, null),
            new UserSeedSpec("Sabina", "Rzayeva", "+994509992003", "sabina@pharmago.local", UserRole.User, null)
        };

        var result = new Dictionary<string, AppUser>(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            var user = await EnsureUserAsync(
                context,
                passwordHasher,
                spec.FirstName,
                spec.LastName,
                spec.PhoneNumber,
                spec.Email,
                spec.Role,
                null,
                SharedCustomerPassword,
                cancellationToken);

            result[user.PhoneNumber] = user;
        }

        return result;
    }

    private static async Task<AppUser> EnsureUserAsync(
        ApplicationDbContext context,
        PasswordHasher<AppUser> passwordHasher,
        string firstName,
        string lastName,
        string phoneNumber,
        string email,
        UserRole role,
        Guid? pharmacyId,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);

        if (user is null)
        {
            user = new AppUser
            {
                PhoneNumber = phoneNumber,
            };

            await context.Users.AddAsync(user, cancellationToken);
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;
        user.Role = role;
        user.PharmacyId = pharmacyId;
        user.IsActive = true;
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        return user;
    }

    private static async Task EnsureNotificationPreferenceAsync(
        ApplicationDbContext context,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var preference = await context.NotificationPreferences
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (preference is null)
        {
            preference = new NotificationPreference
            {
                UserId = userId,
            };

            await context.NotificationPreferences.AddAsync(preference, cancellationToken);
        }

        preference.InAppEnabled = true;
        preference.TelegramEnabled = false;
        preference.ReservationConfirmedEnabled = true;
        preference.ReservationReadyEnabled = true;
        preference.ReservationCancelledEnabled = true;
        preference.ReservationExpiredEnabled = true;
        preference.ReservationExpiringSoonEnabled = true;
    }

    private static async Task<Dictionary<string, StockItem>> EnsureStockItemsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, Pharmacy> pharmacies,
        IReadOnlyDictionary<string, Medicine> medicines,
        DateOnly today,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new StockSeedSpec("PharmaGo Central", "Panadol", "CENT-PAN-001", today.AddMonths(12), 45, 0, 1.25m, 2.50m, 10, true),
            new StockSeedSpec("PharmaGo Central", "Nurofen", "CENT-NUR-001", today.AddMonths(10), 30, 0, 1.80m, 3.60m, 8, true),
            new StockSeedSpec("PharmaGo Central", "Claritin", "CENT-CLR-001", today.AddMonths(8), 12, 0, 2.10m, 4.20m, 6, true),
            new StockSeedSpec("PharmaGo Central", "Amoxicillin", "CENT-AMX-001", today.AddMonths(6), 10, 0, 2.50m, 5.50m, 4, true),

            new StockSeedSpec("PharmaGo Khatai Express", "Panadol", "KHT-PAN-001", today.AddMonths(11), 24, 0, 1.25m, 2.70m, 8, true),
            new StockSeedSpec("PharmaGo Khatai Express", "Coldrex", "KHT-CLD-001", today.AddMonths(7), 16, 0, 2.20m, 4.80m, 6, true),
            new StockSeedSpec("PharmaGo Khatai Express", "Azithromycin", "KHT-AZI-001", today.AddMonths(5), 8, 0, 3.40m, 7.20m, 4, true),
            new StockSeedSpec("PharmaGo Khatai Express", "Vitamin C", "KHT-VTC-001", today.AddMonths(9), 22, 0, 1.10m, 2.90m, 10, true),

            new StockSeedSpec("PharmaGo Yasamal Care", "Nurofen", "YSM-NUR-001", today.AddMonths(12), 20, 0, 1.75m, 3.70m, 8, true),
            new StockSeedSpec("PharmaGo Yasamal Care", "Claritin", "YSM-CLR-001", today.AddMonths(9), 9, 0, 2.15m, 4.50m, 5, true),
            new StockSeedSpec("PharmaGo Yasamal Care", "Metformin", "YSM-MTF-001", today.AddMonths(14), 14, 0, 1.90m, 4.40m, 6, true),
            new StockSeedSpec("PharmaGo Yasamal Care", "Vitamin C", "YSM-VTC-001", today.AddMonths(10), 18, 0, 1.00m, 2.80m, 8, true),

            new StockSeedSpec("PharmaGo Sumgait Family", "Panadol", "SMG-PAN-001", today.AddMonths(13), 18, 0, 1.20m, 2.55m, 8, true),
            new StockSeedSpec("PharmaGo Sumgait Family", "Coldrex", "SMG-CLD-001", today.AddMonths(8), 11, 0, 2.15m, 4.70m, 6, true),
            new StockSeedSpec("PharmaGo Sumgait Family", "Amoxicillin", "SMG-AMX-001", today.AddMonths(7), 6, 0, 2.60m, 5.80m, 4, true),
            new StockSeedSpec("PharmaGo Sumgait Family", "Metformin", "SMG-MTF-001", today.AddMonths(15), 15, 0, 1.95m, 4.60m, 6, true)
        };

        var result = new Dictionary<string, StockItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            var stockItem = await context.StockItems
                .FirstOrDefaultAsync(x => x.BatchNumber == spec.BatchNumber, cancellationToken);

            if (stockItem is null)
            {
                stockItem = new StockItem
                {
                    BatchNumber = spec.BatchNumber,
                };

                await context.StockItems.AddAsync(stockItem, cancellationToken);
            }

            stockItem.PharmacyId = pharmacies[spec.PharmacyName].Id;
            stockItem.MedicineId = medicines[spec.MedicineName].Id;
            stockItem.ExpirationDate = spec.ExpirationDate;
            stockItem.Quantity = spec.Quantity;
            stockItem.ReservedQuantity = spec.ReservedQuantity;
            stockItem.PurchasePrice = spec.PurchasePrice;
            stockItem.RetailPrice = spec.RetailPrice;
            stockItem.ReorderLevel = spec.ReorderLevel;
            stockItem.IsReservable = spec.IsReservable;
            stockItem.IsActive = true;
            stockItem.LastStockUpdatedAtUtc = utcNow;

            result[BuildStockKey(spec.PharmacyName, spec.MedicineName)] = stockItem;
        }

        return result;
    }

    private static async Task<List<Reservation>> EnsureReservationsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, AppUser> customers,
        IReadOnlyDictionary<string, Pharmacy> pharmacies,
        IReadOnlyDictionary<string, Medicine> medicines,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new ReservationSeedSpec("PG-SHOW-3001", customers["+994509992001"].Id, "PharmaGo Central", ReservationStatus.Pending, utcNow.AddHours(3), null, null, null, null, "Ждет подтверждения в центральной аптеке.", new[] { new ReservationItemSeedSpec("Panadol", 2, 2.50m) }),
            new ReservationSeedSpec("PG-SHOW-3002", customers["+994509992002"].Id, "PharmaGo Central", ReservationStatus.ReadyForPickup, utcNow.AddHours(8), utcNow.AddHours(-2), utcNow.AddMinutes(-35), utcNow.AddMinutes(-20), null, "Готов к выдаче сегодня вечером.", new[] { new ReservationItemSeedSpec("Amoxicillin", 1, 5.50m) }),
            new ReservationSeedSpec("PG-SHOW-3003", customers["+994509992001"].Id, "PharmaGo Khatai Express", ReservationStatus.Confirmed, utcNow.AddHours(5), utcNow.AddMinutes(-50), null, utcNow.AddMinutes(25), null, "Уже подтвержден, но еще собирается.", new[] { new ReservationItemSeedSpec("Coldrex", 1, 4.80m), new ReservationItemSeedSpec("Vitamin C", 1, 2.90m) }),
            new ReservationSeedSpec("PG-SHOW-3004", customers["+994509992003"].Id, "PharmaGo Yasamal Care", ReservationStatus.Completed, utcNow.AddHours(-12), utcNow.AddHours(-20), utcNow.AddHours(-18), utcNow.AddHours(-18), utcNow.AddHours(-16), "Успешно выданный завершенный резерв.", new[] { new ReservationItemSeedSpec("Metformin", 1, 4.40m) }),
            new ReservationSeedSpec("PG-SHOW-3005", customers["+994509992002"].Id, "PharmaGo Sumgait Family", ReservationStatus.Cancelled, utcNow.AddHours(-6), null, null, null, null, "Клиент отменил заказ до подтверждения.", new[] { new ReservationItemSeedSpec("Panadol", 1, 2.55m) }, CancelledAtUtc: utcNow.AddHours(-5)),
            new ReservationSeedSpec("PG-SHOW-3006", customers["+994509992003"].Id, "PharmaGo Sumgait Family", ReservationStatus.Expired, utcNow.AddHours(-1), utcNow.AddHours(-8), null, utcNow.AddHours(-7), null, "Истекший резерв для сценария no-show.", new[] { new ReservationItemSeedSpec("Coldrex", 1, 4.70m) }, ExpiredAtUtc: utcNow.AddMinutes(-5))
        };

        var result = new List<Reservation>();

        foreach (var spec in specs)
        {
            var reservation = await context.Reservations
                .FirstOrDefaultAsync(x => x.ReservationNumber == spec.ReservationNumber, cancellationToken);

            if (reservation is null)
            {
                reservation = new Reservation
                {
                    ReservationNumber = spec.ReservationNumber,
                };

                await context.Reservations.AddAsync(reservation, cancellationToken);
            }
            else
            {
                await context.ReservationItems
                    .Where(x => x.ReservationId == reservation.Id)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            reservation.CustomerId = spec.CustomerId;
            reservation.PharmacyId = pharmacies[spec.PharmacyName].Id;
            reservation.Pharmacy = pharmacies[spec.PharmacyName];
            reservation.Status = spec.Status;
            reservation.ReservedUntilUtc = spec.ReservedUntilUtc;
            reservation.ConfirmedAtUtc = spec.ConfirmedAtUtc;
            reservation.ReadyForPickupAtUtc = spec.ReadyForPickupAtUtc;
            reservation.PickupAvailableFromUtc = spec.PickupAvailableFromUtc;
            reservation.CompletedAtUtc = spec.CompletedAtUtc;
            reservation.CancelledAtUtc = spec.CancelledAtUtc;
            reservation.ExpiredAtUtc = spec.ExpiredAtUtc;
            reservation.TelegramChatId = null;
            reservation.Notes = spec.Notes;

            var items = spec.Items
                .Select(item => new ReservationItem
                {
                    ReservationId = reservation.Id,
                    MedicineId = medicines[item.MedicineName].Id,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                })
                .ToList();

            await context.ReservationItems.AddRangeAsync(items, cancellationToken);
            reservation.Items = items;
            reservation.TotalAmount = items.Sum(x => x.TotalPrice);

            result.Add(reservation);
        }

        return result;
    }

    private static void ReconcileReservedQuantities(
        IReadOnlyDictionary<string, StockItem> stockItems,
        IReadOnlyCollection<Reservation> reservations)
    {
        foreach (var stockItem in stockItems.Values)
        {
            stockItem.ReservedQuantity = 0;
        }

        foreach (var reservation in reservations)
        {
            if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed or ReservationStatus.ReadyForPickup))
            {
                continue;
            }

            foreach (var item in reservation.Items)
            {
                var stockItem = stockItems.Values.First(x => x.PharmacyId == reservation.PharmacyId && x.MedicineId == item.MedicineId);
                stockItem.ReservedQuantity += item.Quantity;
            }
        }
    }

    private static async Task EnsureTimelineAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, AppUser> pharmacists,
        IReadOnlyCollection<Reservation> reservations,
        CancellationToken cancellationToken)
    {
        foreach (var reservation in reservations)
        {
            var pharmacist = pharmacists[reservation.Pharmacy!.Name];

            await EnsureAuditEventAsync(
                context,
                reservation.Id,
                reservation.PharmacyId,
                pharmacist.Id,
                "reservation.created",
                $"Reservation {reservation.ReservationNumber} created for showcase data.",
                new { reservation.Id, reservation.ReservationNumber, Status = ReservationStatus.Pending.ToString() },
                cancellationToken);

            if (reservation.Status is ReservationStatus.Confirmed or ReservationStatus.ReadyForPickup or ReservationStatus.Completed or ReservationStatus.Expired)
            {
                await EnsureAuditEventAsync(
                    context,
                    reservation.Id,
                    reservation.PharmacyId,
                    pharmacist.Id,
                    "reservation.confirmed",
                    $"Reservation {reservation.ReservationNumber} confirmed in showcase flow.",
                    new { reservation.Id, reservation.ReservationNumber, Status = ReservationStatus.Confirmed.ToString() },
                    cancellationToken);
            }

            if (reservation.Status is ReservationStatus.ReadyForPickup or ReservationStatus.Completed)
            {
                await EnsureAuditEventAsync(
                    context,
                    reservation.Id,
                    reservation.PharmacyId,
                    pharmacist.Id,
                    "reservation.ready_for_pickup",
                    $"Reservation {reservation.ReservationNumber} prepared for pickup in showcase flow.",
                    new { reservation.Id, reservation.ReservationNumber, Status = ReservationStatus.ReadyForPickup.ToString() },
                    cancellationToken);
            }

            if (reservation.Status is ReservationStatus.Completed)
            {
                await EnsureAuditEventAsync(
                    context,
                    reservation.Id,
                    reservation.PharmacyId,
                    pharmacist.Id,
                    "reservation.completed",
                    $"Reservation {reservation.ReservationNumber} completed in showcase flow.",
                    new { reservation.Id, reservation.ReservationNumber, Status = ReservationStatus.Completed.ToString() },
                    cancellationToken);
            }

            if (reservation.Status is ReservationStatus.Cancelled)
            {
                await EnsureAuditEventAsync(
                    context,
                    reservation.Id,
                    reservation.PharmacyId,
                    pharmacist.Id,
                    "reservation.cancelled",
                    $"Reservation {reservation.ReservationNumber} cancelled in showcase flow.",
                    new { reservation.Id, reservation.ReservationNumber, Status = ReservationStatus.Cancelled.ToString() },
                    cancellationToken);
            }

            if (reservation.Status is ReservationStatus.Expired)
            {
                await EnsureAuditEventAsync(
                    context,
                    reservation.Id,
                    reservation.PharmacyId,
                    pharmacist.Id,
                    "reservation.expired",
                    $"Reservation {reservation.ReservationNumber} expired in showcase flow.",
                    new { reservation.Id, reservation.ReservationNumber, Status = ReservationStatus.Expired.ToString() },
                    cancellationToken);
            }
        }
    }

    private static async Task EnsureAuditEventAsync(
        ApplicationDbContext context,
        Guid reservationId,
        Guid pharmacyId,
        Guid userId,
        string action,
        string description,
        object metadata,
        CancellationToken cancellationToken)
    {
        var existing = await context.AuditLogs.FirstOrDefaultAsync(
            x => x.EntityName == "Reservation" && x.EntityId == reservationId.ToString() && x.Action == action,
            cancellationToken);

        if (existing is null)
        {
            await context.AuditLogs.AddAsync(new AuditLog
            {
                EntityName = "Reservation",
                EntityId = reservationId.ToString(),
                PharmacyId = pharmacyId,
                UserId = userId,
                Action = action,
                Description = description,
                MetadataJson = JsonSerializer.Serialize(metadata),
            }, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        existing.PharmacyId = pharmacyId;
        existing.UserId = userId;
        existing.Description = description;
        existing.MetadataJson = JsonSerializer.Serialize(metadata);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureNotificationsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, AppUser> pharmacists,
        IReadOnlyDictionary<string, AppUser> customers,
        IReadOnlyCollection<Reservation> reservations,
        CancellationToken cancellationToken)
    {
        foreach (var reservation in reservations)
        {
            var pharmacist = pharmacists[reservation.Pharmacy!.Name];
            var customer = customers.Values.First(x => x.Id == reservation.CustomerId);

            if (reservation.Status is ReservationStatus.Pending)
            {
                await EnsureNotificationAsync(
                    context,
                    pharmacist.Id,
                    $"showcase-pharmacist-pending-{reservation.ReservationNumber}",
                    NotificationEventType.ReservationExpiringSoon,
                    reservation.Id,
                    "Новый резерв в очереди",
                    $"Появился новый резерв {reservation.ReservationNumber} в аптеке {reservation.Pharmacy.Name}.",
                    false,
                    cancellationToken);
            }

            if (reservation.Status is ReservationStatus.Confirmed or ReservationStatus.ReadyForPickup or ReservationStatus.Completed or ReservationStatus.Expired)
            {
                await EnsureNotificationAsync(
                    context,
                    customer.Id,
                    $"showcase-customer-confirmed-{reservation.ReservationNumber}",
                    NotificationEventType.ReservationConfirmed,
                    reservation.Id,
                    "Резерв подтвержден",
                    $"Ваш резерв {reservation.ReservationNumber} подтвержден аптекой {reservation.Pharmacy.Name}.",
                    reservation.Status is not ReservationStatus.Confirmed,
                    cancellationToken);
            }

            if (reservation.Status is ReservationStatus.ReadyForPickup or ReservationStatus.Completed)
            {
                await EnsureNotificationAsync(
                    context,
                    customer.Id,
                    $"showcase-customer-ready-{reservation.ReservationNumber}",
                    NotificationEventType.ReservationReadyForPickup,
                    reservation.Id,
                    "Резерв готов к выдаче",
                    $"Ваш резерв {reservation.ReservationNumber} готов к выдаче в {reservation.Pharmacy.Name}.",
                    reservation.Status is ReservationStatus.Completed,
                    cancellationToken);

                await EnsureNotificationAsync(
                    context,
                    pharmacist.Id,
                    $"showcase-pharmacist-ready-{reservation.ReservationNumber}",
                    NotificationEventType.ReservationReadyForPickup,
                    reservation.Id,
                    "Заказ готов к выдаче",
                    $"Резерв {reservation.ReservationNumber} уже готов к выдаче клиенту.",
                    false,
                    cancellationToken);
            }

            if (reservation.Status is ReservationStatus.Completed)
            {
                await EnsureNotificationAsync(
                    context,
                    customer.Id,
                    $"showcase-customer-completed-{reservation.ReservationNumber}",
                    NotificationEventType.ReservationCompleted,
                    reservation.Id,
                    "Резерв завершен",
                    $"Резерв {reservation.ReservationNumber} успешно выдан и завершен.",
                    false,
                    cancellationToken);
            }

            if (reservation.Status is ReservationStatus.Cancelled)
            {
                await EnsureNotificationAsync(
                    context,
                    customer.Id,
                    $"showcase-customer-cancelled-{reservation.ReservationNumber}",
                    NotificationEventType.ReservationCancelled,
                    reservation.Id,
                    "Резерв отменен",
                    $"Резерв {reservation.ReservationNumber} отменен.",
                    false,
                    cancellationToken);
            }

            if (reservation.Status is ReservationStatus.Expired)
            {
                await EnsureNotificationAsync(
                    context,
                    customer.Id,
                    $"showcase-customer-expired-{reservation.ReservationNumber}",
                    NotificationEventType.ReservationExpired,
                    reservation.Id,
                    "Резерв истек",
                    $"Срок действия резерва {reservation.ReservationNumber} истек.",
                    false,
                    cancellationToken);
            }
        }
    }

    private static async Task EnsureNotificationAsync(
        ApplicationDbContext context,
        Guid userId,
        string deliveryKey,
        NotificationEventType eventType,
        Guid reservationId,
        string title,
        string message,
        bool isRead,
        CancellationToken cancellationToken)
    {
        var log = await context.NotificationDeliveryLogs
            .FirstOrDefaultAsync(x => x.UserId == userId && x.DeliveryKey == deliveryKey, cancellationToken);

        if (log is null)
        {
            log = new NotificationDeliveryLog
            {
                UserId = userId,
                DeliveryKey = deliveryKey,
            };

            await context.NotificationDeliveryLogs.AddAsync(log, cancellationToken);
        }

        log.ReservationId = reservationId;
        log.EventType = eventType;
        log.Channel = NotificationChannel.InApp;
        log.Status = NotificationDeliveryStatus.Sent;
        log.Title = title;
        log.Message = message;
        log.PayloadJson = JsonSerializer.Serialize(new
        {
            reservationId,
            eventType = eventType.ToString(),
        });
        log.ErrorMessage = null;
        log.DeliveredAtUtc = DateTime.UtcNow;
        log.ReadAtUtc = isRead ? DateTime.UtcNow : null;
    }

    private static string BuildStockKey(string pharmacyName, string medicineName)
        => $"{pharmacyName}::{medicineName}";

    public sealed record ShowcaseBootstrapResult(
        IReadOnlyCollection<ShowcasePharmacyCredential> Pharmacies,
        IReadOnlyCollection<ShowcaseCustomerCredential> Customers,
        string ModeratorPhoneNumber,
        string ModeratorPassword,
        int MedicineCount,
        int StockItemCount,
        int ReservationCount);

    public sealed record ShowcasePharmacyCredential(
        string PharmacyName,
        string City,
        Guid PharmacyId,
        string PharmacistName,
        string PharmacistPhoneNumber,
        string PharmacistPassword);

    public sealed record ShowcaseCustomerCredential(
        string CustomerName,
        string PhoneNumber,
        string Password);

    private sealed record CategorySeedSpec(string Name, string Description);

    private sealed record MedicineSeedSpec(
        string BrandName,
        string GenericName,
        string DosageForm,
        string Strength,
        string Manufacturer,
        string CountryOfOrigin,
        string Barcode,
        bool RequiresPrescription,
        string CategoryName);

    private sealed record PharmacySeedSpec(
        string Name,
        string City,
        string Region,
        string Address,
        string PhoneNumber,
        decimal Latitude,
        decimal Longitude,
        bool SupportsReservations,
        bool HasDelivery,
        bool IsOpen24Hours = false);

    private sealed record UserSeedSpec(
        string FirstName,
        string LastName,
        string PhoneNumber,
        string Email,
        UserRole Role,
        string? PharmacyName);

    private sealed record StockSeedSpec(
        string PharmacyName,
        string MedicineName,
        string BatchNumber,
        DateOnly ExpirationDate,
        int Quantity,
        int ReservedQuantity,
        decimal PurchasePrice,
        decimal RetailPrice,
        int ReorderLevel,
        bool IsReservable);

    private sealed record ReservationSeedSpec(
        string ReservationNumber,
        Guid CustomerId,
        string PharmacyName,
        ReservationStatus Status,
        DateTime ReservedUntilUtc,
        DateTime? ConfirmedAtUtc,
        DateTime? ReadyForPickupAtUtc,
        DateTime? PickupAvailableFromUtc,
        DateTime? CompletedAtUtc,
        string Notes,
        IReadOnlyCollection<ReservationItemSeedSpec> Items,
        DateTime? CancelledAtUtc = null,
        DateTime? ExpiredAtUtc = null);

    private sealed record ReservationItemSeedSpec(string MedicineName, int Quantity, decimal UnitPrice);
}

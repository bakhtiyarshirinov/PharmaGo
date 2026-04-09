using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Infrastructure.Persistence;

public static class PharmacistSmokeBootstrapSeeder
{
    public const string PharmacyName = "Bootstrap Pharmacy Baku";
    public const string PharmacistPhoneNumber = "+994509990001";
    public const string PharmacistPassword = "Pharmacist123!";
    public const string ModeratorPhoneNumber = "+994509990003";
    public const string ModeratorPassword = "Moderator123!";

    private const string PharmacistEmail = "bootstrap.pharmacist@pharmago.local";
    private const string ModeratorEmail = "bootstrap.moderator@pharmago.local";
    private const string CustomerPhoneNumber = "+994509990101";
    private const string CustomerPassword = "User12345!";
    private const string CustomerEmail = "bootstrap.customer@pharmago.local";
    private const string CategoryName = "Bootstrap Essentials";

    public static async Task<PharmacistSmokeBootstrapResult> SeedAsync(
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

    private static async Task<PharmacistSmokeBootstrapResult> SeedCoreAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var passwordHasher = new PasswordHasher<AppUser>();
        var utcNow = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(utcNow);

        var category = await EnsureCategoryAsync(context, cancellationToken);
        var pharmacy = await EnsurePharmacyAsync(context, utcNow, cancellationToken);
        var pharmacist = await EnsureUserAsync(
            context,
            passwordHasher,
            firstName: "Farid",
            lastName: "Bootstrapov",
            phoneNumber: PharmacistPhoneNumber,
            email: PharmacistEmail,
            role: UserRole.Pharmacist,
            pharmacyId: pharmacy.Id,
            password: PharmacistPassword,
            cancellationToken);
        var moderator = await EnsureUserAsync(
            context,
            passwordHasher,
            firstName: "Nigar",
            lastName: "Moderatorova",
            phoneNumber: ModeratorPhoneNumber,
            email: ModeratorEmail,
            role: UserRole.Moderator,
            pharmacyId: null,
            password: ModeratorPassword,
            cancellationToken);
        var customer = await EnsureUserAsync(
            context,
            passwordHasher,
            firstName: "Nurlan",
            lastName: "Mammadli",
            phoneNumber: CustomerPhoneNumber,
            email: CustomerEmail,
            role: UserRole.User,
            pharmacyId: null,
            password: CustomerPassword,
            cancellationToken);

        await EnsureNotificationPreferenceAsync(context, pharmacist.Id, cancellationToken);

        var medicines = await EnsureMedicinesAsync(context, category.Id, cancellationToken);
        var stockByMedicineKey = await EnsureStockItemsAsync(context, pharmacy.Id, medicines, today, utcNow, cancellationToken);
        var reservations = await EnsureReservationsAsync(
            context,
            customer.Id,
            pharmacy.Id,
            medicines,
            utcNow,
            cancellationToken);

        ReconcileReservedQuantities(stockByMedicineKey, reservations);
        await context.SaveChangesAsync(cancellationToken);

        await EnsureTimelineAsync(context, pharmacist, reservations, cancellationToken);
        await EnsureNotificationsAsync(context, pharmacist.Id, reservations, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return new PharmacistSmokeBootstrapResult(
            pharmacy.Id,
            PharmacyName,
            pharmacist.Id,
            PharmacistPhoneNumber,
            PharmacistPassword,
            moderator.Id,
            ModeratorPhoneNumber,
            ModeratorPassword,
            reservations.Pending.ReservationNumber,
            reservations.Ready.ReservationNumber);
    }

    private static async Task<MedicineCategory> EnsureCategoryAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var category = await context.MedicineCategories
            .FirstOrDefaultAsync(x => x.Name == CategoryName, cancellationToken);

        if (category is not null)
        {
            category.Description = "Minimal deterministic category for pharmacist smoke bootstrap.";
            return category;
        }

        category = new MedicineCategory
        {
            Name = CategoryName,
            Description = "Minimal deterministic category for pharmacist smoke bootstrap."
        };

        await context.MedicineCategories.AddAsync(category, cancellationToken);
        return category;
    }

    private static async Task<Pharmacy> EnsurePharmacyAsync(
        ApplicationDbContext context,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var pharmacy = await context.Pharmacies
            .FirstOrDefaultAsync(x => x.Name == PharmacyName, cancellationToken);

        if (pharmacy is null)
        {
            pharmacy = new Pharmacy
            {
                Name = PharmacyName
            };

            await context.Pharmacies.AddAsync(pharmacy, cancellationToken);
        }

        pharmacy.Address = "Nizami Street 42";
        pharmacy.City = "Baku";
        pharmacy.Region = "Nasimi";
        pharmacy.PhoneNumber = "+994509990000";
        pharmacy.Latitude = "40.409264";
        pharmacy.Longitude = "49.867092";
        pharmacy.LocationLatitude = 40.409264m;
        pharmacy.LocationLongitude = 49.867092m;
        pharmacy.OpeningHoursJson = """
        {"timeZone":"Asia/Baku","weekly":[{"day":"Mon","open":"08:00","close":"22:00"},{"day":"Tue","open":"08:00","close":"22:00"},{"day":"Wed","open":"08:00","close":"22:00"},{"day":"Thu","open":"08:00","close":"22:00"},{"day":"Fri","open":"08:00","close":"22:00"},{"day":"Sat","open":"09:00","close":"21:00"},{"day":"Sun","open":"09:00","close":"21:00"}]}
        """;
        // Smoke pharmacy stays available around the clock so the full reservation
        // lifecycle can be exercised deterministically at any hour.
        pharmacy.IsOpen24Hours = true;
        pharmacy.SupportsReservations = true;
        pharmacy.HasDelivery = false;
        pharmacy.IsActive = true;
        pharmacy.LastLocationVerifiedAtUtc = utcNow;

        return pharmacy;
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
        var user = await context.Users
            .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);

        if (user is null)
        {
            user = new AppUser
            {
                PhoneNumber = phoneNumber
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
                UserId = userId
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

    private static async Task<Dictionary<string, Medicine>> EnsureMedicinesAsync(
        ApplicationDbContext context,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new MedicineSeedSpec("Bootstrap Panadol", "Paracetamol", "Tablet", "500 mg", "GSK", "Azerbaijan", "8000000000001", false),
            new MedicineSeedSpec("Bootstrap Nurofen", "Ibuprofen", "Tablet", "200 mg", "Reckitt", "Azerbaijan", "8000000000002", false),
            new MedicineSeedSpec("Bootstrap Claritin", "Loratadine", "Tablet", "10 mg", "Bayer", "Belgium", "8000000000003", false)
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
                    Barcode = spec.Barcode
                };

                await context.Medicines.AddAsync(medicine, cancellationToken);
            }

            medicine.BrandName = spec.BrandName;
            medicine.GenericName = spec.GenericName;
            medicine.Description = $"Bootstrap medicine for {spec.BrandName}.";
            medicine.DosageForm = spec.DosageForm;
            medicine.Strength = spec.Strength;
            medicine.Manufacturer = spec.Manufacturer;
            medicine.CountryOfOrigin = spec.CountryOfOrigin;
            medicine.RequiresPrescription = spec.RequiresPrescription;
            medicine.CategoryId = categoryId;
            medicine.IsActive = true;

            medicines[spec.BrandName] = medicine;
        }

        return medicines;
    }

    private static async Task<Dictionary<string, StockItem>> EnsureStockItemsAsync(
        ApplicationDbContext context,
        Guid pharmacyId,
        IReadOnlyDictionary<string, Medicine> medicines,
        DateOnly today,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new StockSeedSpec("Bootstrap Panadol", "BOOT-PAN-001", today.AddMonths(12), 40, 0, 1.30m, 2.60m, 10, true),
            new StockSeedSpec("Bootstrap Nurofen", "BOOT-NUR-001", today.AddMonths(10), 18, 0, 1.90m, 3.80m, 8, true),
            new StockSeedSpec("Bootstrap Claritin", "BOOT-CLR-001", today.AddDays(14), 4, 0, 2.10m, 4.20m, 6, true)
        };

        var stockByMedicine = new Dictionary<string, StockItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            var stockItem = await context.StockItems
                .FirstOrDefaultAsync(x => x.BatchNumber == spec.BatchNumber, cancellationToken);

            if (stockItem is null)
            {
                stockItem = new StockItem
                {
                    BatchNumber = spec.BatchNumber
                };

                await context.StockItems.AddAsync(stockItem, cancellationToken);
            }

            stockItem.PharmacyId = pharmacyId;
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

            stockByMedicine[spec.MedicineName] = stockItem;
        }

        return stockByMedicine;
    }

    private static async Task<BootstrapReservationSet> EnsureReservationsAsync(
        ApplicationDbContext context,
        Guid customerId,
        Guid pharmacyId,
        IReadOnlyDictionary<string, Medicine> medicines,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var pending = await EnsureReservationAsync(
            context,
            new ReservationSeedSpec(
                "PG-BOOT-2001",
                customerId,
                pharmacyId,
                ReservationStatus.Pending,
                utcNow.AddHours(2),
                null,
                null,
                null,
                "Новый резерв в очереди для подтверждения.",
                new[] { new ReservationItemSeedSpec("Bootstrap Panadol", 1, 2.60m) }),
            medicines,
            cancellationToken);

        var ready = await EnsureReservationAsync(
            context,
            new ReservationSeedSpec(
                "PG-BOOT-2002",
                customerId,
                pharmacyId,
                ReservationStatus.ReadyForPickup,
                utcNow.AddHours(6),
                utcNow.AddHours(-2),
                utcNow.AddMinutes(-30),
                utcNow.AddMinutes(-20),
                "Подготовлен к выдаче и готов для smoke-проверки завершения.",
                new[] { new ReservationItemSeedSpec("Bootstrap Nurofen", 1, 3.80m) }),
            medicines,
            cancellationToken);

        return new BootstrapReservationSet(pending, ready);
    }

    private static async Task<Reservation> EnsureReservationAsync(
        ApplicationDbContext context,
        ReservationSeedSpec spec,
        IReadOnlyDictionary<string, Medicine> medicines,
        CancellationToken cancellationToken)
    {
        var reservation = await context.Reservations
            .FirstOrDefaultAsync(x => x.ReservationNumber == spec.ReservationNumber, cancellationToken);

        if (reservation is null)
        {
            reservation = new Reservation
            {
                ReservationNumber = spec.ReservationNumber
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
        reservation.PharmacyId = spec.PharmacyId;
        reservation.Status = spec.Status;
        reservation.ReservedUntilUtc = spec.ReservedUntilUtc;
        reservation.ConfirmedAtUtc = spec.ConfirmedAtUtc;
        reservation.ReadyForPickupAtUtc = spec.ReadyForPickupAtUtc;
        reservation.PickupAvailableFromUtc = spec.PickupAvailableFromUtc;
        reservation.CancelledAtUtc = null;
        reservation.CompletedAtUtc = null;
        reservation.ExpiredAtUtc = null;
        reservation.TelegramChatId = null;
        reservation.Notes = spec.Notes;

        var items = spec.Items
            .Select(itemSpec => new ReservationItem
            {
                ReservationId = reservation.Id,
                MedicineId = medicines[itemSpec.MedicineName].Id,
                Quantity = itemSpec.Quantity,
                UnitPrice = itemSpec.UnitPrice
            })
            .ToList();

        await context.ReservationItems.AddRangeAsync(items, cancellationToken);

        reservation.Items = items;
        reservation.TotalAmount = items.Sum(x => x.TotalPrice);

        return reservation;
    }

    private static void ReconcileReservedQuantities(
        IReadOnlyDictionary<string, StockItem> stockByMedicine,
        BootstrapReservationSet reservations)
    {
        foreach (var stockItem in stockByMedicine.Values)
        {
            stockItem.ReservedQuantity = 0;
        }

        foreach (var reservation in new[] { reservations.Pending, reservations.Ready })
        {
            if (reservation.Status is not (ReservationStatus.Pending or ReservationStatus.Confirmed or ReservationStatus.ReadyForPickup))
            {
                continue;
            }

            foreach (var item in reservation.Items)
            {
                var stockItem = stockByMedicine.Values.First(x => x.MedicineId == item.MedicineId);
                stockItem.ReservedQuantity += item.Quantity;
            }
        }
    }

    private static async Task EnsureTimelineAsync(
        ApplicationDbContext context,
        AppUser pharmacist,
        BootstrapReservationSet reservations,
        CancellationToken cancellationToken)
    {
        await EnsureAuditEventAsync(
            context,
            reservations.Pending.Id,
            reservations.Pending.PharmacyId,
            pharmacist.Id,
            "reservation.created",
            $"Reservation {reservations.Pending.ReservationNumber} created for bootstrap queue smoke test.",
            new { reservations.Pending.Id, reservations.Pending.ReservationNumber, Status = ReservationStatus.Pending.ToString() },
            cancellationToken);

        await EnsureAuditEventAsync(
            context,
            reservations.Ready.Id,
            reservations.Ready.PharmacyId,
            pharmacist.Id,
            "reservation.created",
            $"Reservation {reservations.Ready.ReservationNumber} created for bootstrap queue smoke test.",
            new { reservations.Ready.Id, reservations.Ready.ReservationNumber, Status = ReservationStatus.Pending.ToString() },
            cancellationToken);

        await EnsureAuditEventAsync(
            context,
            reservations.Ready.Id,
            reservations.Ready.PharmacyId,
            pharmacist.Id,
            "reservation.confirmed",
            $"Reservation {reservations.Ready.ReservationNumber} confirmed during bootstrap initialization.",
            new { reservations.Ready.Id, reservations.Ready.ReservationNumber, Status = ReservationStatus.Confirmed.ToString() },
            cancellationToken);

        await EnsureAuditEventAsync(
            context,
            reservations.Ready.Id,
            reservations.Ready.PharmacyId,
            pharmacist.Id,
            "reservation.ready_for_pickup",
            $"Reservation {reservations.Ready.ReservationNumber} prepared for pickup during bootstrap initialization.",
            new { reservations.Ready.Id, reservations.Ready.ReservationNumber, Status = ReservationStatus.ReadyForPickup.ToString() },
            cancellationToken);
    }

    private static async Task EnsureAuditEventAsync(
        ApplicationDbContext context,
        Guid reservationId,
        Guid pharmacyId,
        Guid pharmacistUserId,
        string action,
        string description,
        object metadata,
        CancellationToken cancellationToken)
    {
        var existing = await context.AuditLogs
            .FirstOrDefaultAsync(
                x => x.EntityName == "Reservation" &&
                    x.EntityId == reservationId.ToString() &&
                    x.Action == action,
                cancellationToken);

        if (existing is null)
        {
            await context.AuditLogs.AddAsync(new AuditLog
            {
                EntityName = "Reservation",
                EntityId = reservationId.ToString(),
                PharmacyId = pharmacyId,
                UserId = pharmacistUserId,
                Action = action,
                Description = description,
                MetadataJson = JsonSerializer.Serialize(metadata)
            }, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        existing.PharmacyId = pharmacyId;
        existing.UserId = pharmacistUserId;
        existing.Description = description;
        existing.MetadataJson = JsonSerializer.Serialize(metadata);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureNotificationsAsync(
        ApplicationDbContext context,
        Guid pharmacistUserId,
        BootstrapReservationSet reservations,
        CancellationToken cancellationToken)
    {
        var specs = new[]
        {
            new NotificationSeedSpec(
                "bootstrap-confirmed",
                NotificationEventType.ReservationConfirmed,
                reservations.Ready.Id,
                "Резерв подтвержден",
                $"Резерв {reservations.Ready.ReservationNumber} подтвержден и передан в работу.",
                IsRead: false),
            new NotificationSeedSpec(
                "bootstrap-ready",
                NotificationEventType.ReservationReadyForPickup,
                reservations.Ready.Id,
                "Резерв готов к выдаче",
                $"Резерв {reservations.Ready.ReservationNumber} уже можно выдать клиенту.",
                IsRead: false),
            new NotificationSeedSpec(
                "bootstrap-expiring",
                NotificationEventType.ReservationExpiringSoon,
                reservations.Pending.Id,
                "Резерв скоро истечет",
                $"Резерв {reservations.Pending.ReservationNumber} ожидает подтверждения и скоро истечет.",
                IsRead: true)
        };

        foreach (var spec in specs)
        {
            var log = await context.NotificationDeliveryLogs
                .FirstOrDefaultAsync(x => x.UserId == pharmacistUserId && x.DeliveryKey == spec.DeliveryKey, cancellationToken);

            if (log is null)
            {
                log = new NotificationDeliveryLog
                {
                    UserId = pharmacistUserId,
                    DeliveryKey = spec.DeliveryKey
                };

                await context.NotificationDeliveryLogs.AddAsync(log, cancellationToken);
            }

            log.ReservationId = spec.ReservationId;
            log.EventType = spec.EventType;
            log.Channel = NotificationChannel.InApp;
            log.Status = NotificationDeliveryStatus.Sent;
            log.Title = spec.Title;
            log.Message = spec.Message;
            log.PayloadJson = JsonSerializer.Serialize(new
            {
                reservationId = spec.ReservationId,
                eventType = spec.EventType.ToString()
            });
            log.ErrorMessage = null;
            log.DeliveredAtUtc = DateTime.UtcNow;
            log.ReadAtUtc = spec.IsRead ? DateTime.UtcNow : null;
        }
    }

    public sealed record PharmacistSmokeBootstrapResult(
        Guid PharmacyId,
        string PharmacyName,
        Guid PharmacistUserId,
        string PharmacistPhoneNumber,
        string PharmacistPassword,
        Guid ModeratorUserId,
        string ModeratorPhoneNumber,
        string ModeratorPassword,
        string PendingReservationNumber,
        string ReadyReservationNumber);

    private sealed record MedicineSeedSpec(
        string BrandName,
        string GenericName,
        string DosageForm,
        string Strength,
        string Manufacturer,
        string CountryOfOrigin,
        string Barcode,
        bool RequiresPrescription);

    private sealed record StockSeedSpec(
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
        Guid PharmacyId,
        ReservationStatus Status,
        DateTime ReservedUntilUtc,
        DateTime? ConfirmedAtUtc,
        DateTime? ReadyForPickupAtUtc,
        DateTime? PickupAvailableFromUtc,
        string Notes,
        IReadOnlyCollection<ReservationItemSeedSpec> Items);

    private sealed record ReservationItemSeedSpec(string MedicineName, int Quantity, decimal UnitPrice);

    private sealed record NotificationSeedSpec(
        string DeliveryKey,
        NotificationEventType EventType,
        Guid ReservationId,
        string Title,
        string Message,
        bool IsRead);

    private sealed record BootstrapReservationSet(Reservation Pending, Reservation Ready);
}

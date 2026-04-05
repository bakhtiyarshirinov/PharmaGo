using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmaGo.Api.Realtime;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Reservations.Queries.GetReservation;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.Infrastructure.Caching;

namespace PharmaGo.Api.Background;

public class ReservationExpirationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReservationExpirationSettings> settings,
    ILogger<ReservationExpirationWorker> logger) : BackgroundService
{
    private readonly ReservationExpirationSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireReservationsAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Reservation expiration worker iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(15, _settings.PollingIntervalSeconds)), stoppingToken);
        }
    }

    private async Task ExpireReservationsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var cacheService = scope.ServiceProvider.GetRequiredService<IAppCacheService>();
        var reservationNotificationService = scope.ServiceProvider.GetRequiredService<IReservationNotificationService>();
        var reservationStateService = scope.ServiceProvider.GetRequiredService<IReservationStateService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var realtimeNotificationService = scope.ServiceProvider.GetRequiredService<RealtimeNotificationService>();

        var now = DateTime.UtcNow;

        var reservations = await context.Reservations
            .Include(x => x.Items)
            .ThenInclude(x => x.Medicine)
            .Include(x => x.Customer)
            .Include(x => x.Pharmacy)
            .Where(x =>
                (x.Status == ReservationStatus.Pending ||
                 x.Status == ReservationStatus.Confirmed ||
                 x.Status == ReservationStatus.ReadyForPickup) &&
                x.ReservedUntilUtc <= now)
            .ToListAsync(cancellationToken);

        if (reservations.Count == 0)
        {
            return;
        }

        foreach (var reservation in reservations)
        {
            reservation.Status = ReservationStatus.Expired;
            reservation.ExpiredAtUtc = now;
            await reservationStateService.ReleaseReservedStockAsync(reservation, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.MedicinesSearch, cancellationToken);
        await cacheService.BumpScopeVersionAsync(CacheScopes.Dashboard, cancellationToken);

        foreach (var reservation in reservations)
        {
            var payload = new ReservationResponse
            {
                ReservationId = reservation.Id,
                ReservationNumber = reservation.ReservationNumber,
                Status = reservation.Status,
                PharmacyId = reservation.PharmacyId,
                PharmacyName = reservation.Pharmacy?.Name ?? string.Empty,
                CustomerId = reservation.CustomerId,
                CustomerFullName = reservation.Customer is null
                    ? string.Empty
                    : $"{reservation.Customer.FirstName} {reservation.Customer.LastName}",
                PhoneNumber = reservation.Customer?.PhoneNumber ?? string.Empty,
                CreatedAtUtc = reservation.CreatedAtUtc,
                ReservedUntilUtc = reservation.ReservedUntilUtc,
                ConfirmedAtUtc = reservation.ConfirmedAtUtc,
                ReadyForPickupAtUtc = reservation.ReadyForPickupAtUtc,
                CompletedAtUtc = reservation.CompletedAtUtc,
                CancelledAtUtc = reservation.CancelledAtUtc,
                ExpiredAtUtc = reservation.ExpiredAtUtc,
                TotalAmount = reservation.TotalAmount,
                Notes = reservation.Notes,
                Items = reservation.Items.Select(item => new ReservationItemResponse
                {
                    MedicineId = item.MedicineId,
                    MedicineName = item.Medicine?.BrandName ?? string.Empty,
                    GenericName = item.Medicine?.GenericName ?? string.Empty,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                }).ToList()
            };

            await realtimeNotificationService.NotifyReservationStatusChangedAsync(
                reservation.PharmacyId,
                reservation.CustomerId,
                payload,
                cancellationToken);
            await reservationNotificationService.DispatchStatusNotificationAsync(
                reservation,
                ReservationStatus.Confirmed,
                cancellationToken);

            await auditService.WriteAsync(
                action: "reservation.expired",
                entityName: "Reservation",
                entityId: reservation.Id.ToString(),
                userId: reservation.CustomerId,
                pharmacyId: reservation.PharmacyId,
                description: $"Reservation {reservation.ReservationNumber} expired automatically.",
                metadata: new
                {
                    reservation.Id,
                    reservation.ReservationNumber,
                    Status = reservation.Status.ToString(),
                    reservation.ReservedUntilUtc
                },
                cancellationToken: cancellationToken);
        }

        logger.LogInformation("Expired {Count} reservations.", reservations.Count);
    }
}

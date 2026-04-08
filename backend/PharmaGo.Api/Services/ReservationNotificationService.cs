using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmaGo.Api.Background;
using PharmaGo.Api.Observability;
using PharmaGo.Api.Realtime;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Notifications.Contracts;
using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.Api.Services;

public class ReservationNotificationService(
    ApplicationDbContext context,
    RealtimeNotificationService realtimeNotificationService,
    IOptions<ReservationNotificationSettings> settings,
    PharmaGoMetrics metrics,
    ILogger<ReservationNotificationService> logger) : IReservationNotificationService
{
    private readonly ReservationNotificationSettings _settings = settings.Value;

    public async Task DispatchStatusNotificationAsync(
        Reservation reservation,
        ReservationStatus previousStatus,
        CancellationToken cancellationToken = default)
    {
        var eventType = reservation.Status switch
        {
            ReservationStatus.Confirmed when previousStatus != ReservationStatus.Confirmed => NotificationEventType.ReservationConfirmed,
            ReservationStatus.ReadyForPickup => NotificationEventType.ReservationReadyForPickup,
            ReservationStatus.Completed => NotificationEventType.ReservationCompleted,
            ReservationStatus.Cancelled => NotificationEventType.ReservationCancelled,
            ReservationStatus.Expired => NotificationEventType.ReservationExpired,
            _ => (NotificationEventType?)null
        };

        if (!eventType.HasValue)
        {
            return;
        }

        var (title, message) = BuildStatusContent(reservation, eventType.Value);
        await DispatchToUserAsync(
            reservation.CustomerId,
            reservation.Id,
            eventType.Value,
            title,
            message,
            new
            {
                reservation.Id,
                reservation.ReservationNumber,
                Status = reservation.Status.ToString(),
                reservation.ReservedUntilUtc,
                reservation.PickupAvailableFromUtc,
                reservation.PharmacyId,
                PharmacyName = reservation.Pharmacy?.Name
            },
            null,
            cancellationToken);
    }

    public async Task<int> DispatchExpiringSoonNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reminderOffsets = _settings.ExpiringSoonReminderMinutes
            .Where(x => x > 0)
            .Distinct()
            .OrderByDescending(x => x)
            .ToArray();

        if (reminderOffsets.Length == 0)
        {
            return 0;
        }

        var reservations = await context.Reservations
            .AsNoTracking()
            .Include(x => x.Pharmacy)
            .Where(x =>
                (x.Status == ReservationStatus.Confirmed || x.Status == ReservationStatus.ReadyForPickup) &&
                x.ReservedUntilUtc > now)
            .ToListAsync(cancellationToken);

        var dispatched = 0;

        foreach (var reservation in reservations)
        {
            var remainingMinutes = (reservation.ReservedUntilUtc - now).TotalMinutes;
            var dueOffset = reminderOffsets
                .FirstOrDefault(offset => remainingMinutes <= offset && remainingMinutes > Math.Max(0, offset - 5));

            if (dueOffset <= 0)
            {
                continue;
            }

            var deliveryKey = $"reservation-expiring-soon:{dueOffset}";
            var alreadySent = await context.NotificationDeliveryLogs
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UserId == reservation.CustomerId &&
                    x.ReservationId == reservation.Id &&
                    x.EventType == NotificationEventType.ReservationExpiringSoon &&
                    x.Channel == NotificationChannel.InApp &&
                    x.Status == NotificationDeliveryStatus.Sent &&
                    x.DeliveryKey == deliveryKey,
                    cancellationToken);

            if (alreadySent)
            {
                continue;
            }

            await DispatchToUserAsync(
                reservation.CustomerId,
                reservation.Id,
                NotificationEventType.ReservationExpiringSoon,
                "Reservation expiring soon",
                $"Reservation {reservation.ReservationNumber} at {reservation.Pharmacy?.Name ?? "your pharmacy"} expires in {dueOffset} minutes.",
                new
                {
                    reservation.Id,
                    reservation.ReservationNumber,
                    Status = reservation.Status.ToString(),
                    ReminderMinutes = dueOffset,
                    reservation.ReservedUntilUtc,
                    reservation.PickupAvailableFromUtc,
                    reservation.PharmacyId,
                    PharmacyName = reservation.Pharmacy?.Name
                },
                deliveryKey,
                cancellationToken);

            dispatched++;
        }

        return dispatched;
    }

    private async Task DispatchToUserAsync(
        Guid userId,
        Guid reservationId,
        NotificationEventType eventType,
        string title,
        string message,
        object payload,
        string? deliveryKey,
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return;
        }

        var preference = await context.NotificationPreferences
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? new NotificationPreference
            {
                UserId = userId
            };

        if (!await context.NotificationPreferences.AnyAsync(x => x.UserId == userId, cancellationToken))
        {
            await context.NotificationPreferences.AddAsync(preference, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var inAppAllowed = preference.InAppEnabled && IsEventEnabled(preference, eventType);

        if (!inAppAllowed)
        {
            await WriteLogAsync(userId, reservationId, eventType, NotificationChannel.InApp, NotificationDeliveryStatus.Skipped, title, message, payload, deliveryKey, null, cancellationToken);
            metrics.RecordNotificationDispatch(eventType, NotificationChannel.InApp, NotificationDeliveryStatus.Skipped);
            logger.LogInformation(
                "Skipped notification {EventType} for user {UserId} because preferences disabled the event.",
                eventType,
                userId);
            return;
        }

        try
        {
            var notificationPayload = new NotificationMessagePayload
            {
                EventType = eventType,
                UserId = userId,
                ReservationId = reservationId,
                Title = title,
                Message = message,
                CreatedAtUtc = DateTime.UtcNow,
                Data = payload
            };

            await realtimeNotificationService.NotifyUserNotificationAsync(userId, notificationPayload, cancellationToken);
            await WriteLogAsync(userId, reservationId, eventType, NotificationChannel.InApp, NotificationDeliveryStatus.Sent, title, message, payload, deliveryKey, null, cancellationToken);
            metrics.RecordNotificationDispatch(eventType, NotificationChannel.InApp, NotificationDeliveryStatus.Sent);
            logger.LogInformation(
                "Dispatched notification {EventType} for user {UserId} and reservation {ReservationId}.",
                eventType,
                userId,
                reservationId);
        }
        catch (Exception exception)
        {
            await WriteLogAsync(userId, reservationId, eventType, NotificationChannel.InApp, NotificationDeliveryStatus.Failed, title, message, payload, deliveryKey, exception.Message, cancellationToken);
            metrics.RecordNotificationDispatch(eventType, NotificationChannel.InApp, NotificationDeliveryStatus.Failed);
            logger.LogError(
                exception,
                "Failed to dispatch notification {EventType} for user {UserId} and reservation {ReservationId}.",
                eventType,
                userId,
                reservationId);
        }
    }

    private async Task WriteLogAsync(
        Guid userId,
        Guid reservationId,
        NotificationEventType eventType,
        NotificationChannel channel,
        NotificationDeliveryStatus status,
        string title,
        string message,
        object payload,
        string? deliveryKey,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        await context.NotificationDeliveryLogs.AddAsync(new NotificationDeliveryLog
        {
            UserId = userId,
            ReservationId = reservationId,
            EventType = eventType,
            Channel = channel,
            Status = status,
            DeliveryKey = deliveryKey,
            Title = title,
            Message = message,
            PayloadJson = JsonSerializer.Serialize(payload),
            ErrorMessage = errorMessage,
            DeliveredAtUtc = status == NotificationDeliveryStatus.Sent ? DateTime.UtcNow : null
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool IsEventEnabled(NotificationPreference preference, NotificationEventType eventType)
    {
        return eventType switch
        {
            NotificationEventType.ReservationConfirmed => preference.ReservationConfirmedEnabled,
            NotificationEventType.ReservationReadyForPickup => preference.ReservationReadyEnabled,
            NotificationEventType.ReservationCompleted => true,
            NotificationEventType.ReservationCancelled => preference.ReservationCancelledEnabled,
            NotificationEventType.ReservationExpired => preference.ReservationExpiredEnabled,
            NotificationEventType.ReservationExpiringSoon => preference.ReservationExpiringSoonEnabled,
            _ => true
        };
    }

    private static (string Title, string Message) BuildStatusContent(Reservation reservation, NotificationEventType eventType)
    {
        return eventType switch
        {
            NotificationEventType.ReservationConfirmed => (
                "Reservation confirmed",
                $"Reservation {reservation.ReservationNumber} has been confirmed and is held until {reservation.ReservedUntilUtc:HH:mm} UTC."),
            NotificationEventType.ReservationReadyForPickup => (
                "Reservation ready for pickup",
                $"Reservation {reservation.ReservationNumber} is ready for pickup at {reservation.Pharmacy?.Name ?? "the selected pharmacy"}."),
            NotificationEventType.ReservationCompleted => (
                "Reservation completed",
                $"Reservation {reservation.ReservationNumber} has been picked up successfully."),
            NotificationEventType.ReservationCancelled => (
                "Reservation cancelled",
                $"Reservation {reservation.ReservationNumber} has been cancelled."),
            NotificationEventType.ReservationExpired => (
                "Reservation expired",
                $"Reservation {reservation.ReservationNumber} expired before pickup."),
            _ => (
                "Reservation updated",
                $"Reservation {reservation.ReservationNumber} changed status.")
        };
    }
}

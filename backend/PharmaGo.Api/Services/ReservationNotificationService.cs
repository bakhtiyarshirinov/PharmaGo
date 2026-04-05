using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmaGo.Api.Background;
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
    IOptions<ReservationNotificationSettings> settings) : IReservationNotificationService
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
                reservation.PharmacyId,
                PharmacyName = reservation.Pharmacy?.Name
            },
            cancellationToken);
    }

    public async Task<int> DispatchExpiringSoonNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var reminderBoundary = now.AddMinutes(Math.Max(5, _settings.ExpiringSoonLeadMinutes));

        var reservations = await context.Reservations
            .AsNoTracking()
            .Include(x => x.Pharmacy)
            .Where(x =>
                (x.Status == ReservationStatus.Confirmed || x.Status == ReservationStatus.ReadyForPickup) &&
                x.ReservedUntilUtc > now &&
                x.ReservedUntilUtc <= reminderBoundary)
            .ToListAsync(cancellationToken);

        var dispatched = 0;

        foreach (var reservation in reservations)
        {
            var alreadySent = await context.NotificationDeliveryLogs
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UserId == reservation.CustomerId &&
                    x.ReservationId == reservation.Id &&
                    x.EventType == NotificationEventType.ReservationExpiringSoon &&
                    x.Channel == NotificationChannel.InApp &&
                    x.Status == NotificationDeliveryStatus.Sent,
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
                $"Reservation {reservation.ReservationNumber} at {reservation.Pharmacy?.Name ?? "your pharmacy"} expires at {reservation.ReservedUntilUtc:HH:mm} UTC.",
                new
                {
                    reservation.Id,
                    reservation.ReservationNumber,
                    Status = reservation.Status.ToString(),
                    reservation.ReservedUntilUtc,
                    reservation.PharmacyId,
                    PharmacyName = reservation.Pharmacy?.Name
                },
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
            await WriteLogAsync(userId, reservationId, eventType, NotificationChannel.InApp, NotificationDeliveryStatus.Skipped, title, message, payload, null, cancellationToken);
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
            await WriteLogAsync(userId, reservationId, eventType, NotificationChannel.InApp, NotificationDeliveryStatus.Sent, title, message, payload, null, cancellationToken);
        }
        catch (Exception exception)
        {
            await WriteLogAsync(userId, reservationId, eventType, NotificationChannel.InApp, NotificationDeliveryStatus.Failed, title, message, payload, exception.Message, cancellationToken);
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

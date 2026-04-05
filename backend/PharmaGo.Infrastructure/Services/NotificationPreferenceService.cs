using Microsoft.EntityFrameworkCore;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Notifications.Contracts;
using PharmaGo.Domain.Models;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.Infrastructure.Services;

public class NotificationPreferenceService(ApplicationDbContext context) : INotificationPreferenceService
{
    public async Task<NotificationPreferencesResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var preference = await GetOrCreateAsync(userId, cancellationToken);
        var telegramLinked = await context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.TelegramChatId != null)
            .FirstOrDefaultAsync(cancellationToken);

        return Map(preference, telegramLinked);
    }

    public async Task<NotificationPreferencesResponse> UpdateAsync(
        Guid userId,
        UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var preference = await GetOrCreateAsync(userId, cancellationToken);

        preference.InAppEnabled = request.InAppEnabled;
        preference.TelegramEnabled = request.TelegramEnabled;
        preference.ReservationConfirmedEnabled = request.ReservationConfirmedEnabled;
        preference.ReservationReadyEnabled = request.ReservationReadyEnabled;
        preference.ReservationCancelledEnabled = request.ReservationCancelledEnabled;
        preference.ReservationExpiredEnabled = request.ReservationExpiredEnabled;
        preference.ReservationExpiringSoonEnabled = request.ReservationExpiringSoonEnabled;

        await context.SaveChangesAsync(cancellationToken);

        var telegramLinked = await context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.TelegramChatId != null)
            .FirstAsync(cancellationToken);

        return Map(preference, telegramLinked);
    }

    private async Task<NotificationPreference> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var preference = await context.NotificationPreferences
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (preference is not null)
        {
            return preference;
        }

        preference = new NotificationPreference
        {
            UserId = userId
        };

        await context.NotificationPreferences.AddAsync(preference, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return preference;
    }

    private static NotificationPreferencesResponse Map(NotificationPreference preference, bool telegramLinked)
    {
        return new NotificationPreferencesResponse
        {
            InAppEnabled = preference.InAppEnabled,
            TelegramEnabled = preference.TelegramEnabled,
            TelegramLinked = telegramLinked,
            ReservationConfirmedEnabled = preference.ReservationConfirmedEnabled,
            ReservationReadyEnabled = preference.ReservationReadyEnabled,
            ReservationCancelledEnabled = preference.ReservationCancelledEnabled,
            ReservationExpiredEnabled = preference.ReservationExpiredEnabled,
            ReservationExpiringSoonEnabled = preference.ReservationExpiringSoonEnabled
        };
    }
}

namespace PharmaGo.Application.Notifications.Contracts;

public class NotificationPreferencesResponse
{
    public bool InAppEnabled { get; init; }
    public bool TelegramEnabled { get; init; }
    public bool TelegramLinked { get; init; }
    public bool ReservationConfirmedEnabled { get; init; }
    public bool ReservationReadyEnabled { get; init; }
    public bool ReservationCancelledEnabled { get; init; }
    public bool ReservationExpiredEnabled { get; init; }
    public bool ReservationExpiringSoonEnabled { get; init; }
}

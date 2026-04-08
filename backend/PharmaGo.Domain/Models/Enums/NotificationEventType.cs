namespace PharmaGo.Domain.Models.Enums;

public enum NotificationEventType
{
    ReservationConfirmed = 1,
    ReservationReadyForPickup = 2,
    ReservationCancelled = 3,
    ReservationExpired = 4,
    ReservationExpiringSoon = 5,
    ReservationCompleted = 6
}

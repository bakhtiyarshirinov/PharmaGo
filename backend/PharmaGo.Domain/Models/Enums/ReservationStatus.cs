namespace PharmaGo.Domain.Models.Enums;

public enum ReservationStatus
{
    Pending = 1,
    Confirmed = 2,
    ReadyForPickup = 3,
    Completed = 4,
    Cancelled = 5,
    Expired = 6
}

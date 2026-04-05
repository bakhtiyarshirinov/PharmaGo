using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Notifications.Contracts;

public class UpdateNotificationPreferencesRequest
{
    [Required]
    public bool InAppEnabled { get; init; } = true;

    [Required]
    public bool TelegramEnabled { get; init; }

    [Required]
    public bool ReservationConfirmedEnabled { get; init; } = true;

    [Required]
    public bool ReservationReadyEnabled { get; init; } = true;

    [Required]
    public bool ReservationCancelledEnabled { get; init; } = true;

    [Required]
    public bool ReservationExpiredEnabled { get; init; } = true;

    [Required]
    public bool ReservationExpiringSoonEnabled { get; init; } = true;
}

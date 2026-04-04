using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Application.Reservations.Commands.UpdateReservationStatus;

public class UpdateReservationStatusRequest
{
    public ReservationStatus Status { get; init; }
}

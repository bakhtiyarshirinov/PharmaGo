using Microsoft.AspNetCore.Http;
using PharmaGo.Application.Abstractions;
using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Infrastructure.Services;

public class ReservationTransitionPolicy : IReservationTransitionPolicy
{
    public ReservationTransitionDecision Evaluate(
        ReservationStatus currentStatus,
        ReservationStatus nextStatus,
        bool isOwner,
        bool isPharmacist,
        bool isModerator)
    {
        if (isOwner)
        {
            if (nextStatus != ReservationStatus.Cancelled)
            {
                return Forbidden("reservation_transition_forbidden", "Customers can only cancel their own reservations.");
            }

            return currentStatus is ReservationStatus.Pending or ReservationStatus.Confirmed or ReservationStatus.ReadyForPickup
                ? Allowed()
                : Unprocessable("reservation_transition_invalid", $"Reservation cannot be cancelled from status '{currentStatus}'.");
        }

        if (!isPharmacist && !isModerator)
        {
            return Forbidden("reservation_transition_forbidden", "You are not allowed to change this reservation.");
        }

        return (currentStatus, nextStatus) switch
        {
            (ReservationStatus.Pending, ReservationStatus.Confirmed) => Allowed(),
            (ReservationStatus.Pending, ReservationStatus.Cancelled) => Allowed(),
            (ReservationStatus.Pending, ReservationStatus.Expired) => Allowed(),
            (ReservationStatus.Confirmed, ReservationStatus.ReadyForPickup) => Allowed(),
            (ReservationStatus.Confirmed, ReservationStatus.Cancelled) => Allowed(),
            (ReservationStatus.Confirmed, ReservationStatus.Expired) => Allowed(),
            (ReservationStatus.ReadyForPickup, ReservationStatus.Completed) => Allowed(),
            (ReservationStatus.ReadyForPickup, ReservationStatus.Cancelled) => Allowed(),
            (ReservationStatus.ReadyForPickup, ReservationStatus.Expired) => Allowed(),
            _ => Unprocessable(
                "reservation_transition_invalid",
                $"Reservation cannot transition from '{currentStatus}' to '{nextStatus}'.")
        };
    }

    private static ReservationTransitionDecision Allowed()
        => new(true, StatusCodes.Status200OK, string.Empty, string.Empty);

    private static ReservationTransitionDecision Forbidden(string code, string message)
        => new(false, StatusCodes.Status403Forbidden, code, message);

    private static ReservationTransitionDecision Unprocessable(string code, string message)
        => new(false, StatusCodes.Status422UnprocessableEntity, code, message);
}

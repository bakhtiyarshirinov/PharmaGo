using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Application.Abstractions;

public interface IReservationTransitionPolicy
{
    ReservationTransitionDecision Evaluate(
        ReservationStatus currentStatus,
        ReservationStatus nextStatus,
        bool isOwner,
        bool isPharmacist,
        bool isModerator);
}

public sealed record ReservationTransitionDecision(
    bool IsAllowed,
    int StatusCode,
    string Code,
    string Message);

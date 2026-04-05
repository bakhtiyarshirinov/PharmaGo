using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Application.Reservations.Queries.GetReservationTimeline;

public class ReservationTimelineEventResponse
{
    public string Action { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ReservationStatus? Status { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid? UserId { get; init; }
    public string? UserFullName { get; init; }
    public bool IsSystemEvent { get; init; }
}

using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Application.Reservations.Queries.GetReservation;

public class ReservationResponse
{
    public Guid ReservationId { get; init; }
    public string ReservationNumber { get; init; } = string.Empty;
    public ReservationStatus Status { get; init; }
    public Guid PharmacyId { get; init; }
    public string PharmacyName { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string CustomerFullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime ReservedUntilUtc { get; init; }
    public DateTime? ConfirmedAtUtc { get; init; }
    public DateTime? ReadyForPickupAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public DateTime? CancelledAtUtc { get; init; }
    public DateTime? ExpiredAtUtc { get; init; }
    public decimal TotalAmount { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyCollection<ReservationItemResponse> Items { get; init; } = Array.Empty<ReservationItemResponse>();
}

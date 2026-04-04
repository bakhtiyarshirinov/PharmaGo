namespace PharmaGo.Application.Reservations.Commands.CreateReservation;

public class CreateReservationRequest
{
    public Guid PharmacyId { get; init; }
    public string? Notes { get; init; }
    public int ReserveForHours { get; init; } = 2;
    public IReadOnlyCollection<CreateReservationItemRequest> Items { get; init; } = Array.Empty<CreateReservationItemRequest>();
}

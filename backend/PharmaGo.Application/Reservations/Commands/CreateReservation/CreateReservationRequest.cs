namespace PharmaGo.Application.Reservations.Commands.CreateReservation;

public class CreateReservationRequest
{
    public Guid PharmacyId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? TelegramUsername { get; init; }
    public string? TelegramChatId { get; init; }
    public string? Notes { get; init; }
    public int ReserveForHours { get; init; } = 2;
    public IReadOnlyCollection<CreateReservationItemRequest> Items { get; init; } = Array.Empty<CreateReservationItemRequest>();
}

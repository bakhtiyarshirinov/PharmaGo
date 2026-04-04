using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Reservations.Commands.CreateReservation;

public class CreateReservationRequest
{
    [Required]
    public Guid PharmacyId { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }

    [Range(1, 24)]
    public int ReserveForHours { get; init; } = 2;

    [MinLength(1)]
    public IReadOnlyCollection<CreateReservationItemRequest> Items { get; init; } = Array.Empty<CreateReservationItemRequest>();
}

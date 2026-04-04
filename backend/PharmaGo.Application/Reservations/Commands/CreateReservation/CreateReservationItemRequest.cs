using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Reservations.Commands.CreateReservation;

public class CreateReservationItemRequest
{
    [Required]
    public Guid MedicineId { get; init; }

    [Range(1, 1000)]
    public int Quantity { get; init; }
}

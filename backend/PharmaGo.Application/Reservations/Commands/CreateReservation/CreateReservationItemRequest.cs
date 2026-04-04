namespace PharmaGo.Application.Reservations.Commands.CreateReservation;

public class CreateReservationItemRequest
{
    public Guid MedicineId { get; init; }
    public int Quantity { get; init; }
}

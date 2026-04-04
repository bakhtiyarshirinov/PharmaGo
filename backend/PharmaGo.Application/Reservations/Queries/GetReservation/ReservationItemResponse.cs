namespace PharmaGo.Application.Reservations.Queries.GetReservation;

public class ReservationItemResponse
{
    public Guid MedicineId { get; init; }
    public string MedicineName { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}

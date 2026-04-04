namespace PharmaGo.Domain.Models;

public class ReservationItem : BaseEntity
{
    public Guid ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public Guid MedicineId { get; set; }
    public Medicine? Medicine { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}

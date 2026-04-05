namespace PharmaGo.Domain.Models;

public class ReservationIdempotencyRecord : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;

    public Guid? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}

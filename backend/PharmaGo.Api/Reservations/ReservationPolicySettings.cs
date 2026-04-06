namespace PharmaGo.Api.Reservations;

public class ReservationPolicySettings
{
    public const string SectionName = "ReservationPolicy";

    public int ReservationLifetimeHours { get; set; } = 2;
    public int MaxActiveReservationsPerUser { get; set; } = 3;
}

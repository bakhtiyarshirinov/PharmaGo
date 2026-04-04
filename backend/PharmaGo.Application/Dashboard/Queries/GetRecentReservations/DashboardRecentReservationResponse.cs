using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Application.Dashboard.Queries.GetRecentReservations;

public class DashboardRecentReservationResponse
{
    public Guid ReservationId { get; init; }
    public string ReservationNumber { get; init; } = string.Empty;
    public ReservationStatus Status { get; init; }
    public string CustomerFullName { get; init; } = string.Empty;
    public string PharmacyName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime ReservedUntilUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

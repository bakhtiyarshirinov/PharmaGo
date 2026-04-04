namespace PharmaGo.Application.Dashboard.Queries.GetDashboardSummary;

public class DashboardSummaryResponse
{
    public Guid? PharmacyId { get; init; }
    public string? PharmacyName { get; init; }
    public int TotalMedicines { get; init; }
    public int TotalPharmacies { get; init; }
    public int TotalUsers { get; init; }
    public int TotalStockItems { get; init; }
    public int TotalAvailableUnits { get; init; }
    public int TotalReservedUnits { get; init; }
    public int ActiveReservations { get; init; }
    public int ReadyForPickupReservations { get; init; }
    public int LowStockAlerts { get; init; }
    public int CompletedToday { get; init; }
    public decimal ReservedValue { get; init; }
}

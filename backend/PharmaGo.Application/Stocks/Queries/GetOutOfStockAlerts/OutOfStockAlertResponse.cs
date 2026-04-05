namespace PharmaGo.Application.Stocks.Queries.GetOutOfStockAlerts;

public class OutOfStockAlertResponse
{
    public Guid PharmacyId { get; init; }
    public string PharmacyName { get; init; } = string.Empty;
    public Guid MedicineId { get; init; }
    public string MedicineName { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public int BatchCount { get; init; }
    public int TotalQuantity { get; init; }
    public int TotalReservedQuantity { get; init; }
    public int TotalAvailableQuantity { get; init; }
    public int ReorderLevel { get; init; }
    public DateOnly? NearestExpirationDate { get; init; }
    public DateTime? LastStockUpdatedAtUtc { get; init; }
}

namespace PharmaGo.Application.Stocks.Queries.GetExpiringStockAlerts;

public class ExpiringStockAlertResponse
{
    public Guid StockItemId { get; init; }
    public Guid PharmacyId { get; init; }
    public string PharmacyName { get; init; } = string.Empty;
    public Guid MedicineId { get; init; }
    public string MedicineName { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public string BatchNumber { get; init; } = string.Empty;
    public DateOnly ExpirationDate { get; init; }
    public int DaysUntilExpiration { get; init; }
    public int Quantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public decimal RetailPrice { get; init; }
    public DateTime? LastStockUpdatedAtUtc { get; init; }
}

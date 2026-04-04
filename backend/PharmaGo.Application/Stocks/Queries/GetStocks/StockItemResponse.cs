namespace PharmaGo.Application.Stocks.Queries.GetStocks;

public class StockItemResponse
{
    public Guid Id { get; init; }
    public Guid PharmacyId { get; init; }
    public string PharmacyName { get; init; } = string.Empty;
    public Guid MedicineId { get; init; }
    public string MedicineName { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public string BatchNumber { get; init; } = string.Empty;
    public DateOnly ExpirationDate { get; init; }
    public int Quantity { get; init; }
    public int ReservedQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal RetailPrice { get; init; }
    public int ReorderLevel { get; init; }
    public bool IsLowStock { get; init; }
    public bool IsActive { get; init; }
}

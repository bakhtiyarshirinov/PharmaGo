namespace PharmaGo.Application.Stocks.Commands.CreateStockItem;

public class CreateStockItemRequest
{
    public Guid PharmacyId { get; init; }
    public Guid MedicineId { get; init; }
    public string BatchNumber { get; init; } = string.Empty;
    public DateOnly ExpirationDate { get; init; }
    public int Quantity { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal RetailPrice { get; init; }
    public int ReorderLevel { get; init; }
}

namespace PharmaGo.Application.Stocks.Commands.UpdateStockItem;

public class UpdateStockItemRequest
{
    public string BatchNumber { get; init; } = string.Empty;
    public DateOnly ExpirationDate { get; init; }
    public int Quantity { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal RetailPrice { get; init; }
    public int ReorderLevel { get; init; }
    public bool IsActive { get; init; } = true;
}

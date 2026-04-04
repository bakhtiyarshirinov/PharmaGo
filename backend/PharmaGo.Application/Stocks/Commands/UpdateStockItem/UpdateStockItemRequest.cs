using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Stocks.Commands.UpdateStockItem;

public class UpdateStockItemRequest
{
    [Required]
    [MaxLength(64)]
    public string BatchNumber { get; init; } = string.Empty;

    public DateOnly ExpirationDate { get; init; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; init; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal PurchasePrice { get; init; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal RetailPrice { get; init; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; init; }

    public bool IsActive { get; init; } = true;
}

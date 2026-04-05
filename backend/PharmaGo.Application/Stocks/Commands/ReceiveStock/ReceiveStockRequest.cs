using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Stocks.Commands.ReceiveStock;

public class ReceiveStockRequest
{
    [Range(1, int.MaxValue)]
    public int QuantityReceived { get; init; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal? PurchasePrice { get; init; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal? RetailPrice { get; init; }

    [Range(0, int.MaxValue)]
    public int? ReorderLevel { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }
}

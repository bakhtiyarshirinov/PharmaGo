using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Stocks.Commands.AdjustStockQuantity;

public class AdjustStockQuantityRequest
{
    [Range(-999999999, 999999999)]
    public int QuantityDelta { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }
}

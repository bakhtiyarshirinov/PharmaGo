using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Stocks.Commands.WriteOffStock;

public class WriteOffStockRequest
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }
}

namespace PharmaGo.Application.Stocks.Queries.GetRestockSuggestions;

public class RestockSuggestionResponse
{
    public Guid StockItemId { get; set; }
    public Guid PharmacyId { get; set; }
    public string PharmacyName { get; set; } = string.Empty;
    public Guid MedicineId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public int Deficit { get; set; }
    public int SuggestedOrderQuantity { get; set; }
    public Guid DepotId { get; set; }
    public string DepotName { get; set; } = string.Empty;
    public int SupplierAvailableQuantity { get; set; }
    public int MinimumOrderQuantity { get; set; }
    public int EstimatedDeliveryHours { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal EstimatedWholesaleCost { get; set; }
}

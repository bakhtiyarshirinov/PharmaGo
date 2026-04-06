namespace PharmaGo.Application.MasterData.Contracts;

public class ManagedDepotResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string? ContactPhone { get; init; }
    public string? ContactEmail { get; init; }
    public bool IsActive { get; init; }
    public int SupplierOfferCount { get; init; }
}

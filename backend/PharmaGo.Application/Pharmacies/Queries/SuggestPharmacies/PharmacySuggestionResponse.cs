namespace PharmaGo.Application.Pharmacies.Queries.SuggestPharmacies;

public class PharmacySuggestionResponse
{
    public Guid PharmacyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ChainName { get; init; }
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public bool IsOpen24Hours { get; init; }
    public bool SupportsReservations { get; init; }
    public bool HasDelivery { get; init; }
}

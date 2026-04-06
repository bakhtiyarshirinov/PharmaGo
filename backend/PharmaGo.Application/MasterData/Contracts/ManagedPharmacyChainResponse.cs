namespace PharmaGo.Application.MasterData.Contracts;

public class ManagedPharmacyChainResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? LegalName { get; init; }
    public string? TaxNumber { get; init; }
    public string? SupportPhone { get; init; }
    public string? SupportEmail { get; init; }
    public bool IsActive { get; init; }
    public int PharmacyCount { get; init; }
}

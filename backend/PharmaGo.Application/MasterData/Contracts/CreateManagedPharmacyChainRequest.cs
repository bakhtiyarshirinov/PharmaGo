using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.MasterData.Contracts;

public class CreateManagedPharmacyChainRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [StringLength(250)]
    public string? LegalName { get; init; }

    [StringLength(64)]
    public string? TaxNumber { get; init; }

    [StringLength(32)]
    public string? SupportPhone { get; init; }

    [StringLength(256)]
    [EmailAddress]
    public string? SupportEmail { get; init; }

    public bool IsActive { get; init; } = true;
}

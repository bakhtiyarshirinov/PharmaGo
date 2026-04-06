using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.MasterData.Contracts;

public class CreateManagedDepotRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(400)]
    public string Address { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; init; } = string.Empty;

    [StringLength(32)]
    public string? ContactPhone { get; init; }

    [StringLength(256)]
    [EmailAddress]
    public string? ContactEmail { get; init; }

    public bool IsActive { get; init; } = true;
}

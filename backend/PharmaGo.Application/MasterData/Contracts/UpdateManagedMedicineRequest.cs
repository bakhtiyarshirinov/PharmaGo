using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.MasterData.Contracts;

public class UpdateManagedMedicineRequest
{
    [Required]
    [StringLength(200)]
    public string BrandName { get; init; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string GenericName { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [Required]
    [StringLength(100)]
    public string DosageForm { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Strength { get; init; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Manufacturer { get; init; } = string.Empty;

    [StringLength(100)]
    public string? CountryOfOrigin { get; init; }

    [StringLength(64)]
    public string? Barcode { get; init; }

    public bool RequiresPrescription { get; init; }
    public bool IsActive { get; init; } = true;
    public Guid? CategoryId { get; init; }
}

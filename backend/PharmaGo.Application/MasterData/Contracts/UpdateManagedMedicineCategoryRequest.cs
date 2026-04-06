using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.MasterData.Contracts;

public class UpdateManagedMedicineCategoryRequest
{
    [Required]
    [StringLength(150)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }
}

using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.MasterData.Contracts;

public class CreateManagedSupplierMedicineRequest
{
    [Required]
    public Guid DepotId { get; init; }

    [Required]
    public Guid MedicineId { get; init; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal WholesalePrice { get; init; }

    [Range(0, int.MaxValue)]
    public int AvailableQuantity { get; init; }

    [Range(1, int.MaxValue)]
    public int MinimumOrderQuantity { get; init; }

    [Range(0, int.MaxValue)]
    public int EstimatedDeliveryHours { get; init; }

    public bool IsAvailable { get; init; } = true;
}

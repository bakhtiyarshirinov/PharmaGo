using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Medicines.Queries.GetMedicineAvailability;

public class GetMedicineAvailabilityRequest
{
    public Guid MedicineId { get; set; }

    [MaxLength(100)]
    public string? City { get; init; }

    [Range(-90d, 90d)]
    public double? Latitude { get; init; }

    [Range(-180d, 180d)]
    public double? Longitude { get; init; }

    [Range(0.1d, 100d)]
    public double RadiusKm { get; init; } = 10;

    public bool? OpenNow { get; init; }
    public bool? OnlyReservable { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "distance";
}

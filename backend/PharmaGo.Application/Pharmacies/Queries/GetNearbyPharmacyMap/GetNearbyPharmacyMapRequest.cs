using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Pharmacies.Queries.GetNearbyPharmacyMap;

public class GetNearbyPharmacyMapRequest
{
    [Range(-90d, 90d)]
    public double? Latitude { get; init; }

    [Range(-180d, 180d)]
    public double? Longitude { get; init; }

    [Range(0.1d, 100d)]
    public double RadiusKm { get; init; } = 10;

    [MaxLength(200)]
    public string? Query { get; init; }

    [MaxLength(200)]
    public string? MedicineQuery { get; init; }

    public bool? OpenNow { get; init; }
    public bool? SupportsReservations { get; init; }
    public bool? HasDelivery { get; init; }

    [Range(1, 500)]
    public int Limit { get; init; } = 100;
}

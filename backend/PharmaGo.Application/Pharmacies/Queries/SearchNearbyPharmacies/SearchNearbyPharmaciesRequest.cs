using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Pharmacies.Queries.SearchNearbyPharmacies;

public class SearchNearbyPharmaciesRequest
{
    [MaxLength(200)]
    public string? Query { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [Range(-90d, 90d)]
    public double? Latitude { get; init; }

    [Range(-180d, 180d)]
    public double? Longitude { get; init; }

    [Range(0.1d, 100d)]
    public double RadiusKm { get; init; } = 10;

    public bool? OpenNow { get; init; }
    public bool? SupportsReservations { get; init; }
    public bool? HasDelivery { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [MaxLength(32)]
    public string SortBy { get; init; } = "distance";

    [MaxLength(4)]
    public string SortDirection { get; init; } = "asc";
}

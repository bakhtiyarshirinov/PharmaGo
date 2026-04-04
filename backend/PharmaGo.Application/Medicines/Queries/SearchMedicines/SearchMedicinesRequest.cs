using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Medicines.Queries.SearchMedicines;

public class SearchMedicinesRequest
{
    [Required]
    [MaxLength(200)]
    public string Query { get; init; } = string.Empty;

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
    public string SortBy { get; init; } = "relevance";

    [Range(1, 50)]
    public int Limit { get; init; } = 20;

    [Range(1, 20)]
    public int AvailabilityLimit { get; init; } = 5;
}

using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Pharmacies.Queries.GetPharmacyMedicines;

public class GetPharmacyMedicinesRequest
{
    [MaxLength(200)]
    public string? Query { get; init; }

    public Guid? CategoryId { get; init; }
    public bool InStockOnly { get; init; } = true;
    public bool? OnlyReservable { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [MaxLength(32)]
    public string SortBy { get; init; } = "name";

    [MaxLength(4)]
    public string SortDirection { get; init; } = "asc";
}

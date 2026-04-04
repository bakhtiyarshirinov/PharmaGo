using PharmaGo.Application.Medicines.Queries.SearchMedicines;

namespace PharmaGo.Application.Abstractions;

public interface IMedicineSearchService
{
    Task<IReadOnlyCollection<MedicineSearchResponse>> SearchAsync(
        SearchMedicinesRequest request,
        CancellationToken cancellationToken = default);
}

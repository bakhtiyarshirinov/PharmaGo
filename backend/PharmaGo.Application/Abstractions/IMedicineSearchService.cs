using PharmaGo.Application.Medicines.Queries.SearchMedicines;

namespace PharmaGo.Application.Abstractions;

public interface IMedicineSearchService
{
    Task<IReadOnlyCollection<MedicineSearchResponse>> SearchAsync(
        SearchMedicinesRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MedicineSuggestionResponse>> SuggestAsync(
        string query,
        int limit = 8,
        CancellationToken cancellationToken = default);
}

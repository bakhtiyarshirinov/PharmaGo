using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Pharmacies.Queries.GetNearbyPharmacyMap;
using PharmaGo.Application.Pharmacies.Queries.SearchNearbyPharmacies;
using PharmaGo.Application.Pharmacies.Queries.SuggestPharmacies;

namespace PharmaGo.Application.Abstractions;

public interface IPharmacyDiscoveryService
{
    Task<PagedResponse<NearbyPharmacyResponse>> SearchAsync(
        SearchNearbyPharmaciesRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PharmacySuggestionResponse>> SuggestAsync(
        string query,
        int limit = 8,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<NearbyPharmacyMapResponse>> GetMapAsync(
        GetNearbyPharmacyMapRequest request,
        CancellationToken cancellationToken = default);
}

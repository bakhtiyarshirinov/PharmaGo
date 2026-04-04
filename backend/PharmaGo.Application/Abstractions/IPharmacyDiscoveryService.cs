using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Pharmacies.Queries.SearchNearbyPharmacies;

namespace PharmaGo.Application.Abstractions;

public interface IPharmacyDiscoveryService
{
    Task<PagedResponse<NearbyPharmacyResponse>> SearchAsync(
        SearchNearbyPharmaciesRequest request,
        CancellationToken cancellationToken = default);
}

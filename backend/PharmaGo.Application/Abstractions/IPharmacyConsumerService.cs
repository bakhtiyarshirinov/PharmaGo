using PharmaGo.Application.Pharmacies.Queries.GetConsumerPharmacyFeed;

namespace PharmaGo.Application.Abstractions;

public interface IPharmacyConsumerService
{
    Task<IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>> GetPopularAsync(
        Guid? userId,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>> GetFavoritesAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>> GetRecentAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<bool> AddFavoriteAsync(
        Guid userId,
        Guid pharmacyId,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveFavoriteAsync(
        Guid userId,
        Guid pharmacyId,
        CancellationToken cancellationToken = default);

    Task RecordViewAsync(
        Guid userId,
        Guid pharmacyId,
        CancellationToken cancellationToken = default);
}

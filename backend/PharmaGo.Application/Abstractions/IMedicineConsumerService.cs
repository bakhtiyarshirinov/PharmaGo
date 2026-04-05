using PharmaGo.Application.Medicines.Queries.GetConsumerMedicineFeed;

namespace PharmaGo.Application.Abstractions;

public interface IMedicineConsumerService
{
    Task<IReadOnlyCollection<ConsumerMedicineFeedItemResponse>> GetPopularAsync(
        Guid? userId,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ConsumerMedicineFeedItemResponse>> GetFavoritesAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ConsumerMedicineFeedItemResponse>> GetRecentAsync(
        Guid userId,
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<bool> AddFavoriteAsync(
        Guid userId,
        Guid medicineId,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveFavoriteAsync(
        Guid userId,
        Guid medicineId,
        CancellationToken cancellationToken = default);

    Task RecordViewAsync(
        Guid userId,
        Guid medicineId,
        CancellationToken cancellationToken = default);
}

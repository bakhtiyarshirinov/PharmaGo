using PharmaGo.Application.Medicines.Queries.GetMedicineDetail;
using PharmaGo.Application.Medicines.Queries.GetMedicineRecommendations;

namespace PharmaGo.Application.Abstractions;

public interface IMedicineCatalogService
{
    Task<MedicineDetailResponse?> GetByIdAsync(
        Guid medicineId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MedicineRecommendationResponse>?> GetSubstitutionsAsync(
        Guid medicineId,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MedicineRecommendationResponse>?> GetSimilarAsync(
        Guid medicineId,
        int limit = 10,
        CancellationToken cancellationToken = default);
}

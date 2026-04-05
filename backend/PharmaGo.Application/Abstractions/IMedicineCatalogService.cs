using PharmaGo.Application.Medicines.Queries.GetMedicineDetail;

namespace PharmaGo.Application.Abstractions;

public interface IMedicineCatalogService
{
    Task<MedicineDetailResponse?> GetByIdAsync(
        Guid medicineId,
        CancellationToken cancellationToken = default);
}

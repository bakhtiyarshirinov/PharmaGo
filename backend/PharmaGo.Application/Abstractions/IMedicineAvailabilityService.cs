using PharmaGo.Application.Medicines.Queries.GetMedicineAvailability;

namespace PharmaGo.Application.Abstractions;

public interface IMedicineAvailabilityService
{
    Task<MedicineAvailabilityResponse?> GetAvailabilityAsync(
        GetMedicineAvailabilityRequest request,
        CancellationToken cancellationToken = default);
}

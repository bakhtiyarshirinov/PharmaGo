using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Pharmacies.Queries.GetPharmacyDetail;
using PharmaGo.Application.Pharmacies.Queries.GetPharmacyMedicines;

namespace PharmaGo.Application.Abstractions;

public interface IPharmacyCatalogService
{
    Task<PharmacyDetailResponse?> GetByIdAsync(
        Guid pharmacyId,
        double? latitude,
        double? longitude,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<PharmacyMedicineResponse>?> GetMedicinesAsync(
        Guid pharmacyId,
        GetPharmacyMedicinesRequest request,
        CancellationToken cancellationToken = default);
}

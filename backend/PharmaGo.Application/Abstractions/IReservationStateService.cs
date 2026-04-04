using PharmaGo.Domain.Models;

namespace PharmaGo.Application.Abstractions;

public interface IReservationStateService
{
    Task ReleaseReservedStockAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task CompleteReservationAsync(Reservation reservation, CancellationToken cancellationToken = default);
}

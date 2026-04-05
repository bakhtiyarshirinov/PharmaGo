using PharmaGo.Domain.Models;
using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Application.Abstractions;

public interface IReservationNotificationService
{
    Task DispatchStatusNotificationAsync(
        Reservation reservation,
        ReservationStatus previousStatus,
        CancellationToken cancellationToken = default);

    Task<int> DispatchExpiringSoonNotificationsAsync(CancellationToken cancellationToken = default);
}

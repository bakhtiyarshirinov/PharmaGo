using Microsoft.AspNetCore.SignalR;
using PharmaGo.Api.Hubs;
using PharmaGo.Application.Stocks.Queries.GetLowStockAlerts;
using PharmaGo.Application.Stocks.Queries.GetStocks;

namespace PharmaGo.Api.Realtime;

public class RealtimeNotificationService(IHubContext<NotificationHub> hubContext)
{
    public Task NotifyReservationCreatedAsync(Guid pharmacyId, object payload, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients.Groups($"pharmacy:{pharmacyId}", "role:Moderator")
            .SendAsync(NotificationEvents.ReservationCreated, payload, cancellationToken);
    }

    public Task NotifyReservationStatusChangedAsync(Guid pharmacyId, Guid customerId, object payload, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients.Groups($"pharmacy:{pharmacyId}", $"user:{customerId}", "role:Moderator")
            .SendAsync(NotificationEvents.ReservationStatusChanged, payload, cancellationToken);
    }

    public Task NotifyLowStockAsync(Guid pharmacyId, LowStockAlertResponse payload, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients.Groups($"pharmacy:{pharmacyId}", "role:Moderator")
            .SendAsync(NotificationEvents.StockLow, payload, cancellationToken);
    }

    public Task NotifyStockRestoredAsync(Guid pharmacyId, StockItemResponse payload, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients.Groups($"pharmacy:{pharmacyId}", "role:Moderator")
            .SendAsync(NotificationEvents.StockRestored, payload, cancellationToken);
    }
}

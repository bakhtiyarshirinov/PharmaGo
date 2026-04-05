using Microsoft.Extensions.Options;
using PharmaGo.Application.Abstractions;

namespace PharmaGo.Api.Background;

public class ReservationNotificationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReservationNotificationSettings> settings,
    ILogger<ReservationNotificationWorker> logger) : BackgroundService
{
    private readonly ReservationNotificationSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IReservationNotificationService>();
                var dispatched = await service.DispatchExpiringSoonNotificationsAsync(stoppingToken);

                if (dispatched > 0)
                {
                    logger.LogInformation("Dispatched {Count} reservation expiring soon notifications.", dispatched);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Reservation notification worker iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(30, _settings.PollingIntervalSeconds)), stoppingToken);
        }
    }
}

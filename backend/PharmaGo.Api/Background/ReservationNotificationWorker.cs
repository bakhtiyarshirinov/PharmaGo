using Microsoft.Extensions.Options;
using PharmaGo.Api.Observability;
using PharmaGo.Application.Abstractions;

namespace PharmaGo.Api.Background;

public class ReservationNotificationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ReservationNotificationSettings> settings,
    ILogger<ReservationNotificationWorker> logger,
    BackgroundWorkerExecutionMonitor executionMonitor,
    PharmaGoMetrics metrics) : BackgroundService
{
    public const string WorkerName = "reservation_notifications";
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
                executionMonitor.RecordSuccess(WorkerName, dispatched);
                metrics.RecordBackgroundWorkerRun(WorkerName);
                metrics.RecordBackgroundWorkerProcessed(WorkerName, dispatched);

                if (dispatched > 0)
                {
                    logger.LogInformation("Dispatched {Count} reservation expiring soon notifications.", dispatched);
                }
            }
            catch (Exception exception)
            {
                executionMonitor.RecordFailure(WorkerName, exception);
                metrics.RecordBackgroundWorkerFailure(WorkerName);
                logger.LogError(exception, "Reservation notification worker iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(30, _settings.PollingIntervalSeconds)), stoppingToken);
        }
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PharmaGo.Api.Background;

namespace PharmaGo.Api.Observability;

public class BackgroundWorkersHealthCheck(
    BackgroundWorkerExecutionMonitor monitor,
    IOptions<ReservationExpirationSettings> expirationSettings,
    IOptions<ReservationNotificationSettings> notificationSettings) : IHealthCheck
{
    private static readonly string[] WorkerNames =
    [
        ReservationExpirationWorker.WorkerName,
        ReservationNotificationWorker.WorkerName
    ];

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var snapshot = monitor.GetSnapshot();
        var now = DateTime.UtcNow;
        var maxStaleness = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase)
        {
            [ReservationExpirationWorker.WorkerName] = TimeSpan.FromSeconds(Math.Max(120, expirationSettings.Value.PollingIntervalSeconds * 3)),
            [ReservationNotificationWorker.WorkerName] = TimeSpan.FromSeconds(Math.Max(180, notificationSettings.Value.PollingIntervalSeconds * 3))
        };

        var data = new Dictionary<string, object>();
        var messages = new List<string>();
        var overallStatus = HealthStatus.Healthy;

        foreach (var workerName in WorkerNames)
        {
            snapshot.TryGetValue(workerName, out var state);
            data[workerName] = state is null
                ? new
                {
                    status = "not_started",
                    lastRunAtUtc = (DateTime?)null,
                    lastSucceededAtUtc = (DateTime?)null,
                    lastFailedAtUtc = (DateTime?)null,
                    lastProcessedCount = 0
                }
                : new
                {
                    status = "tracked",
                    state.LastRunAtUtc,
                    state.LastSucceededAtUtc,
                    state.LastFailedAtUtc,
                    state.LastProcessedCount,
                    state.LastError
                };

            var workerMaxStaleness = maxStaleness[workerName];
            if (state is null)
            {
                if (now - monitor.StartedAtUtc > workerMaxStaleness)
                {
                    overallStatus = HealthStatus.Degraded;
                    messages.Add($"{workerName} has not completed a successful run yet.");
                }

                continue;
            }

            if (state.LastSucceededAtUtc.HasValue && now - state.LastSucceededAtUtc.Value > workerMaxStaleness)
            {
                overallStatus = HealthStatus.Degraded;
                messages.Add($"{workerName} is stale.");
            }

            if (state.LastFailedAtUtc.HasValue &&
                (!state.LastSucceededAtUtc.HasValue || state.LastFailedAtUtc.Value >= state.LastSucceededAtUtc.Value))
            {
                overallStatus = HealthStatus.Degraded;
                messages.Add($"{workerName} last run failed.");
            }
        }

        var description = messages.Count == 0
            ? "Background workers are healthy."
            : string.Join(" ", messages);

        return Task.FromResult(new HealthCheckResult(overallStatus, description, data: data));
    }
}

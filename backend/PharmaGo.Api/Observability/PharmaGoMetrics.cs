using System.Diagnostics.Metrics;
using PharmaGo.Domain.Models.Enums;

namespace PharmaGo.Api.Observability;

public sealed class PharmaGoMetrics : IDisposable
{
    private readonly Meter _meter = new("PharmaGo.Api", "1.0.0");
    private readonly Counter<long> _reservationsCreated;
    private readonly Counter<long> _reservationTransitions;
    private readonly Counter<long> _notificationsDispatched;
    private readonly Counter<long> _backgroundWorkerRuns;
    private readonly Counter<long> _backgroundWorkerProcessed;
    private readonly Counter<long> _backgroundWorkerFailures;

    public PharmaGoMetrics()
    {
        _reservationsCreated = _meter.CreateCounter<long>("pharmago_reservations_created_total");
        _reservationTransitions = _meter.CreateCounter<long>("pharmago_reservation_transitions_total");
        _notificationsDispatched = _meter.CreateCounter<long>("pharmago_notifications_dispatched_total");
        _backgroundWorkerRuns = _meter.CreateCounter<long>("pharmago_background_worker_runs_total");
        _backgroundWorkerProcessed = _meter.CreateCounter<long>("pharmago_background_worker_processed_total");
        _backgroundWorkerFailures = _meter.CreateCounter<long>("pharmago_background_worker_failures_total");
    }

    public void RecordReservationCreated()
    {
        _reservationsCreated.Add(1);
    }

    public void RecordReservationTransition(ReservationStatus fromStatus, ReservationStatus toStatus)
    {
        _reservationTransitions.Add(
            1,
            new KeyValuePair<string, object?>("from_status", fromStatus.ToString()),
            new KeyValuePair<string, object?>("to_status", toStatus.ToString()));
    }

    public void RecordNotificationDispatch(
        NotificationEventType eventType,
        NotificationChannel channel,
        NotificationDeliveryStatus status)
    {
        _notificationsDispatched.Add(
            1,
            new KeyValuePair<string, object?>("event_type", eventType.ToString()),
            new KeyValuePair<string, object?>("channel", channel.ToString()),
            new KeyValuePair<string, object?>("status", status.ToString()));
    }

    public void RecordBackgroundWorkerRun(string workerName)
    {
        _backgroundWorkerRuns.Add(1, new KeyValuePair<string, object?>("worker", workerName));
    }

    public void RecordBackgroundWorkerProcessed(string workerName, int processedCount)
    {
        if (processedCount <= 0)
        {
            return;
        }

        _backgroundWorkerProcessed.Add(
            processedCount,
            new KeyValuePair<string, object?>("worker", workerName));
    }

    public void RecordBackgroundWorkerFailure(string workerName)
    {
        _backgroundWorkerFailures.Add(1, new KeyValuePair<string, object?>("worker", workerName));
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}

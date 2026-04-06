using System.Collections.Concurrent;

namespace PharmaGo.Api.Observability;

public sealed class BackgroundWorkerExecutionMonitor
{
    private readonly ConcurrentDictionary<string, BackgroundWorkerExecutionState> _states = new(StringComparer.OrdinalIgnoreCase);

    public DateTime StartedAtUtc { get; } = DateTime.UtcNow;

    public void RecordSuccess(string workerName, int processedCount)
    {
        _states.AddOrUpdate(
            workerName,
            _ => new BackgroundWorkerExecutionState
            {
                LastRunAtUtc = DateTime.UtcNow,
                LastSucceededAtUtc = DateTime.UtcNow,
                LastProcessedCount = processedCount
            },
            (_, state) => state with
            {
                LastRunAtUtc = DateTime.UtcNow,
                LastSucceededAtUtc = DateTime.UtcNow,
                LastProcessedCount = processedCount,
                LastError = null
            });
    }

    public void RecordFailure(string workerName, Exception exception)
    {
        _states.AddOrUpdate(
            workerName,
            _ => new BackgroundWorkerExecutionState
            {
                LastRunAtUtc = DateTime.UtcNow,
                LastFailedAtUtc = DateTime.UtcNow,
                LastError = exception.Message
            },
            (_, state) => state with
            {
                LastRunAtUtc = DateTime.UtcNow,
                LastFailedAtUtc = DateTime.UtcNow,
                LastError = exception.Message
            });
    }

    public IReadOnlyDictionary<string, BackgroundWorkerExecutionState> GetSnapshot()
    {
        return _states;
    }
}

public sealed record BackgroundWorkerExecutionState
{
    public DateTime? LastRunAtUtc { get; init; }
    public DateTime? LastSucceededAtUtc { get; init; }
    public DateTime? LastFailedAtUtc { get; init; }
    public int LastProcessedCount { get; init; }
    public string? LastError { get; init; }
}

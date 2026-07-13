namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Wakes the local outbox publisher when new rows are committed.
/// </summary>
public interface IOutboxSignal
{
    /// <summary>Signals that the publisher should check the outbox immediately.</summary>
    void Notify();

    /// <summary>Waits until a signal arrives, or until the polling fallback interval elapses.</summary>
    Task WaitAsync(TimeSpan fallbackInterval, CancellationToken cancellationToken);
}

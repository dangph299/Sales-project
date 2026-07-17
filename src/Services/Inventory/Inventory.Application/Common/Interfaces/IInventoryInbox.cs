namespace Inventory.Application.Common.Interfaces;

/// <summary>
/// Application port for idempotent integration-event processing.
/// </summary>
public interface IInventoryInbox
{
    /// <summary>
    /// Non-transactional existence check for an already-committed inbox record. Lets a duplicate or
    /// redelivered event return early without opening a SERIALIZABLE transaction, at the cost of one
    /// extra lightweight query on every first delivery. This check can race with a concurrent writer,
    /// so the transactional insert in <see cref="TryRecordAsync"/> remains the authoritative duplicate
    /// barrier; this method never replaces it.
    /// </summary>
    /// <returns><see langword="true"/> when a committed inbox record already exists for the event; otherwise <see langword="false"/>.</returns>
    Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an incoming event id.
    /// </summary>
    /// <returns><see langword="true"/> when this event has not been processed before; otherwise <see langword="false"/>.</returns>
    Task<bool> TryRecordAsync(Guid eventId, CancellationToken cancellationToken = default);
}

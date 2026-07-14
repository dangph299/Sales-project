namespace Inventory.Application;

/// <summary>
/// Application port for idempotent integration-event processing.
/// </summary>
public interface IInventoryInbox
{
    /// <summary>
    /// Records an incoming event id.
    /// </summary>
    /// <returns><see langword="true"/> when this event has not been processed before; otherwise <see langword="false"/>.</returns>
    Task<bool> TryRecordAsync(Guid eventId, CancellationToken cancellationToken = default);
}

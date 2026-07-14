using Inventory.Application;

namespace Inventory.Infrastructure;

/// <summary>
/// Adapter from Inventory application use cases to service metrics.
/// </summary>
public sealed class InventoryMetricsAdapter : IInventoryMetrics
{
    /// <inheritdoc/>
    public void RecordInboxDuplicate() => InventoryMetrics.InboxDuplicate.Add(1);

    /// <inheritdoc/>
    public void RecordInboxProcessed() => InventoryMetrics.InboxProcessed.Add(1);

    /// <inheritdoc/>
    public void RecordReservationRejected() => InventoryMetrics.ReservationRejected.Add(1);

    /// <inheritdoc/>
    public void RecordReservationReserved() => InventoryMetrics.ReservationReserved.Add(1);
}

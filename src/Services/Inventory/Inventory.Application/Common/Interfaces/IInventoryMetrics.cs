namespace Inventory.Application.Common.Interfaces;

/// <summary>
/// Application port for Inventory use-case metrics.
/// </summary>
public interface IInventoryMetrics
{
    /// <summary>Records a duplicate inbox event.</summary>
    void RecordInboxDuplicate();

    /// <summary>Records a processed inbox event.</summary>
    void RecordInboxProcessed();

    /// <summary>Records a rejected reservation.</summary>
    void RecordReservationRejected();

    /// <summary>Records a successful reservation.</summary>
    void RecordReservationReserved();
}

namespace Inventory.Application.Common.Interfaces;

/// <summary>
/// Inventory-specific port for opening transactions around event-processing use cases.
/// </summary>
public interface IInventoryTransactionManager
{
    /// <summary>
    /// Opens a serializable transaction.
    /// </summary>
    Task<IInventoryTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken = default);
}

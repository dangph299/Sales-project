namespace Inventory.Application;

/// <summary>
/// Database transaction opened for an Inventory use case.
/// </summary>
public interface IInventoryTransaction : IAsyncDisposable
{
    /// <summary>
    /// Commits the transaction.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls the transaction back.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

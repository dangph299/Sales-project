namespace BuildingBlocks.Application;

/// <summary>
/// Commits all changes made within the current unit of work.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The number of state entries written to the underlying store.
    /// </returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

namespace BuildingBlocks.Application;

/// <summary>
/// Commits all changes made within the current unit of work.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes.
    /// </summary>
    /// <returns>Number of state entries written to the underlying store.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

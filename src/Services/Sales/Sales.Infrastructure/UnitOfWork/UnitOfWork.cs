using Sales.Application.Common.Interfaces;

namespace Sales.Infrastructure;

/// <summary>
/// Thin <see cref="ISalesUnitOfWork"/> wrapper delegating to <see cref="SalesDbContext.SaveChangesAsync"/>,
/// so <see cref="Sales.Application"/> depends only on the narrow unit-of-work port
/// instead of the full <c>DbContext</c> surface.
/// </summary>
public sealed class UnitOfWork(SalesDbContext db) : ISalesUnitOfWork
{
    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);

    /// <inheritdoc/>
    public void DiscardPendingChanges()
    {
        db.ChangeTracker.Clear();
    }
}

namespace Sales.Infrastructure;

/// <summary>
/// Thin <see cref="IUnitOfWork"/> wrapper delegating to <see cref="SalesDbContext.SaveChangesAsync"/>,
/// so <see cref="Sales.Application"/> depends only on the narrow <see cref="IUnitOfWork"/> port
/// instead of the full <c>DbContext</c> surface.
/// </summary>
public sealed class UnitOfWork(SalesDbContext db) : IUnitOfWork
{
    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}

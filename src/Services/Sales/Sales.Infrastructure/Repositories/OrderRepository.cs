using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// EF Core implementation of <see cref="IOrderRepository"/>, adding the eager-loaded lookup on top
/// of the generic CRUD from <see cref="Repository{T}"/>.
/// </summary>
public sealed class OrderRepository(SalesDbContext db) : Repository<Order>(db), IOrderRepository
{
    /// <inheritdoc/>
    public Task<Order?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        Db.Orders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}

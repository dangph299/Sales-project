using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Order persistence adapter with line-aware loading.
/// </summary>
public sealed class OrderRepository(SalesDbContext db) : Repository<Order>(db), IOrderRepository
{
    /// <inheritdoc/>
    public Task<Order?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        Db.Orders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}

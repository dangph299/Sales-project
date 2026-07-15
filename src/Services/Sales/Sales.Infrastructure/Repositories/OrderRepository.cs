using Microsoft.EntityFrameworkCore;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Order persistence adapter with line-aware loading.
/// </summary>
public sealed class OrderRepository(SalesDbContext db) : Repository<Order>(db), IOrderRepository
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Guid>> FindExpiredCancellableOrderIdsAsync(
        DateTimeOffset orderUpdatedBefore,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var cancellableStatuses = new[]
        {
            OrderStatus.Draft,
            OrderStatus.PendingInventory,
            OrderStatus.InventoryRejected
        };

        return await Db.Orders
            .AsNoTracking()
            .Where(x => cancellableStatuses.Contains(x.Status) && x.UpdatedAt <= orderUpdatedBefore)
            .OrderBy(x => x.UpdatedAt)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<Order?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) =>
        Db.Orders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}

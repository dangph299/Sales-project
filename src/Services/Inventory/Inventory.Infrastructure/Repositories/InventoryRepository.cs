using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core repository for inventory item aggregates.
/// </summary>
public sealed class InventoryRepository(InventoryDbContext db) : IInventoryRepository
{
    /// <inheritdoc/>
    public Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return db.Items.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<InventoryItem>> GetByProductIdsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var ids = productIds.Distinct().ToArray();
        return await db.Items.Where(x => ids.Contains(x.ProductId)).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Add(InventoryItem item)
    {
        db.Items.Add(item);
    }
}

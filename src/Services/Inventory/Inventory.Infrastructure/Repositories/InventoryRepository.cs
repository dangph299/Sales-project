using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core repository for inventory item aggregates.
/// </summary>
public sealed class InventoryRepository(InventoryDbContext db) : IInventoryRepository
{
    /// <inheritdoc/>
    public Task<InventoryItem?> GetByProductVariantIdAsync(Guid productVariantId, CancellationToken cancellationToken = default)
    {
        return db.Items.SingleOrDefaultAsync(x => x.ProductVariantId == productVariantId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<InventoryItem>> GetByProductVariantIdsAsync(
        IEnumerable<Guid> productVariantIds,
        CancellationToken cancellationToken = default)
    {
        var ids = productVariantIds.Distinct().ToArray();
        return await db.Items.Where(x => ids.Contains(x.ProductVariantId)).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Add(InventoryItem item)
    {
        db.Items.Add(item);
    }
}

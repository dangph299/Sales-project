using System.Data;
using BuildingBlocks.Application;
using Inventory.Application;
using Microsoft.EntityFrameworkCore.Storage;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core unit of work for Inventory use cases.
/// </summary>
public sealed class InventoryUnitOfWork(InventoryDbContext db) : IUnitOfWork
{
    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}

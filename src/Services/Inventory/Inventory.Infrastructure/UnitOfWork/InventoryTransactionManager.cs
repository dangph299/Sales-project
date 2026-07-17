using System.Data;
using Inventory.Application;
using Inventory.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Inventory.Infrastructure;

/// <summary>
/// EF Core transaction manager for Inventory event-processing use cases.
/// </summary>
public sealed class InventoryTransactionManager(InventoryDbContext db) : IInventoryTransactionManager
{
    /// <inheritdoc/>
    public async Task<IInventoryTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        return new EfInventoryTransaction(transaction);
    }

    private sealed class EfInventoryTransaction(IDbContextTransaction transaction) : IInventoryTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return transaction.CommitAsync(cancellationToken);
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return transaction.RollbackAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return transaction.DisposeAsync();
        }
    }
}

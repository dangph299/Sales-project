using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sales.Application.Common.Interfaces;

namespace Sales.Infrastructure;

/// <summary>
/// EF Core transaction manager for the Sales use cases that write more than one aggregate.
/// </summary>
public sealed class SalesTransactionManager(SalesDbContext db) : ISalesTransactionManager
{
    /// <inheritdoc/>
    public async Task<ISalesTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        return new EfSalesTransaction(transaction);
    }

    private sealed class EfSalesTransaction(IDbContextTransaction transaction) : ISalesTransaction
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

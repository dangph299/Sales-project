namespace Sales.Application.Common.Interfaces;

/// <summary>
/// Opens database transactions for the Sales use cases that write more than one aggregate and must
/// not leave one of them behind.
/// </summary>
public interface ISalesTransactionManager
{
    /// <summary>
    /// Begins a transaction at the provider's default isolation level.
    /// </summary>
    Task<ISalesTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// A database transaction owned by the caller that opened it.
/// </summary>
public interface ISalesTransaction : IAsyncDisposable
{
    /// <summary>
    /// Commits every change written inside this transaction.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards every change written inside this transaction.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

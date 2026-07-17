using Inventory.Application.Common.Interfaces;
using MediatR;

namespace Inventory.Application.Common.Behaviors;

/// <summary>
/// Wraps every Inventory command in a serializable transaction, deduplicating Kafka-sourced
/// commands through the inbox before invoking the handler, and committing only after the handler
/// and inbox bookkeeping both succeed. Any failure — including a persistence-layer concurrency or
/// uniqueness conflict — rolls the transaction back before the exception is rethrown; translating
/// that exception into an HTTP response is the API layer's responsibility, so this behavior stays free of
/// EF Core/Npgsql-specific types per the Application → Infrastructure dependency rule.
/// </summary>
public sealed class InventoryTransactionBehavior<TRequest, TResponse>(
    IInventoryTransactionManager transactions,
    IInventoryInbox inbox,
    IInventoryMetrics metrics,
    IUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    /// <inheritdoc/>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Existence pre-check before opening a serializable transaction. Every first delivery pays one
        // extra lightweight query here; in return, a duplicate or redelivered event whose inbox record
        // is already committed returns early and skips the transaction, insert, and rollback below.
        // This mainly helps when Kafka retries/redeliveries are common. It can race with a concurrent
        // writer, so the transactional TryRecordAsync insert below stays the authoritative barrier.
        if (request is IIdempotentCommand<TResponse> preChecked
            && await inbox.HasBeenProcessedAsync(preChecked.EventId, cancellationToken))
        {
            metrics.RecordInboxDuplicate();
            return preChecked.DuplicateResponse;
        }

        await using var transaction = await transactions.BeginSerializableTransactionAsync(cancellationToken);
        try
        {
            if (request is IIdempotentCommand<TResponse> idempotent
                && !await inbox.TryRecordAsync(idempotent.EventId, cancellationToken))
            {
                metrics.RecordInboxDuplicate();
                await transaction.RollbackAsync(cancellationToken);
                return idempotent.DuplicateResponse;
            }

            var response = await next(cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (request is IIdempotentCommand<TResponse>) metrics.RecordInboxProcessed();
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

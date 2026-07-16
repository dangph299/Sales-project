using BuildingBlocks.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// EF Core implementation shared by service-specific inbox stores.
/// </summary>
public abstract class EfInboxFailureRecorder<TDbContext>(
    TDbContext db,
    IOptions<InboxConsumerOptions> options) : IInboxFailureRecorder
    where TDbContext : DbContext
{
    private const int MaxErrorLength = 2000;

    /// <summary>Gets the service inbox set.</summary>
    protected abstract DbSet<InboxMessage> Inbox { get; }

    /// <summary>Gets the stable consumer id written to inbox rows.</summary>
    protected abstract string Consumer { get; }

    /// <inheritdoc/>
    public async Task<InboundFailureResult> RecordFailureAsync(
        EventEnvelope envelope,
        InboundMessageContext context,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        var maxAttempts = Math.Max(1, options.Value.MaxAttempts);
        var now = DateTimeOffset.UtcNow;
        var row = await Inbox.SingleOrDefaultAsync(x => x.EventId == envelope.EventId, cancellationToken);

        if (row is null)
        {
            row = new InboxMessage
            {
                EventId = envelope.EventId,
                ProcessedAt = now,
                Consumer = Consumer
            };
            Inbox.Add(row);
        }

        row.Attempts++;
        row.LastFailedAt = now;
        row.LastExceptionType = exception.GetType().FullName;
        row.LastError = exception.Message[..Math.Min(MaxErrorLength, exception.Message.Length)];
        row.OriginalTopic = context.Topic;
        row.OriginalConsumerGroup = context.GroupId;
        row.OriginalPartition = context.Partition;
        row.OriginalOffset = context.Offset;

        if (row.Attempts >= maxAttempts)
        {
            row.Status = InboxMessageStatus.DeadLettered;
            row.DeadLetteredAt = now;
        }
        else
        {
            row.Status = InboxMessageStatus.Failed;
            row.DeadLetteredAt = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new InboundFailureResult(row.Attempts, row.Status == InboxMessageStatus.DeadLettered);
    }
}

using BuildingBlocks.Contracts;

namespace AuditLog.Infrastructure;

/// <summary>
/// Port for persisting consumed events to the audit store, abstracting the MongoDB driver away
/// from <see cref="AuditEventHandler"/>.
/// </summary>
public interface IAuditWriter
{
    /// <summary>
    /// Upserts a consumed event, keyed by its event id so redeliveries do not create duplicates.
    /// </summary>
    /// <param name="envelope">Event envelope to store.</param>
    /// <param name="topic">Kafka topic the event was consumed from.</param>
    /// <param name="partition">Kafka partition the event was consumed from.</param>
    /// <param name="offset">Kafka offset the event was consumed from.</param>
    Task UpsertAsync(EventEnvelope envelope, string topic, int partition, long offset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the audit store's indexes exist, safe to call repeatedly.
    /// </summary>
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}

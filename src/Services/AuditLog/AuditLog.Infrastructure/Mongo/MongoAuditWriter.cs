using BuildingBlocks.Contracts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AuditLog.Infrastructure;

/// <summary>
/// Audit writer that stores consumed events for later inspection.
/// </summary>
public sealed class MongoAuditWriter(IMongoDatabase database) : IAuditWriter
{
    private const string CollectionName = "events";

    /// <inheritdoc/>
    public Task UpsertAsync(EventEnvelope envelope, string topic, int partition, long offset, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<AuditDocument>(CollectionName);
        var update = Builders<AuditDocument>.Update
            .SetOnInsert(x => x.Id, ObjectId.GenerateNewId())
            .Set(x => x.EventId, envelope.EventId)
            .Set(x => x.EventType, envelope.EventType)
            .Set(x => x.AggregateId, envelope.AggregateId)
            .Set(x => x.Version, envelope.Version)
            .Set(x => x.CorrelationId, envelope.CorrelationId)
            .Set(x => x.CausationId, envelope.CausationId)
            .Set(x => x.OccurredAt, envelope.OccurredAt)
            .Set(x => x.Actor, envelope.Actor)
            .Set(x => x.Payload, envelope.Data.GetRawText())
            .Set(x => x.Topic, topic)
            .Set(x => x.Partition, partition)
            .Set(x => x.Offset, offset);
        return collection.UpdateOneAsync(x => x.EventId == envelope.EventId, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<AuditDocument>(CollectionName);
        return collection.Indexes.CreateOneAsync(new CreateIndexModel<AuditDocument>(
            Builders<AuditDocument>.IndexKeys.Ascending(x => x.EventId),
            new CreateIndexOptions { Unique = true, Name = "ux_events_event_id" }), cancellationToken: cancellationToken);
    }
}

using BuildingBlocks.Contracts;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

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
        var auditEvent = envelope.Data.Deserialize<AuditLogEvent>() ??
            throw new InvalidOperationException("AuditLogEvent payload is required.");
        if (auditEvent.SchemaVersion != 1)
        {
            throw new NotSupportedException($"Unsupported audit schema version {auditEvent.SchemaVersion}.");
        }

        var collection = database.GetCollection<AuditDocument>(CollectionName);
        var update = Builders<AuditDocument>.Update
            .SetOnInsert(x => x.Id, ObjectId.GenerateNewId())
            .Set(x => x.AuditId, auditEvent.AuditId)
            .Set(x => x.EventId, envelope.EventId)
            .Set(x => x.ServiceName, auditEvent.ServiceName)
            .Set(x => x.EventType, auditEvent.EventType)
            .Set(x => x.EntityType, auditEvent.EntityType)
            .Set(x => x.EntityId, auditEvent.EntityId)
            .Set(x => x.Action, auditEvent.Action)
            .Set(x => x.Description, auditEvent.Description)
            .Set(x => x.AggregateId, envelope.AggregateId)
            .Set(x => x.Version, envelope.Version)
            .Set(x => x.CorrelationId, auditEvent.CorrelationId ?? envelope.CorrelationId.ToString())
            .Set(x => x.CausationId, auditEvent.CausationId ?? envelope.CausationId?.ToString())
            .Set(x => x.TraceId, auditEvent.TraceId)
            .Set(x => x.OccurredAt, auditEvent.OccurredAt)
            .Set(x => x.ActorId, auditEvent.ActorId)
            .Set(x => x.ActorName, auditEvent.ActorName)
            .Set(x => x.Actor, envelope.Actor)
            .Set(x => x.SchemaVersion, auditEvent.SchemaVersion)
            .Set(x => x.Changes, auditEvent.Changes.Select(change => new AuditChangeDocument
            {
                PropertyPath = change.PropertyPath,
                OldValue = NormalizeAuditValue(change.OldValue),
                NewValue = NormalizeAuditValue(change.NewValue)
            }).ToArray())
            .Set(x => x.Metadata, auditEvent.Metadata.ToDictionary(x => x.Key, x => NormalizeAuditValue(x.Value)))
            .Set(x => x.Payload, envelope.Data.GetRawText())
            .Set(x => x.Topic, topic)
            .Set(x => x.Partition, partition)
            .Set(x => x.Offset, offset)
            .Set(x => x.ReceivedAt, DateTimeOffset.UtcNow);
        return collection.UpdateOneAsync(x => x.AuditId == auditEvent.AuditId, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<AuditDocument>(CollectionName);
        var indexes = new[]
        {
            new CreateIndexModel<AuditDocument>(
                Builders<AuditDocument>.IndexKeys.Ascending(x => x.AuditId),
                new CreateIndexOptions { Unique = true, Name = "ux_events_audit_id" }),
            new CreateIndexModel<AuditDocument>(
                Builders<AuditDocument>.IndexKeys.Ascending(x => x.EntityType).Ascending(x => x.EntityId).Descending(x => x.OccurredAt),
                new CreateIndexOptions { Name = "ix_events_entity_time" }),
            new CreateIndexModel<AuditDocument>(
                Builders<AuditDocument>.IndexKeys.Ascending(x => x.ServiceName).Descending(x => x.OccurredAt),
                new CreateIndexOptions { Name = "ix_events_service_time" }),
            new CreateIndexModel<AuditDocument>(
                Builders<AuditDocument>.IndexKeys.Ascending(x => x.CorrelationId),
                new CreateIndexOptions { Name = "ix_events_correlation_id" })
        };
        return collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private static object? NormalizeAuditValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when jsonElement.TryGetInt64(out var longValue) => longValue,
                JsonValueKind.Number when jsonElement.TryGetDecimal(out var decimalValue) => decimalValue,
                JsonValueKind.Array => jsonElement.EnumerateArray().Select(element => NormalizeAuditValue(element)).ToArray(),
                JsonValueKind.Object => jsonElement.EnumerateObject()
                    .ToDictionary(property => property.Name, property => NormalizeAuditValue(property.Value)),
                _ => jsonElement.ToString()
            };
        }

        return value;
    }
}

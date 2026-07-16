using AuditLog.Infrastructure;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using MongoDB.Driver;

namespace AuditLog.Tests;

[Trait("Category", "Reliability")]
[Collection("AuditReliabilityMongo")]
public sealed class MongoReliabilityTests
{
    private readonly MongoReliabilityFixture _fixture;

    public MongoReliabilityTests(MongoReliabilityFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Mongo_unique_audit_id_deduplicates_audit_documents()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var client = new MongoClient(_fixture.ConnectionString);
        var database = client.GetDatabase("audit_reliability_tests");
        var collection = database.GetCollection<AuditDocument>("events");
        await collection.DeleteManyAsync(_ => true);
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<AuditDocument>(
            Builders<AuditDocument>.IndexKeys.Ascending(x => x.AuditId), new CreateIndexOptions { Unique = true }));

        var auditId = Guid.NewGuid();
        await collection.InsertOneAsync(Document(auditId, "first"));

        await Assert.ThrowsAsync<MongoWriteException>(() => collection.InsertOneAsync(Document(auditId, "duplicate")));
        Assert.Equal(1, await collection.CountDocumentsAsync(x => x.AuditId == auditId));
    }

    [SkippableFact]
    public async Task Mongo_writer_stores_audit_log_event_schema()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var client = new MongoClient(_fixture.ConnectionString);
        var database = client.GetDatabase("audit_reliability_tests");
        var collection = database.GetCollection<AuditDocument>("events");
        await collection.DeleteManyAsync(_ => true);
        var writer = new MongoAuditWriter(database);
        await writer.EnsureIndexesAsync();
        var auditId = Guid.NewGuid();
        var auditEvent = new AuditLogEvent
        {
            AuditId = auditId,
            ServiceName = "Sales",
            EventType = "ProductUpdated",
            EntityType = "Product",
            EntityId = Guid.NewGuid().ToString(),
            Action = AuditActions.Updated,
            ActorId = "tester",
            ActorName = "tester",
            CorrelationId = Guid.NewGuid().ToString(),
            TraceId = "trace",
            OccurredAt = DateTimeOffset.UtcNow,
            Changes = [new AuditChange { PropertyPath = "Name", OldValue = "Old", NewValue = "New" }]
        };
        var envelope = EventEnvelopeFactory.Create(Guid.NewGuid(), 1, auditEvent, "tester");

        await writer.UpsertAsync(envelope, KafkaTopics.SalesAudit, partition: 0, offset: 10);
        await writer.UpsertAsync(envelope, KafkaTopics.SalesAudit, partition: 0, offset: 11);

        var document = await collection.Find(x => x.AuditId == auditId).SingleAsync();
        Assert.Equal("Sales", document.ServiceName);
        Assert.Equal("Product", document.EntityType);
        Assert.Equal("ProductUpdated", document.EventType);
        Assert.Equal("tester", document.ActorId);
        Assert.Single(document.Changes);
        Assert.Equal(1, await collection.CountDocumentsAsync(x => x.AuditId == auditId));
    }

    [SkippableFact]
    public async Task Mongo_writer_rejects_unsupported_schema_version()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var client = new MongoClient(_fixture.ConnectionString);
        var database = client.GetDatabase("audit_reliability_tests");
        var writer = new MongoAuditWriter(database);
        var auditEvent = new AuditLogEvent
        {
            AuditId = Guid.NewGuid(),
            ServiceName = "Sales",
            EventType = "ProductUpdated",
            EntityType = "Product",
            EntityId = Guid.NewGuid().ToString(),
            Action = AuditActions.Updated,
            OccurredAt = DateTimeOffset.UtcNow,
            SchemaVersion = 999
        };
        var envelope = EventEnvelopeFactory.Create(Guid.NewGuid(), 1, auditEvent, "tester");

        await Assert.ThrowsAsync<NotSupportedException>(() => writer.UpsertAsync(envelope, KafkaTopics.SalesAudit, 0, 1));
    }

    private static AuditDocument Document(Guid auditId, string payload)
    {
        return new AuditDocument
        {
            AuditId = auditId,
            EventId = Guid.NewGuid(),
            ServiceName = "Sales",
            EventType = "ProductCreated",
            EntityType = "Product",
            EntityId = Guid.NewGuid().ToString(),
            Action = "Created",
            AggregateId = Guid.NewGuid(),
            Version = 1,
            CorrelationId = Guid.NewGuid().ToString(),
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = "integration-test",
            SchemaVersion = 1,
            Payload = payload,
            Topic = "sales.audit.v1"
        };
    }
}

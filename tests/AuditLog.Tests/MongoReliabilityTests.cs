using AuditLog.Infrastructure;
using MongoDB.Driver;

namespace AuditLog.Tests;

[Trait("Category", "Reliability")]
public sealed class MongoReliabilityTests
{
    [Fact]
    public async Task Mongo_unique_event_id_deduplicates_audit_documents()
    {
        if (!ReliabilityTestSettings.Enabled) return;

        var client = new MongoClient(ReliabilityTestSettings.MongoConnectionString);
        var database = client.GetDatabase(ReliabilityTestSettings.MongoDatabase);
        var collection = database.GetCollection<AuditDocument>("events");
        await collection.DeleteManyAsync(_ => true);
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<AuditDocument>(
            Builders<AuditDocument>.IndexKeys.Ascending(x => x.EventId), new CreateIndexOptions { Unique = true }));

        var eventId = Guid.NewGuid();
        await collection.InsertOneAsync(Document(eventId, "first"));

        await Assert.ThrowsAsync<MongoWriteException>(() => collection.InsertOneAsync(Document(eventId, "duplicate")));
        Assert.Equal(1, await collection.CountDocumentsAsync(x => x.EventId == eventId));
    }

    private static AuditDocument Document(Guid eventId, string payload)
    {
        return new AuditDocument
        {
            EventId = eventId,
            EventType = "AuditChanged",
            AggregateId = Guid.NewGuid(),
            Version = 1,
            CorrelationId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            Actor = "integration-test",
            Payload = payload,
            Topic = "sales.audit.v1"
        };
    }
}

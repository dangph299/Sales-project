using AuditLog.Infrastructure;

namespace AuditLog.Tests;

public sealed class AuditDocumentTests
{
    [Fact]
    public void Event_id_is_the_idempotency_key()
    {
        var eventId = Guid.NewGuid();
        var document = new AuditDocument { EventId = eventId, EventType = "ProductCreated", Actor = "tester", Payload = "{}", Topic = "sales.audit.v1" };
        Assert.Equal(eventId, document.EventId);
    }

    [Fact]
    public void New_documents_get_distinct_mongo_ids()
    {
        var first = new AuditDocument();
        var second = new AuditDocument();

        Assert.NotEqual(first.Id, second.Id);
    }
}

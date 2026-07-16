using AuditLog.Infrastructure;

namespace AuditLog.Tests;

public sealed class AuditDocumentTests
{
    [Fact]
    public void Audit_id_is_the_idempotency_key()
    {
        var auditId = Guid.NewGuid();
        var document = new AuditDocument { AuditId = auditId, EventType = "ProductCreated", Actor = "tester", Payload = "{}", Topic = "sales.audit.v1" };
        Assert.Equal(auditId, document.AuditId);
    }

    [Fact]
    public void New_documents_get_distinct_mongo_ids()
    {
        var first = new AuditDocument();
        var second = new AuditDocument();

        Assert.NotEqual(first.Id, second.Id);
    }
}

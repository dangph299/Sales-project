using BuildingBlocks.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Adds generated audit events to the service outbox before EF commits business changes.
/// </summary>
public sealed class AuditSaveChangesInterceptor(
    IAuditEntryFactory auditEntryFactory,
    IOptions<AuditOptions> options,
    IOutboxSignal outboxSignal) : SaveChangesInterceptor
{
    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        EnqueueAuditEvents(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EnqueueAuditEvents(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc/>
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        outboxSignal.Notify();
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        outboxSignal.Notify();
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void EnqueueAuditEvents(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var auditOptions = options.Value;
        var auditEvents = auditEntryFactory.CreateAuditEvents(dbContext, auditOptions.ServiceName);
        foreach (var auditEvent in auditEvents)
        {
            var aggregateId = Guid.TryParse(auditEvent.EntityId, out var parsedEntityId)
                ? parsedEntityId
                : auditEvent.AuditId;
            var envelope = EventEnvelopeFactory.Create(
                aggregateId,
                version: 0,
                auditEvent,
                actor: auditEvent.ActorId ?? "system",
                correlationId: Guid.TryParse(auditEvent.CorrelationId, out var correlationId) ? correlationId : null,
                causationId: Guid.TryParse(auditEvent.CausationId, out var causationId) ? causationId : null);
            dbContext.Set<OutboxMessage>().Add(OutboxMessage.From(envelope, auditOptions.TopicName));
        }
    }
}

using System.Collections;
using BuildingBlocks.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Creates aggregate-level audit events from EF Core pending scalar property changes.
/// </summary>
public sealed class EfCoreAuditEntryFactory(
    IOptions<AuditOptions> options,
    IAuditContextAccessor contextAccessor,
    IAuditAggregateResolver aggregateResolver,
    IEnumerable<IAuditEnricher> enrichers) : IAuditEntryFactory
{
    /// <inheritdoc/>
    public IReadOnlyCollection<AuditLogEvent> CreateAuditEvents(DbContext dbContext, string serviceName)
    {
        var auditOptions = options.Value;
        var candidateEntries = dbContext.ChangeTracker.Entries()
            .Where(entityEntry => entityEntry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(entityEntry => !auditOptions.IsEntityIgnored(entityEntry.Metadata.ClrType))
            .Where(entityEntry => !IsInfrastructureEntity(entityEntry))
            .ToArray();

        var groupedChanges = new Dictionary<AuditAggregateIdentity, List<AuditChange>>();
        var groupedEntries = new Dictionary<AuditAggregateIdentity, List<EntityEntry>>();
        var groupedActions = new Dictionary<AuditAggregateIdentity, HashSet<string>>();

        foreach (var entityEntry in candidateEntries)
        {
            var aggregate = aggregateResolver.Resolve(entityEntry);
            var groupAggregate = aggregate with { PropertyPrefix = string.Empty };
            var changes = CreateChanges(entityEntry, aggregate.PropertyPrefix, auditOptions);
            if (changes.Count == 0)
            {
                continue;
            }

            if (!groupedChanges.TryGetValue(groupAggregate, out var aggregateChanges))
            {
                aggregateChanges = [];
                groupedChanges[groupAggregate] = aggregateChanges;
                groupedEntries[groupAggregate] = [];
                groupedActions[groupAggregate] = [];
            }

            aggregateChanges.AddRange(changes);
            groupedEntries[groupAggregate].Add(entityEntry);
            groupedActions[groupAggregate].Add(GetAction(entityEntry));
        }

        var auditEvents = new List<AuditLogEvent>();
        foreach (var (aggregate, changes) in groupedChanges)
        {
            var limitedChanges = changes.Take(auditOptions.MaximumChangesPerEvent).ToArray();
            var action = ResolveAction(groupedActions[aggregate]);
            var auditEvent = new AuditLogEvent
            {
                AuditId = Guid.NewGuid(),
                ServiceName = serviceName,
                EventType = $"{aggregate.EntityType}{action}",
                EntityType = aggregate.EntityType,
                EntityId = aggregate.EntityId,
                Action = action,
                ActorId = contextAccessor.ActorId,
                ActorName = contextAccessor.ActorName,
                CorrelationId = contextAccessor.CorrelationId,
                CausationId = contextAccessor.CausationId,
                TraceId = contextAccessor.TraceId,
                OccurredAt = DateTimeOffset.UtcNow,
                Changes = limitedChanges,
                Metadata = changes.Count > limitedChanges.Length
                    ? new Dictionary<string, object?> { ["truncatedChanges"] = changes.Count - limitedChanges.Length }
                    : new Dictionary<string, object?>()
            };

            var context = new AuditEnrichmentContext(
                new AuditEventData(auditEvent.Metadata, limitedChanges),
                groupedEntries[aggregate],
                aggregate,
                serviceName);
            foreach (var enricher in enrichers)
            {
                if (enricher.CanEnrich(context))
                {
                    auditEvent = enricher.Enrich(auditEvent, context);
                }
            }

            auditEvents.Add(auditEvent);
        }

        return auditEvents;
    }

    private static bool IsInfrastructureEntity(EntityEntry entityEntry)
    {
        var clrType = entityEntry.Metadata.ClrType;
        return clrType == typeof(OutboxMessage)
            || clrType == typeof(InboxMessage)
            || clrType.Name.Contains("Audit", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyCollection<AuditChange> CreateChanges(
        EntityEntry entityEntry,
        string propertyPrefix,
        AuditOptions auditOptions)
    {
        var changes = new List<AuditChange>();
        foreach (var propertyEntry in entityEntry.Properties)
        {
            var property = propertyEntry.Metadata;
            if (property.IsShadowProperty() || property.IsConcurrencyToken || property.IsPrimaryKey())
            {
                continue;
            }

            var propertyName = property.Name;
            if (auditOptions.IsPropertyIgnored(entityEntry.Metadata.ClrType, propertyName))
            {
                continue;
            }

            var oldValue = entityEntry.State == EntityState.Added ? null : propertyEntry.OriginalValue;
            var newValue = entityEntry.State == EntityState.Deleted ? null : propertyEntry.CurrentValue;
            if (entityEntry.State == EntityState.Added && newValue is null)
            {
                continue;
            }

            if (entityEntry.State == EntityState.Modified)
            {
                if (!propertyEntry.IsModified || ValuesEqual(oldValue, newValue))
                {
                    continue;
                }
            }

            changes.Add(new AuditChange
            {
                PropertyPath = BuildPropertyPath(propertyPrefix, propertyName),
                OldValue = NormalizeValue(oldValue, entityEntry.Metadata.ClrType, propertyName, auditOptions),
                NewValue = NormalizeValue(newValue, entityEntry.Metadata.ClrType, propertyName, auditOptions)
            });
        }

        return changes;
    }

    private static string BuildPropertyPath(string propertyPrefix, string propertyName)
    {
        return string.IsNullOrWhiteSpace(propertyPrefix) ? propertyName : $"{propertyPrefix}.{propertyName}";
    }

    private static object? NormalizeValue(object? value, Type entityType, string propertyName, AuditOptions auditOptions)
    {
        if (value is null)
        {
            return null;
        }

        if (auditOptions.IsPropertyMasked(entityType, propertyName))
        {
            return "***";
        }

        if (value is byte[] or IEnumerable<byte>)
        {
            return "[binary]";
        }

        if (value is string text)
        {
            return text.Length <= auditOptions.MaximumStringLength
                ? text
                : string.Concat(text.AsSpan(0, auditOptions.MaximumStringLength), "...[truncated]");
        }

        if (value is IEnumerable and not string)
        {
            return value.ToString();
        }

        return value;
    }

    private static bool ValuesEqual(object? oldValue, object? newValue)
    {
        return Equals(oldValue, newValue);
    }

    private static string GetAction(EntityEntry entityEntry)
    {
        if (entityEntry.State == EntityState.Modified && IsSoftDelete(entityEntry))
        {
            return AuditActions.Deleted;
        }

        return entityEntry.State switch
        {
            EntityState.Added => AuditActions.Created,
            EntityState.Deleted => AuditActions.Deleted,
            _ => AuditActions.Updated
        };
    }

    private static bool IsSoftDelete(EntityEntry entityEntry)
    {
        var propertyEntry = entityEntry.Properties.SingleOrDefault(property =>
            property.Metadata.Name.Equals("IsDelete", StringComparison.OrdinalIgnoreCase)
            || property.Metadata.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase));
        return propertyEntry is not null
            && propertyEntry.IsModified
            && propertyEntry.OriginalValue is false
            && propertyEntry.CurrentValue is true;
    }

    private static string ResolveAction(HashSet<string> actions)
    {
        if (actions.Count == 1)
        {
            return actions.Single();
        }

        if (actions.Contains(AuditActions.Deleted))
        {
            return AuditActions.Deleted;
        }

        if (actions.Contains(AuditActions.Created))
        {
            return AuditActions.Created;
        }

        return AuditActions.Updated;
    }
}

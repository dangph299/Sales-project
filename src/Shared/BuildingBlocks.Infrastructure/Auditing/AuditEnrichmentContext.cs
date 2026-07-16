using BuildingBlocks.Contracts;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Context available to audit enrichers when they add business meaning to a generic data diff.
/// </summary>
public sealed record AuditEnrichmentContext(
    AuditEventData EventData,
    IReadOnlyCollection<EntityEntry> Entries,
    AuditAggregateIdentity Aggregate,
    string ServiceName);

/// <summary>
/// Minimal EF event data needed by audit enrichers.
/// </summary>
public sealed record AuditEventData(
    IReadOnlyDictionary<string, object?> Metadata,
    IReadOnlyCollection<AuditChange> Changes);

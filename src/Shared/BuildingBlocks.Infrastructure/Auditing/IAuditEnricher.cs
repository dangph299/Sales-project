using BuildingBlocks.Contracts;

namespace BuildingBlocks.Infrastructure;

/// <summary>
/// Adds business meaning to automatically generated audit events when a field diff is not enough.
/// </summary>
public interface IAuditEnricher
{
    /// <summary>
    /// Determines whether this enricher applies to the audit event.
    /// </summary>
    /// <param name="context">Audit enrichment context.</param>
    /// <returns><see langword="true"/> when the enricher can apply.</returns>
    bool CanEnrich(AuditEnrichmentContext context);

    /// <summary>
    /// Enriches the audit event with business description or metadata.
    /// </summary>
    /// <param name="auditEvent">Audit event to enrich.</param>
    /// <param name="context">Audit enrichment context.</param>
    /// <returns>Enriched audit event.</returns>
    AuditLogEvent Enrich(AuditLogEvent auditEvent, AuditEnrichmentContext context);
}

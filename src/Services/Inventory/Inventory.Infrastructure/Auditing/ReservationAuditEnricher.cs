using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Inventory.Domain;

namespace Inventory.Infrastructure;

/// <summary>
/// Adds business descriptions to generic reservation status audit events.
/// </summary>
public sealed class ReservationAuditEnricher : IAuditEnricher
{
    /// <inheritdoc/>
    public bool CanEnrich(AuditEnrichmentContext context)
    {
        return context.Aggregate.EntityType == nameof(Reservation);
    }

    /// <inheritdoc/>
    public AuditLogEvent Enrich(AuditLogEvent auditEvent, AuditEnrichmentContext context)
    {
        var statusChange = auditEvent.Changes.SingleOrDefault(change => change.PropertyPath == nameof(Reservation.Status));
        if (statusChange?.NewValue is null)
        {
            return auditEvent;
        }

        var newStatus = statusChange.NewValue.ToString();
        var description = newStatus switch
        {
            nameof(ReservationStatus.Active) => "Inventory reservation became active.",
            nameof(ReservationStatus.Released) => "Inventory reservation was released.",
            _ => auditEvent.Description
        };

        return auditEvent with
        {
            EventType = $"Reservation{newStatus}",
            Description = description
        };
    }
}

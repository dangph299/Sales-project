using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Sales.Domain;

namespace Sales.Infrastructure;

/// <summary>
/// Adds business descriptions to generic order status audit events.
/// </summary>
public sealed class OrderAuditEnricher : IAuditEnricher
{
    /// <inheritdoc/>
    public bool CanEnrich(AuditEnrichmentContext context)
    {
        return context.Aggregate.EntityType == nameof(Order);
    }

    /// <inheritdoc/>
    public AuditLogEvent Enrich(AuditLogEvent auditEvent, AuditEnrichmentContext context)
    {
        var statusChange = auditEvent.Changes.SingleOrDefault(change => change.PropertyPath == nameof(Order.Status));
        if (statusChange?.NewValue is null)
        {
            return auditEvent;
        }

        var newStatus = statusChange.NewValue.ToString();
        var description = newStatus switch
        {
            nameof(OrderStatus.PendingInventory) => "Order confirmation was requested and is waiting for inventory.",
            nameof(OrderStatus.Confirmed) => "Order was confirmed after inventory reservation succeeded.",
            nameof(OrderStatus.InventoryRejected) => "Order confirmation was rejected by inventory.",
            nameof(OrderStatus.Cancelled) => "Order was cancelled.",
            nameof(OrderStatus.Draft) => "Order was returned to draft.",
            _ => auditEvent.Description
        };

        return auditEvent with
        {
            EventType = $"Order{newStatus}",
            Description = description
        };
    }
}

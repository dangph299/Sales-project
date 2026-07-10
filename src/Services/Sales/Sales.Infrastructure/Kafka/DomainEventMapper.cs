using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure;
using Sales.Application;
using Sales.Domain;

namespace Sales.Infrastructure;

internal static class DomainEventMapper
{
    private static readonly IReadOnlyDictionary<string, string> ProductDisplayNames = new Dictionary<string, string>
    {
        ["Sku"] = "SKU",
        ["Name"] = "Product Name",
        ["Price"] = "Price",
        ["IsActive"] = "Active"
    };

    private static readonly IReadOnlyDictionary<string, string> CustomerDisplayNames = new Dictionary<string, string>
    {
        ["Name"] = "Customer Name",
        ["Phone"] = "Phone"
    };

    /// <summary>Converts a Sales domain event into the Kafka topic + envelope ready to enqueue in the Outbox.</summary>
    public static (string Topic, EventEnvelope Envelope) Map(
        AggregateRoot<Guid> aggregate,
        BuildingBlocks.Domain.IDomainEvent domainEvent,
        IExecutionContext context)
    {
        var (topic, payload) = MapToPayload(domainEvent);
        var envelope = EventEnvelopeFactory.Create(aggregate.Id, aggregate.Version, payload, context.Actor, context.CorrelationId);
        return (topic, envelope);
    }

    /// <summary>Picks the mapping method for the concrete domain event type; throws if a new event type is added here without one.</summary>
    private static (string Topic, object Payload) MapToPayload(BuildingBlocks.Domain.IDomainEvent domainEvent) => domainEvent switch
    {
        ProductCreatedDomainEvent e => MapProductCreated(e),
        ProductUpdatedDomainEvent e => MapProductUpdated(e),
        CustomerCreatedDomainEvent e => MapCustomerCreated(e),
        CustomerUpdatedDomainEvent e => MapCustomerUpdated(e),
        OrderCreatedDomainEvent e => MapOrderCreated(e),
        OrderLinesReplacedDomainEvent e => MapOrderLinesReplaced(e),
        OrderConfirmationRequestedDomainEvent e => MapOrderConfirmationRequested(e),
        OrderUndoComfirmedDomainEvent e => MapOrderUndoConfirmed(e),
        OrderConfirmedDomainEvent e => MapOrderConfirmed(e),
        OrderInventoryRejectedDomainEvent e => MapOrderInventoryRejected(e),
        _ => throw new InvalidOperationException($"No integration mapping exists for {domainEvent.GetType().Name}.")
    };

    /// <summary>Product created -&gt; SalesAudit "Created", recording the initial Sku/Name/Price/IsActive values.</summary>
    private static (string Topic, object Payload) MapProductCreated(ProductCreatedDomainEvent e)
    {
        var newValues = new { e.Sku, e.Name, e.Price, IsActive = true };
        var changes = AuditChangeDetector.Created(newValues, ProductDisplayNames);
        var audit = new AuditChanged("Product", e.ProductId.ToString(), "Created", changes);
        return (KafkaTopics.SalesAudit, audit);
    }

    /// <summary>Product updated -&gt; SalesAudit "Updated", diffing old vs new Name/Price/IsActive via <see cref="AuditChangeDetector"/>.</summary>
    private static (string Topic, object Payload) MapProductUpdated(ProductUpdatedDomainEvent e)
    {
        var oldValues = new { Name = e.OldName, Price = e.OldPrice, IsActive = e.OldIsActive };
        var newValues = new { Name = e.NewName, Price = e.NewPrice, IsActive = e.NewIsActive };
        var changes = AuditChangeDetector.Updated(oldValues, newValues, ProductDisplayNames);
        var audit = new AuditChanged("Product", e.ProductId.ToString(), "Updated", changes);
        return (KafkaTopics.SalesAudit, audit);
    }

    /// <summary>Customer created -&gt; SalesAudit "Created", recording the initial Name/Phone values.</summary>
    private static (string Topic, object Payload) MapCustomerCreated(CustomerCreatedDomainEvent e)
    {
        var newValues = new { e.Name, e.Phone };
        var changes = AuditChangeDetector.Created(newValues, CustomerDisplayNames);
        var audit = new AuditChanged("Customer", e.CustomerId.ToString(), "Created", changes);
        return (KafkaTopics.SalesAudit, audit);
    }

    /// <summary>Customer updated -&gt; SalesAudit "Updated", diffing old vs new Name/Phone via <see cref="AuditChangeDetector"/>.</summary>
    private static (string Topic, object Payload) MapCustomerUpdated(CustomerUpdatedDomainEvent e)
    {
        var oldValues = new { Name = e.OldName, Phone = e.OldPhone };
        var newValues = new { Name = e.NewName, Phone = e.NewPhone };
        var changes = AuditChangeDetector.Updated(oldValues, newValues, CustomerDisplayNames);
        var audit = new AuditChanged("Customer", e.CustomerId.ToString(), "Updated", changes);
        return (KafkaTopics.SalesAudit, audit);
    }

    /// <summary>Order created -&gt; SalesAudit "Created", recording which customer the order was placed for.</summary>
    private static (string Topic, object Payload) MapOrderCreated(OrderCreatedDomainEvent e)
    {
        var change = AuditChangeDetector.Change("CustomerId", null, e.CustomerId, "Customer", "string");
        var audit = new AuditChanged("Order", e.OrderId.ToString(), "Created", [change]);
        return (KafkaTopics.SalesAudit, audit);
    }

    /// <summary>Order lines replaced -&gt; SalesAudit "LinesReplaced", recording the new TotalQuantity/Total after the edit.</summary>
    private static (string Topic, object Payload) MapOrderLinesReplaced(OrderLinesReplacedDomainEvent e)
    {
        var quantityChange = AuditChangeDetector.Change("TotalQuantity", null, e.TotalQuantity, "Total Quantity");
        var totalChange = AuditChangeDetector.Change("Total", null, e.Total, "Total");
        var audit = new AuditChanged("Order", e.OrderId.ToString(), "LinesReplaced", [quantityChange, totalChange]);
        return (KafkaTopics.SalesAudit, audit);
    }

    /// <summary>Order confirmed by Sales -&gt; integration event asking Inventory to reserve stock for each line (not an audit event).</summary>
    private static (string Topic, object Payload) MapOrderConfirmationRequested(OrderConfirmationRequestedDomainEvent e)
    {
        var lines = e.Lines.Select(x => new OrderLineIntegration(x.ProductId, x.Sku, x.Quantity)).ToArray();
        var integrationEvent = new OrderConfirmationRequested(e.OrderId, lines);
        return (KafkaTopics.OrderConfirmationRequested, integrationEvent);
    }

    /// <summary>Order cancelled by Sales -&gt; integration event asking Inventory to release any reserved stock (not an audit event).</summary>
    private static (string Topic, object Payload) MapOrderUndoConfirmed(OrderUndoComfirmedDomainEvent e)
    {
        var integrationEvent = new OrderCancellationRequested(e.OrderId);
        return (KafkaTopics.OrderUndoConfirmationRequested, integrationEvent);
    }

    /// <summary>Inventory confirmed the reservation -&gt; SalesAudit "Confirmed", recording the order's status transition.</summary>
    private static (string Topic, object Payload) MapOrderConfirmed(OrderConfirmedDomainEvent e)
    {
        var change = AuditChangeDetector.Change("Status", null, "Confirmed", "Status", "string");
        var audit = new AuditChanged("Order", e.OrderId.ToString(), "Confirmed", [change]);
        return (KafkaTopics.SalesAudit, audit);
    }

    /// <summary>Inventory rejected the reservation (insufficient stock) -&gt; SalesAudit "InventoryRejected", recording why.</summary>
    private static (string Topic, object Payload) MapOrderInventoryRejected(OrderInventoryRejectedDomainEvent e)
    {
        var change = AuditChangeDetector.Change("RejectionReason", null, e.Reason, "Rejection Reason", "string");
        var audit = new AuditChanged("Order", e.OrderId.ToString(), "InventoryRejected", [change]);
        return (KafkaTopics.SalesAudit, audit);
    }
}

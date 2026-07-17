using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure;
using Sales.Application.Common.Interfaces;
using Sales.Domain;

namespace Sales.Infrastructure;

internal static class DomainEventMapper
{
    /// <summary>Converts a Sales domain event into the Kafka topic + envelope ready to enqueue in the Outbox.</summary>
    public static (string Topic, EventEnvelope Envelope)? Map(
        AggregateRoot<Guid> aggregate,
        BuildingBlocks.Domain.IDomainEvent domainEvent,
        IExecutionContext context)
    {
        var mapped = MapToPayload(domainEvent);
        if (mapped is null)
        {
            return null;
        }

        var (topic, payload) = mapped.Value;
        var envelope = EventEnvelopeFactory.Create(aggregate.Id, aggregate.Version, payload, context.Actor, context.CorrelationId);
        return (topic, envelope);
    }

    private static (string Topic, object Payload)? MapToPayload(BuildingBlocks.Domain.IDomainEvent domainEvent) => domainEvent switch
    {
        OrderConfirmationRequestedDomainEvent e => MapOrderConfirmationRequested(e),
        OrderUndoComfirmedDomainEvent e => MapOrderUndoConfirmed(e),
        _ => null
    };

    private static (string Topic, object Payload) MapOrderConfirmationRequested(OrderConfirmationRequestedDomainEvent e)
    {
        var lines = e.Lines.Select(x => new OrderLineIntegration(x.ProductId, x.Sku, x.Quantity)).ToArray();
        var integrationEvent = new OrderConfirmationRequested(e.OrderId, lines);
        return (KafkaTopics.OrderConfirmationRequested, integrationEvent);
    }

    private static (string Topic, object Payload) MapOrderUndoConfirmed(OrderUndoComfirmedDomainEvent e)
    {
        var integrationEvent = new OrderCancellationRequested(e.OrderId);
        return (KafkaTopics.OrderUndoConfirmationRequested, integrationEvent);
    }

}

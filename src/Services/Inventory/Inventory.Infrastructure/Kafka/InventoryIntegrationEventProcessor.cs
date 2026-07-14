using System.Text.Json;
using BuildingBlocks.Contracts;
using BuildingBlocks.Infrastructure;
using Inventory.Application;
using MediatR;

namespace Inventory.Infrastructure;

/// <summary>
/// Kafka adapter that converts Inventory integration envelopes into MediatR commands. Unrecognized
/// event types are still recorded in the inbox so redelivered/unexpected events remain traceable
/// and deduplicated, even though no business command is dispatched for them.
/// </summary>
public sealed class InventoryIntegrationEventProcessor(ISender sender, IInventoryInbox inbox) : IIntegrationEventProcessor
{
    /// <inheritdoc/>
    public Task<string> ProcessAsync(EventEnvelope envelope)
    {
        return envelope.EventType switch
        {
            nameof(OrderConfirmationRequested) => Reserve(envelope),
            nameof(OrderCancellationRequested) => Release(envelope),
            _ => RecordIgnored(envelope)
        };
    }

    private async Task<string> RecordIgnored(EventEnvelope envelope)
    {
        await inbox.TryRecordAsync(envelope.EventId, CancellationToken.None);
        return "Ignored";
    }

    private Task<string> Reserve(EventEnvelope envelope)
    {
        var request = envelope.Data.Deserialize<OrderConfirmationRequested>()!;
        return sender.Send(new ReserveStockCommand(
            envelope.EventId,
            request.OrderId,
            envelope.Version,
            envelope.CorrelationId,
            request.Lines), CancellationToken.None);
    }

    private Task<string> Release(EventEnvelope envelope)
    {
        var request = envelope.Data.Deserialize<OrderCancellationRequested>()!;
        return sender.Send(new ReleaseStockCommand(
            envelope.EventId,
            request.OrderId,
            envelope.Version,
            envelope.CorrelationId), CancellationToken.None);
    }
}

namespace BuildingBlocks.Contracts;

/// <summary>
/// Published by Sales when an order is cancelled, asking Inventory to release any reserved stock.
/// </summary>
/// <param name="OrderId">
/// The unique identifier of the order that was cancelled.
/// </param>
public sealed record OrderCancellationRequested(Guid OrderId) : IntegrationEventBase;

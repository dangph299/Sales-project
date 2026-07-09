namespace BuildingBlocks.Contracts;

/// <summary>
/// Published by Sales when an order requests confirmation, asking Inventory to reserve stock for
/// each line.
/// </summary>
/// <param name="OrderId">
/// The unique identifier of the order requesting confirmation.
/// </param>
/// <param name="Lines">
/// The product/quantity pairs that Inventory must reserve stock for.
/// </param>
public sealed record OrderConfirmationRequested(Guid OrderId, IReadOnlyCollection<OrderLineIntegration> Lines) : IntegrationEventBase;

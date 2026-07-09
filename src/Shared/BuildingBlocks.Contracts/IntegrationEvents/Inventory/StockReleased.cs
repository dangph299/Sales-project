namespace BuildingBlocks.Contracts;

/// <summary>
/// Published by Inventory when a reservation's stock has been released back to available, in
/// response to an order cancellation.
/// </summary>
/// <param name="OrderId">
/// The unique identifier of the Sales order the reservation was released for.
/// </param>
public sealed record StockReleased(Guid OrderId) : IntegrationEventBase;

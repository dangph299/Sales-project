namespace BuildingBlocks.Contracts;

/// <summary>
/// Published by Inventory when stock has been successfully reserved for every requested line of an order.
/// </summary>
/// <param name="OrderId">
/// The unique identifier of the Sales order the reservation was made for.
/// </param>
public sealed record StockReserved(Guid OrderId) : IntegrationEventBase;

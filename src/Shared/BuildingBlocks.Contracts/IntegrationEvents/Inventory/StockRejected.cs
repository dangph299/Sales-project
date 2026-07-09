namespace BuildingBlocks.Contracts;

/// <summary>
/// Published by Inventory when a stock reservation request could not be fulfilled, for example due
/// to insufficient stock.
/// </summary>
/// <param name="OrderId">
/// The unique identifier of the Sales order the reservation was requested for.
/// </param>
/// <param name="Reason">
/// A human-readable reason the reservation was rejected.
/// </param>
public sealed record StockRejected(Guid OrderId, string Reason) : IntegrationEventBase;

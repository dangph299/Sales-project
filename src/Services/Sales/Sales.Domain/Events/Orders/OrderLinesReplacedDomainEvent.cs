namespace Sales.Domain;

/// <summary>
/// Raised when an <see cref="Order"/>'s lines are replaced with a new set, changing its totals.
/// </summary>
/// <param name="OrderId">Order whose lines were replaced.</param>
/// <param name="TotalQuantity">Total quantity across all lines after the replacement.</param>
/// <param name="Total">Total monetary amount across all lines after the replacement.</param>
public sealed record OrderLinesReplacedDomainEvent(Guid OrderId, int TotalQuantity, decimal Total) : DomainEvent;

namespace Sales.Domain;

/// <summary>
/// Raised when an <see cref="Order"/>'s lines are replaced with a new set, changing its totals.
/// </summary>
/// <param name="OrderId">
/// The unique identifier of the order whose lines were replaced.
/// </param>
/// <param name="TotalQuantity">
/// The total quantity across all lines after the replacement.
/// </param>
/// <param name="Total">
/// The total monetary amount across all lines after the replacement.
/// </param>
public sealed record OrderLinesReplacedDomainEvent(Guid OrderId, int TotalQuantity, decimal Total) : DomainEvent;

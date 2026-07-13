namespace Sales.Application;

/// <summary>
/// Read model for an order, returned by queries and API responses.
/// </summary>
/// <param name="Id">Order's unique identifier.</param>
/// <param name="CustomerId">Customer the order was placed for.</param>
/// <param name="CustomerName">Customer's name as it was when the order was created.</param>
/// <param name="CustomerPhone">Customer's phone number as it was when the order was created.</param>
/// <param name="CreatedAt">UTC instant the order was created.</param>
/// <param name="Status">Order's current lifecycle status, as its <c>ToString()</c> representation.</param>
/// <param name="TotalQuantity">Sum of all lines' quantities.</param>
/// <param name="Total">Sum of all lines' totals.</param>
/// <param name="Version">Order's current optimistic concurrency version.</param>
/// <param name="UpdatedAt">UTC instant the order was last changed.</param>
/// <param name="RejectionReason">Reason Inventory rejected the reservation, or <see langword="null"/> if it was never rejected.</param>
/// <param name="Lines">Order's lines.</param>
public sealed record OrderDto(Guid Id, Guid CustomerId, string CustomerName, string CustomerPhone, DateTimeOffset CreatedAt,
    string Status, int TotalQuantity, decimal Total, long Version, DateTimeOffset UpdatedAt, string? RejectionReason, IReadOnlyCollection<OrderLineDto> Lines);

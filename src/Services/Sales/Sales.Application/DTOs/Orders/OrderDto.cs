namespace Sales.Application;

/// <summary>
/// Read model for an order, returned by queries and API responses.
/// </summary>
/// <param name="Id">
/// The order's unique identifier.
/// </param>
/// <param name="CustomerId">
/// The unique identifier of the customer the order was placed for.
/// </param>
/// <param name="CustomerName">
/// The customer's name as it was when the order was created.
/// </param>
/// <param name="CustomerPhone">
/// The customer's phone number as it was when the order was created.
/// </param>
/// <param name="CreatedAt">
/// The UTC instant the order was created.
/// </param>
/// <param name="Status">
/// The order's current lifecycle status, as its <c>ToString()</c> representation.
/// </param>
/// <param name="TotalQuantity">
/// The sum of all lines' quantities.
/// </param>
/// <param name="Total">
/// The sum of all lines' totals.
/// </param>
/// <param name="Version">
/// The order's current optimistic concurrency version.
/// </param>
/// <param name="UpdatedAt">
/// The UTC instant the order was last changed.
/// </param>
/// <param name="RejectionReason">
/// The reason Inventory rejected the reservation, or <see langword="null"/> if it was never rejected.
/// </param>
/// <param name="Lines">
/// The order's lines.
/// </param>
public sealed record OrderDto(Guid Id, Guid CustomerId, string CustomerName, string CustomerPhone, DateTimeOffset CreatedAt,
    string Status, int TotalQuantity, decimal Total, long Version, DateTimeOffset UpdatedAt, string? RejectionReason, IReadOnlyCollection<OrderLineDto> Lines);

namespace Sales.Application.Features.Orders.DTOs;

/// <summary>
/// Read model for an order, returned by queries and API responses.
/// </summary>
/// <remarks>
/// Every customer field here is the order's own snapshot, read from the order row. The normalized
/// and reversed phone columns are deliberately absent: they exist only so the database can index
/// phone searches, and no client has any use for them.
/// </remarks>
/// <param name="Id">Order's unique identifier.</param>
/// <param name="OrderCode">Order's backend-assigned business code.</param>
/// <param name="CustomerId">Customer the order was originally placed for.</param>
/// <param name="CustomerName">Customer's name as recorded on this order.</param>
/// <param name="CustomerPhone">Customer's phone number as recorded on this order, in the format it was entered in.</param>
/// <param name="CustomerEmail">Customer's email as recorded on this order, or <see langword="null"/>.</param>
/// <param name="CustomerAddress">Customer's address as recorded on this order, or <see langword="null"/>.</param>
/// <param name="CreatedAt">UTC instant the order was created.</param>
/// <param name="Status">Order's current lifecycle status, as its <c>ToString()</c> representation.</param>
/// <param name="TotalQuantity">Sum of all lines' quantities.</param>
/// <param name="Total">Sum of all lines' totals.</param>
/// <param name="Version">Order's current optimistic concurrency version.</param>
/// <param name="UpdatedAt">UTC instant the order was last changed.</param>
/// <param name="RejectionReason">Reason Inventory rejected the reservation, or <see langword="null"/> if it was never rejected.</param>
/// <param name="Lines">Order's lines.</param>
public sealed record OrderDto(
    Guid Id,
    string OrderCode,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    string? CustomerAddress,
    DateTimeOffset CreatedAt,
    string Status,
    int TotalQuantity,
    decimal Total,
    long Version,
    DateTimeOffset UpdatedAt,
    string? RejectionReason,
    IReadOnlyCollection<OrderLineDto> Lines);

using MediatR;

namespace Sales.Application;

/// <summary>
/// Query to search orders by creation date range and/or customer name/phone.
/// </summary>
/// <param name="From">
/// An optional inclusive lower bound on the order's creation time.
/// </param>
/// <param name="To">
/// An optional inclusive upper bound on the order's creation time.
/// </param>
/// <param name="Customer">
/// An optional substring to match against the order's customer name or phone.
/// </param>
/// <param name="Page">
/// The 1-based page number to return. Defaults to 1.
/// </param>
/// <param name="PageSize">
/// The maximum number of items per page. Defaults to 20.
/// </param>
public sealed record SearchOrders(DateTimeOffset? From, DateTimeOffset? To, string? Customer, int Page = 1, int PageSize = 20) : IRequest<PagedResult<OrderDto>>;

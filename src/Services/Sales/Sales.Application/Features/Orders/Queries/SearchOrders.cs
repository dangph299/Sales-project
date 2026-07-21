using Sales.Application.Features.Orders.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Queries;

/// <summary>
/// Query to search orders by creation date range, customer name/phone, and/or status.
/// </summary>
/// <param name="From">An optional inclusive lower bound on the order's creation time.</param>
/// <param name="To">An optional inclusive upper bound on the order's creation time.</param>
/// <param name="Customer">An optional substring to match against the order's customer name or phone.</param>
/// <param name="Status">An optional status the order must currently be in.</param>
/// <param name="Page">1-based page number. Defaults to 1.</param>
/// <param name="PageSize">Maximum page size. Defaults to 20.</param>
public sealed record SearchOrders(DateTimeOffset? From, DateTimeOffset? To, string? Customer, OrderStatus? Status = null, int Page = 1, int PageSize = 20) : IQuery<PagedResult<OrderDto>>;

using Sales.Application.Features.Orders.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Queries;

/// <summary>
/// Query to search orders by independent filters.
/// </summary>
/// <remarks>
/// Each filter names exactly what it searches. There is no combined term whose meaning the backend
/// has to guess, and no filter is applied on the client to whichever page happens to be loaded.
/// </remarks>
/// <param name="OrderNumber">An optional whole or partial order code, matched from the start.</param>
/// <param name="CustomerName">An optional keyword matched anywhere within the order's customer name snapshot.</param>
/// <param name="CustomerPhone">An optional phone fragment, in any format, matched against the order's customer phone snapshot.</param>
/// <param name="CustomerPhoneMatchMode">Which end of the phone number <paramref name="CustomerPhone"/> must match. Defaults to <see cref="OrderCustomerPhoneMatchMode.Prefix"/>.</param>
/// <param name="From">An optional inclusive lower bound on the order's creation time.</param>
/// <param name="To">An optional inclusive upper bound on the order's creation time.</param>
/// <param name="Status">An optional status the order must currently be in.</param>
/// <param name="Page">1-based page number. Defaults to 1.</param>
/// <param name="PageSize">Maximum page size. Defaults to 20.</param>
public sealed record SearchOrders(
    string? OrderNumber = null,
    string? CustomerName = null,
    string? CustomerPhone = null,
    OrderCustomerPhoneMatchMode CustomerPhoneMatchMode = OrderCustomerPhoneMatchMode.Prefix,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    OrderStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<OrderDto>>;

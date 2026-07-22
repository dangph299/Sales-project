using Sales.Application.Features.Orders.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Interfaces;

/// <summary>
/// Query-side read port for orders, implemented directly against the database without going
/// through the command-side repository/aggregate.
/// </summary>
public interface IOrderReadService
{
    /// <summary>
    /// Gets a single order by its identifier.
    /// </summary>
    /// <param name="id">Order identifier.</param>
    /// <returns>Order, or <see langword="null"/> if none exists.</returns>
    Task<OrderDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches orders by independent filters, reading every customer value from the order's own
    /// snapshot rather than joining the customer table.
    /// </summary>
    /// <param name="orderNumber">An optional whole or partial order code, matched from the start.</param>
    /// <param name="customerName">An optional keyword matched anywhere within the order's customer name.</param>
    /// <param name="customerPhone">An optional phone fragment, in any format. Normalized here, so the caller sends what the user typed.</param>
    /// <param name="customerPhoneMatchMode">Which end of the phone number <paramref name="customerPhone"/> must match.</param>
    /// <param name="from">An optional inclusive lower bound on <c>CreatedAt</c>.</param>
    /// <param name="to">An optional inclusive upper bound on <c>CreatedAt</c>.</param>
    /// <param name="status">An optional status the order must currently be in.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum page size.</param>
    /// <returns>A page of matching orders.</returns>
    Task<PagedResult<OrderDto>> SearchAsync(
        string? orderNumber,
        string? customerName,
        string? customerPhone,
        OrderCustomerPhoneMatchMode customerPhoneMatchMode,
        DateTimeOffset? from,
        DateTimeOffset? to,
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

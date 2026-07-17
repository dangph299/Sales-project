using MediatR;
using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Orders.DTOs;
using Sales.Application.Features.Orders.Interfaces;

namespace Sales.Application.Features.Orders.Queries;

/// <summary>
/// Handles <see cref="GetOrder"/> by delegating to the order read service.
/// </summary>
public sealed class GetOrderHandler(IOrderReadService readService) : IRequestHandler<GetOrder, OrderDto>
{
    /// <summary>
    /// Loads the requested order.
    /// </summary>
    /// <param name="request">Query identifying the order.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order, mapped to an <see cref="OrderDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown when no order exists with the given identifier.</exception>
    public async Task<OrderDto> Handle(GetOrder request, CancellationToken ct) =>
        await readService.GetAsync(request.Id, ct) ?? throw new NotFoundException("Order", request.Id);
}

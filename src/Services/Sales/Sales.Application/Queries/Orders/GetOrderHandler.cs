using MediatR;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="GetOrder"/> by delegating to the order read service.
/// </summary>
public sealed class GetOrderHandler(IOrderReadService readService) : IRequestHandler<GetOrder, OrderDto>
{
    /// <summary>
    /// Loads the requested order.
    /// </summary>
    /// <param name="request">
    /// The query identifying the order to load.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The order, mapped to an <see cref="OrderDto"/>.
    /// </returns>
    /// <exception cref="NotFoundException">
    /// Thrown when no order exists with the given identifier.
    /// </exception>
    public async Task<OrderDto> Handle(GetOrder request, CancellationToken ct) =>
        await readService.GetAsync(request.Id, ct) ?? throw new NotFoundException("Order", request.Id);
}

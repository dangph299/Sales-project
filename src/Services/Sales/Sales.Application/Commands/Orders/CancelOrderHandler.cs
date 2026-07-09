using MediatR;
using Microsoft.Extensions.Logging;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="CancelOrder"/> by loading the order under an optimistic concurrency check and cancelling it.
/// </summary>
public sealed class CancelOrderHandler(IOrderRepository orders, IUnitOfWork uow, ILogger<CancelOrderHandler> logger) : IRequestHandler<CancelOrder, OrderDto>
{
    /// <summary>
    /// Loads the order, cancels it, and commits the unit of work.
    /// </summary>
    /// <param name="request">
    /// The command identifying the order to cancel and its expected version.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The cancelled order, mapped to an <see cref="OrderDto"/>.
    /// </returns>
    /// <exception cref="NotFoundException">
    /// Thrown when no order exists with the given identifier.
    /// </exception>
    /// <exception cref="ConflictException">
    /// Thrown when the order's actual version does not match <see cref="CancelOrder.ExpectedVersion"/>.
    /// </exception>
    /// <exception cref="Sales.Domain.DomainException">
    /// Thrown when the order is confirmed or pending inventory and cannot be cancelled.
    /// </exception>
    public async Task<OrderDto> Handle(CancelOrder request, CancellationToken ct)
    {
        var order = await orders.LoadAndCheck(request.Id, request.ExpectedVersion, ct);
        order.Cancel();
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Order cancelled {OrderId}", order.Id);
        return order.ToDto();
    }
}

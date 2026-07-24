using MapsterMapper;
using Microsoft.Extensions.Logging;
using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Orders.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Commands;

/// <summary>
/// Handles <see cref="CancelOrder"/> by loading the order under an optimistic concurrency check and cancelling it.
/// </summary>
public sealed class CancelOrderHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ILogger<CancelOrderHandler> logger,
    IMapper mapper) : ICommandHandler<CancelOrder, OrderDto>
{
    /// <summary>
    /// Loads the order, cancels it, and commits the unit of work.
    /// </summary>
    /// <param name="request">Command identifying the order to cancel and its expected version.</param>
    /// <returns>Cancelled order, mapped to an <see cref="OrderDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown when no order exists with the given identifier.</exception>
    /// <exception cref="ConflictException">Thrown when the order's actual version does not match <see cref="CancelOrder.ExpectedVersion"/>.</exception>
    /// <exception cref="Sales.Domain.DomainException">Thrown when the order is confirmed or pending inventory and cannot be cancelled.</exception>
    public async Task<OrderDto> Handle(CancelOrder request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.LoadAndCheck(
            request.Id,
            request.ExpectedVersion,
            cancellationToken);
        order.Cancel();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Order cancelled {OrderId}", order.Id);
        return mapper.Map<OrderDto>(order);
    }
}

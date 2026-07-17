using MapsterMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Sales.Application.Features.Orders.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Commands;

/// <summary>
/// Handles <see cref="UndoConfirmOrder"/> by loading the order under an optimistic concurrency check and undoing its confirmation.
/// </summary>
public sealed class UndoConfirmOrderHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ILogger<UndoConfirmOrderHandler> logger,
    IMapper mapper) : IRequestHandler<UndoConfirmOrder, OrderDto>
{
    /// <summary>
    /// Loads the order, undoes its confirmation, and commits the unit of work.
    /// </summary>
    /// <param name="request">Command identifying the order to undo confirmation for and its expected version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Order with confirmation undone, mapped to an <see cref="OrderDto"/>.</returns>
    public async Task<OrderDto> Handle(UndoConfirmOrder request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.LoadAndCheck(
            request.Id,
            request.ExpectedVersion,
            cancellationToken);
        order.UndoConfirmed();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Order confirmation undone {OrderId}", order.Id);
        return mapper.Map<OrderDto>(order);
    }
}

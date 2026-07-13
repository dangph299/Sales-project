using MediatR;
using Microsoft.Extensions.Logging;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="UndoConfirmOrder"/> by loading the order under an optimistic concurrency check and undoing its confirmation.
/// </summary>
public sealed class UndoConfirmOrderHandler(IOrderRepository orders, IUnitOfWork uow, ILogger<UndoConfirmOrderHandler> logger) : IRequestHandler<UndoConfirmOrder, OrderDto>
{
    /// <summary>
    /// Loads the order, undoes its confirmation, and commits the unit of work.
    /// </summary>
    /// <param name="request">Command identifying the order to undo confirmation for and its expected version.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order with confirmation undone, mapped to an <see cref="OrderDto"/>.</returns>
    public async Task<OrderDto> Handle(UndoConfirmOrder request, CancellationToken ct)
    {
        var order = await orders.LoadAndCheck(request.Id, request.ExpectedVersion, ct);
        order.UndoConfirmed();
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Order confirmation undone {OrderId}", order.Id);
        return order.ToDto();
    }
}

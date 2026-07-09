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
    /// <param name="request">
    /// The command identifying the order to undo confirmation for and its expected version.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The order with confirmation undone, mapped to an <see cref="OrderDto"/>.
    /// </returns>
    public async Task<OrderDto> Handle(UndoConfirmOrder request, CancellationToken ct)
    {
        var order = await orders.LoadAndCheck(request.Id, request.ExpectedVersion, ct);
        order.UndoConfirmed();
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Order confirmation undone {OrderId}", order.Id);
        return order.ToDto();
    }
}

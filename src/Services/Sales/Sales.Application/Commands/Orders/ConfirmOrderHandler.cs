using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="ConfirmOrder"/> by moving a draft order to PendingInventory, which raises the
/// domain event that asks Inventory to reserve stock.
/// </summary>
public sealed class ConfirmOrderHandler(IOrderRepository orders, IUnitOfWork uow, ILogger<ConfirmOrderHandler> logger) : IRequestHandler<ConfirmOrder, OrderDto>
{
    /// <summary>
    /// Loads the order, requests confirmation, and commits the unit of work.
    /// </summary>
    /// <param name="request">Command identifying the order to confirm and its expected version.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order in its PendingInventory status, mapped to an <see cref="OrderDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown when no order exists with the given identifier.</exception>
    /// <exception cref="ConflictException">Thrown when the order's actual version does not match <see cref="ConfirmOrder.ExpectedVersion"/>.</exception>
    /// <exception cref="Sales.Domain.DomainException">Thrown when the order is not in the Draft status.</exception>
    public async Task<OrderDto> Handle(ConfirmOrder request, CancellationToken ct)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["OrderId"] = request.Id });
        var sw = Stopwatch.StartNew();

        logger.LogInformation("ConfirmOrder started");

        var order = await orders.LoadAndCheck(request.Id, request.ExpectedVersion, ct);
        order.RequestConfirmation();
        logger.LogInformation("Order status changed {OldStatus} -> {NewStatus}", "Draft", "PendingInventory");

        await uow.SaveChangesAsync(ct);

        var dto = order.ToDto();
        logger.LogInformation("ConfirmOrder completed {ElapsedMs}", sw.ElapsedMilliseconds);
        return dto;
    }
}

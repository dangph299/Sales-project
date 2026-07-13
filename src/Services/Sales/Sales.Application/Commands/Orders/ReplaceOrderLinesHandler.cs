using MediatR;
using Microsoft.Extensions.Logging;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="ReplaceOrderLines"/> by loading a draft order under an optimistic concurrency
/// check and replacing its lines.
/// </summary>
public sealed class ReplaceOrderLinesHandler(IOrderRepository orders, IProductRepository products, IUnitOfWork uow, ILogger<ReplaceOrderLinesHandler> logger)
    : IRequestHandler<ReplaceOrderLines, OrderDto>
{
    /// <summary>
    /// Loads the order, resolves the new requested lines, replaces them, and commits the unit of work.
    /// </summary>
    /// <param name="request">Command identifying the order, its expected version, and the new lines.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order with its new lines, mapped to an <see cref="OrderDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown when the order or one of the requested products does not exist.</exception>
    /// <exception cref="ConflictException">Thrown when the order's actual version does not match <see cref="ReplaceOrderLines.ExpectedVersion"/>.</exception>
    /// <exception cref="Sales.Domain.DomainException">Thrown when the order is not in the Draft status, a requested product is inactive, or the requested lines are empty or contain a repeated product.</exception>
    public async Task<OrderDto> Handle(ReplaceOrderLines request, CancellationToken ct)
    {
        var order = await orders.LoadAndCheck(request.Id, request.ExpectedVersion, ct);
        order.ReplaceLines(await products.Materialize(request.Lines, ct));
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Order lines replaced {OrderId} {TotalQuantity}", order.Id, order.TotalQuantity);
        return order.ToDto();
    }
}

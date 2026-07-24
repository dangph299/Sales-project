using MapsterMapper;
using Microsoft.Extensions.Logging;
using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Orders.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Commands;

/// <summary>
/// Handles <see cref="UpdateOrderCustomer"/> by rewriting the order's own customer snapshot.
/// </summary>
/// <remarks>
/// The dependency list is the guarantee: with no customer repository injected, this handler has no
/// way to read or write the customer table, so an edit here cannot leak back into
/// <see cref="Customer"/>. The order's <see cref="Order.CustomerId"/> comes from the order itself
/// and is never re-resolved from the new phone number, so correcting a typo in a phone number does
/// not silently re-point the order at somebody else.
/// </remarks>
public sealed class UpdateOrderCustomerHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateOrderCustomerHandler> logger,
    IMapper mapper)
    : ICommandHandler<UpdateOrderCustomer, OrderDto>
{
    /// <summary>
    /// Loads the order under an optimistic concurrency check, replaces its customer snapshot, and
    /// commits the unit of work.
    /// </summary>
    /// <param name="request">Command identifying the order, its expected version, and the new customer details.</param>
    /// <returns>Updated order, mapped to an <see cref="OrderDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown when no order exists with the given identifier.</exception>
    /// <exception cref="ConflictException">Thrown when the order's actual version does not match <see cref="UpdateOrderCustomer.ExpectedVersion"/>.</exception>
    /// <exception cref="DomainException">Thrown when the order is not in the Draft status, or the customer details are invalid.</exception>
    public async Task<OrderDto> Handle(UpdateOrderCustomer request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.LoadAndCheck(request.Id, request.ExpectedVersion, cancellationToken);

        order.UpdateCustomerSnapshot(OrderCustomerSnapshot.Create(
            order.CustomerId,
            request.Name,
            request.Phone,
            request.Email,
            request.Address));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Order customer snapshot updated {OrderId} {OrderCode}", order.Id, order.OrderCode);
        return mapper.Map<OrderDto>(order);
    }
}

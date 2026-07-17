using MapsterMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Orders.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Commands;

/// <summary>
/// Handles <see cref="CreateOrder"/> by resolving the customer and requested products, then
/// creating and persisting a new draft <see cref="Order"/> aggregate.
/// </summary>
public sealed class CreateOrderHandler(
    IOrderRepository orderRepository,
    IRepository<Customer> customerRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateOrderHandler> logger,
    IMapper mapper)
    : IRequestHandler<CreateOrder, OrderDto>
{
    /// <summary>
    /// Resolves the customer and requested product lines, creates the order, and commits the unit of work.
    /// </summary>
    /// <param name="request">Command describing the customer and requested lines.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Newly created draft order, mapped to an <see cref="OrderDto"/>.</returns>
    /// <exception cref="NotFoundException">Thrown when the customer or one of the requested products does not exist.</exception>
    /// <exception cref="Sales.Domain.DomainException">Thrown when a requested product is inactive, or the requested lines are empty or contain a repeated product.</exception>
    public async Task<OrderDto> Handle(CreateOrder request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken) ??
            throw new NotFoundException(nameof(Customer), request.CustomerId);
        var orderLineItems = await productRepository.Materialize(request.Lines, cancellationToken);
        var customerSnapshot = CustomerSnapshot.Create(customer.Id, customer.Name, customer.Phone);
        var order = Order.Create(customerSnapshot, orderLineItems);
        await orderRepository.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Order created {OrderId} {CustomerId}", order.Id, order.CustomerId);
        return mapper.Map<OrderDto>(order);
    }
}

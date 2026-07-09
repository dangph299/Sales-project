using MediatR;
using Microsoft.Extensions.Logging;
using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Handles <see cref="CreateOrder"/> by resolving the customer and requested products, then
/// creating and persisting a new draft <see cref="Order"/> aggregate.
/// </summary>
public sealed class CreateOrderHandler(IOrderRepository orders, IRepository<Customer> customers, IProductRepository products, IUnitOfWork uow, ILogger<CreateOrderHandler> logger)
    : IRequestHandler<CreateOrder, OrderDto>
{
    /// <summary>
    /// Resolves the customer and requested product lines, creates the order, and commits the unit of work.
    /// </summary>
    /// <param name="request">
    /// The command describing the customer and requested lines.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The newly created draft order, mapped to an <see cref="OrderDto"/>.
    /// </returns>
    /// <exception cref="NotFoundException">
    /// Thrown when the customer or one of the requested products does not exist.
    /// </exception>
    /// <exception cref="Sales.Domain.DomainException">
    /// Thrown when a requested product is inactive, or the requested lines are empty or contain a
    /// repeated product.
    /// </exception>
    public async Task<OrderDto> Handle(CreateOrder request, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId, ct) ?? throw new NotFoundException(nameof(Customer), request.CustomerId);
        var lines = await products.Materialize(request.Lines, ct);
        var customerSnapshot = CustomerSnapshot.Create(customer.Id, customer.Name, customer.Phone);
        var order = Order.Create(customerSnapshot, lines);
        await orders.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("Order created {OrderId} {CustomerId}", order.Id, order.CustomerId);
        return order.ToDto();
    }
}

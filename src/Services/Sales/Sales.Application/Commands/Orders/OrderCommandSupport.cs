using Sales.Domain;

namespace Sales.Application;

internal static class OrderCommandSupport
{
    /// <summary>
    /// Loads an order with its lines and verifies it is at the expected version before any
    /// business behavior is invoked on it.
    /// </summary>
    /// <param name="orderRepository">Order repository.</param>
    /// <param name="orderId">Order identifier.</param>
    /// <param name="expectedVersion">Version the caller expects the order to currently be at.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Order, with its lines populated.</returns>
    /// <exception cref="NotFoundException">Thrown when no order exists with the given identifier.</exception>
    /// <exception cref="ConflictException">Thrown when the order's actual version does not match <paramref name="expectedVersion"/>.</exception>
    public static async Task<Order> LoadAndCheck(
        this IOrderRepository orderRepository,
        Guid orderId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetWithLinesAsync(orderId, cancellationToken) ??
            throw new NotFoundException(nameof(Order), orderId);
        if (order.Version != expectedVersion) throw new ConflictException(order.Version);
        return order;
    }

    /// <summary>
    /// Loads the requested products in a single bulk query and resolves each requested line into a
    /// validated <see cref="OrderLineItem"/> snapshot.
    /// </summary>
    /// <param name="productRepository">Product repository.</param>
    /// <param name="orderLineInputs">Requested product/quantity/discount lines.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resolved order line items, one per input, in the same order.</returns>
    /// <exception cref="NotFoundException">Thrown when one of the requested products does not exist.</exception>
    /// <exception cref="Sales.Domain.DomainException">Thrown when a requested product is inactive or has invalid snapshot data.</exception>
    public static async Task<IReadOnlyCollection<OrderLineItem>> Materialize(
        this IProductRepository productRepository,
        IEnumerable<OrderLineInput> orderLineInputs,
        CancellationToken cancellationToken)
    {
        var orderLineInputList = orderLineInputs.ToList();
        var productsById = (await productRepository.GetByIdsAsync(
            orderLineInputList.Select(x => x.ProductId),
            cancellationToken)).ToDictionary(x => x.Id);
        var orderLineItems = new List<OrderLineItem>();
        foreach (var orderLineInput in orderLineInputList)
        {
            if (!productsById.TryGetValue(orderLineInput.ProductId, out var product))
                throw new NotFoundException(nameof(Product), orderLineInput.ProductId);

            orderLineItems.Add(new OrderLineItem(
                ProductSnapshot.Create(product.Id, product.Sku, product.Name, product.Price, product.IsActive),
                orderLineInput.Quantity,
                orderLineInput.DiscountPercent ?? throw new DomainException("Discount is required.")));
        }
        return orderLineItems;
    }
}

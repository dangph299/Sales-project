using Sales.Domain;

namespace Sales.Application;

/// <summary>
/// Shared helpers for order command handlers: loading an order under an optimistic concurrency
/// check, and resolving requested product lines into validated <see cref="OrderLineItem"/> snapshots.
/// </summary>
internal static class OrderCommandSupport
{
    /// <summary>
    /// Loads an order with its lines and verifies it is at the expected version before any
    /// business behavior is invoked on it.
    /// </summary>
    /// <param name="orders">
    /// The order repository to load from.
    /// </param>
    /// <param name="id">
    /// The unique identifier of the order to load.
    /// </param>
    /// <param name="expectedVersion">
    /// The version the caller expects the order to currently be at.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The order, with its lines populated.
    /// </returns>
    /// <exception cref="NotFoundException">
    /// Thrown when no order exists with the given identifier.
    /// </exception>
    /// <exception cref="ConflictException">
    /// Thrown when the order's actual version does not match <paramref name="expectedVersion"/>.
    /// </exception>
    public static async Task<Order> LoadAndCheck(this IOrderRepository orders, Guid id, long expectedVersion, CancellationToken ct)
    {
        var order = await orders.GetWithLinesAsync(id, ct) ?? throw new NotFoundException(nameof(Order), id);
        if (order.Version != expectedVersion) throw new ConflictException(order.Version);
        return order;
    }

    /// <summary>
    /// Loads the requested products in a single bulk query and resolves each requested line into a
    /// validated <see cref="OrderLineItem"/> snapshot.
    /// </summary>
    /// <param name="products">
    /// The product repository to load products from.
    /// </param>
    /// <param name="inputs">
    /// The requested product/quantity/discount lines.
    /// </param>
    /// <param name="ct">
    /// A token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// The resolved order line items, one per input, in the same order.
    /// </returns>
    /// <exception cref="NotFoundException">
    /// Thrown when one of the requested products does not exist.
    /// </exception>
    /// <exception cref="Sales.Domain.DomainException">
    /// Thrown when a requested product is inactive or has invalid snapshot data.
    /// </exception>
    public static async Task<IReadOnlyCollection<OrderLineItem>> Materialize(this IProductRepository products, IEnumerable<OrderLineInput> inputs, CancellationToken ct)
    {
        var inputList = inputs.ToList();
        var productsById = (await products.GetByIdsAsync(inputList.Select(x => x.ProductId), ct)).ToDictionary(x => x.Id);
        var result = new List<OrderLineItem>();
        foreach (var input in inputList)
        {
            if (!productsById.TryGetValue(input.ProductId, out var product))
                throw new NotFoundException(nameof(Product), input.ProductId);

            result.Add(new OrderLineItem(
                ProductSnapshot.Create(product.Id, product.Sku, product.Name, product.Price, product.IsActive),
                input.Quantity,
                input.DiscountPercent ?? throw new DomainException("Discount is required.")));
        }
        return result;
    }
}

using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Orders.DTOs;
using Sales.Domain;

namespace Sales.Application.Features.Orders.Commands;

internal static class OrderCommandSupport
{
    /// <summary>
    /// Loads an order with its lines and verifies it is at the expected version before any
    /// business behavior is invoked on it.
    /// </summary>
    /// <param name="orderRepository">Order repository.</param>
    /// <param name="orderId">Order identifier.</param>
    /// <param name="expectedVersion">Version the caller expects the order to currently be at.</param>
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
    /// <returns>Resolved order line items, one per input, in the same order.</returns>
    /// <exception cref="NotFoundException">Thrown when one of the requested products does not exist.</exception>
    /// <exception cref="Sales.Domain.DomainException">Thrown when a requested product is inactive or has invalid snapshot data.</exception>
    public static async Task<IReadOnlyCollection<OrderLineItem>> Materialize(
        this IProductRepository productRepository,
        IEnumerable<OrderLineInput> orderLineInputs,
        CancellationToken cancellationToken)
    {
        var orderLineInputList = orderLineInputs.ToList();
        var variantsById = (await productRepository.GetVariantsByIdsAsync(
            orderLineInputList.Select(x => x.ProductVariantId),
            cancellationToken)).ToDictionary(x => x.Id);
        var productsById = (await productRepository.GetWithVariantsByIdsAsync(
            variantsById.Values.Select(x => x.ProductId),
            cancellationToken)).ToDictionary(x => x.Id);
        var orderLineItems = new List<OrderLineItem>();
        foreach (var orderLineInput in orderLineInputList)
        {
            if (!variantsById.TryGetValue(orderLineInput.ProductVariantId, out var productVariant))
                throw new NotFoundException(nameof(ProductVariant), orderLineInput.ProductVariantId);
            if (!productsById.TryGetValue(productVariant.ProductId, out var product))
                throw new NotFoundException(nameof(Product), productVariant.ProductId);

            var color = await productRepository.GetColorAsync(productVariant.ColorId, cancellationToken) ??
                throw new NotFoundException(nameof(Color), productVariant.ColorId);
            var size = await productRepository.GetSizeAsync(productVariant.SizeId, cancellationToken) ??
                throw new NotFoundException(nameof(Size), productVariant.SizeId);

            orderLineItems.Add(new OrderLineItem(
                ProductSnapshot.Create(
                    product.Id,
                    productVariant.Id,
                    product.ProductCode,
                    product.Name,
                    productVariant.Sku,
                    color.ColorCode,
                    color.Name,
                    size.Code,
                    productVariant.Price,
                    product.Status != EProductStatus.Draft && productVariant.Status == EProductVariantStatus.Published),
                orderLineInput.Quantity,
                orderLineInput.DiscountPercent ?? throw new DomainException("Discount is required.")));
        }
        return orderLineItems;
    }

    /// <summary>
    /// Revalidates the live product state for existing draft order lines before the order asks
    /// Inventory to reserve stock.
    /// </summary>
    /// <param name="productRepository">Product repository.</param>
    /// <param name="orderLines">Existing order lines.</param>
    /// <exception cref="NotFoundException">Thrown when an order line product no longer exists or was deleted.</exception>
    /// <exception cref="Sales.Domain.DomainException">Thrown when an order line product is inactive.</exception>
    public static async Task EnsureOrderLinesCanStillBeOrdered(
        this IProductRepository productRepository,
        IEnumerable<OrderLine> orderLines,
        CancellationToken cancellationToken)
    {
        var lines = orderLines.ToList();
        var variantsById = (await productRepository.GetVariantsByIdsAsync(
            lines.Select(x => x.ProductVariantId),
            cancellationToken)).ToDictionary(x => x.Id);
        if (variantsById.Count == 0)
        {
            var productsByLineProductId = (await productRepository.GetByIdsAsync(
                lines.Select(x => x.ProductId),
                cancellationToken)).ToDictionary(x => x.Id);
            foreach (var line in lines)
            {
                if (!productsByLineProductId.TryGetValue(line.ProductId, out var product))
                    throw new NotFoundException(nameof(Product), line.ProductId);

                if (!product.IsActive)
                {
                    throw new DomainException("Only sellable products can be ordered.");
                }
            }

            return;
        }

        foreach (var line in lines)
        {
            if (!variantsById.TryGetValue(line.ProductVariantId, out var productVariant))
                throw new NotFoundException(nameof(ProductVariant), line.ProductVariantId);

            if (productVariant.Status != EProductVariantStatus.Published)
            {
                throw new DomainException("Only published product variants can be ordered.");
            }
        }
    }
}

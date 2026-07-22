using BuildingBlocks.Domain;
using Microsoft.Extensions.Logging;
using Sales.Application.Common.Exceptions;
using Sales.Application.Features.Orders.Commands;
using Sales.Domain;

namespace Sales.Application.Tests;

public sealed class ConfirmOrderHandlerTests
{
    [Fact]
    public async Task Confirm_moves_order_to_pending_inventory_and_logs_start_and_completion()
    {
        var product = ProductTestFactory.CreatePublishedProduct("sku", "Product", 100);
        var order = CreateOrder(product);
        var logger = new RecordingLogger<ConfirmOrderHandler>();
        var handler = new ConfirmOrderHandler(
            new FakeOrderRepository(order),
            new FakeProductRepository(product),
            new FakeUnitOfWork(),
            logger,
            SalesMapperFactory.Create());

        var dto = await handler.Handle(new ConfirmOrder(order.Id, order.Version), CancellationToken.None);

        Assert.Equal("PendingInventory", dto.Status);
        Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Warning);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("ConfirmOrder started"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("ConfirmOrder completed"));
    }

    [Fact]
    public async Task Confirm_does_not_swallow_the_exception_when_the_order_is_missing()
    {
        // ConfirmOrderHandler itself must not catch/log: the boundary that dispatches the command owns
        // the single failure log (ApiExceptionHandler over HTTP), so the exception must propagate untouched.
        var logger = new RecordingLogger<ConfirmOrderHandler>();
        var handler = new ConfirmOrderHandler(
            new FakeOrderRepository(null),
            new FakeProductRepository(),
            new FakeUnitOfWork(),
            logger,
            SalesMapperFactory.Create());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new ConfirmOrder(Guid.NewGuid(), 1), CancellationToken.None));

        Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Warning);
    }

    [Fact]
    public async Task Confirm_rejects_when_an_order_line_product_was_deleted_after_draft_creation()
    {
        var order = CreateOrder();
        var logger = new RecordingLogger<ConfirmOrderHandler>();
        var handler = new ConfirmOrderHandler(
            new FakeOrderRepository(order),
            new FakeProductRepository(),
            new FakeUnitOfWork(),
            logger,
            SalesMapperFactory.Create());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new ConfirmOrder(order.Id, order.Version), CancellationToken.None));

        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public async Task Confirm_accepts_when_an_order_line_variant_was_discontinued_after_draft_creation()
    {
        var product = ProductTestFactory.CreatePublishedProduct("sku", "Product", 100);
        var order = CreateOrder(product);
        product.DiscontinueVariant(ProductTestFactory.PrimaryVariant(product).Id);
        var logger = new RecordingLogger<ConfirmOrderHandler>();
        var handler = new ConfirmOrderHandler(
            new FakeOrderRepository(order),
            new FakeProductRepository(product),
            new FakeUnitOfWork(),
            logger,
            SalesMapperFactory.Create());

        var dto = await handler.Handle(new ConfirmOrder(order.Id, order.Version), CancellationToken.None);

        Assert.Equal("PendingInventory", dto.Status);
    }

    [Fact]
    public async Task Confirm_rejects_when_an_order_line_variant_is_draft()
    {
        var product = Product.Create("sku", "Product", null, Guid.NewGuid());
        product.AddVariant(Color.Create(Guid.NewGuid(), "BLK", "Black", "#000000"), Size.Create(Guid.NewGuid(), "M", "Medium", 40), 100);
        var order = CreateOrder(product);
        var logger = new RecordingLogger<ConfirmOrderHandler>();
        var handler = new ConfirmOrderHandler(
            new FakeOrderRepository(order),
            new FakeProductRepository(product),
            new FakeUnitOfWork(),
            logger,
            SalesMapperFactory.Create());

        await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new ConfirmOrder(order.Id, order.Version), CancellationToken.None));

        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    private static Order CreateOrder(Product? product = null)
    {
        product ??= ProductTestFactory.CreatePublishedProduct("sku", "Product", 100);
        var variant = ProductTestFactory.PrimaryVariant(product);
        return Order.Create(OrderTestFactory.NextOrderCode(), OrderCustomerSnapshot.Create(Guid.NewGuid(), "A", "0901234567", null, null),
            [
                new(ProductSnapshot.Create(
                    product.Id,
                    variant.Id,
                    product.ProductCode,
                    product.Name,
                    variant.Sku,
                    "BLK",
                    "Black",
                    "M",
                    variant.Price,
                    true), 1, 0)
            ]);
    }

    private sealed class FakeOrderRepository(Order? order) : IOrderRepository
    {
        public Task<IReadOnlyCollection<Guid>> FindExpiredCancellableOrderIdsAsync(
            DateTimeOffset orderUpdatedBefore,
            int batchSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Guid>>([]);

        public Task<Order?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(order);
        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(order);
        public Task<IReadOnlyList<Order>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Order>>([]);
        public Task AddAsync(Order aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Order aggregate) { }
        public void Delete(Order aggregate) { }
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly IReadOnlyList<Product> _products;

        public FakeProductRepository()
        {
            _products = [];
        }

        public FakeProductRepository(Product product)
        {
            _products = [product];
        }

        public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
            Task.FromResult(_products.SingleOrDefault(x => x.Sku == sku));

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_products.SingleOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idSet = ids.ToHashSet();
            return Task.FromResult<IReadOnlyList<Product>>(_products.Where(x => idSet.Contains(x.Id)).ToArray());
        }

        public Task<IReadOnlyList<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<Guid> variantIds, CancellationToken cancellationToken = default)
        {
            var idSet = variantIds.ToHashSet();
            return Task.FromResult<IReadOnlyList<ProductVariant>>(
                _products.SelectMany(x => x.Variants).Where(x => idSet.Contains(x.Id)).ToArray());
        }

        public Task AddAsync(Product aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Product aggregate) { }
        public void Delete(Product aggregate) { }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Sales.Domain;

namespace Sales.Application.Tests;

public sealed class CancelExpiredPendingOrdersHandlerTests
{
    [Fact]
    public async Task Handler_queries_expired_cancellable_orders_with_batch_size()
    {
        var currentUtc = DateTimeOffset.Parse("2026-07-15T10:00:00Z");
        var order = CreateDraftOrder();
        var repository = new FakeOrderRepository([order.Id], new Dictionary<Guid, Order> { [order.Id] = order });
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(new CancelExpiredPendingOrders(currentUtc, 30, 25), CancellationToken.None);

        Assert.Equal(currentUtc.AddMinutes(-30), repository.ExpirationCutoff);
        Assert.Equal(25, repository.BatchSize);
        Assert.Equal(1, result.ScannedOrderCount);
        Assert.Equal(1, result.CancelledOrderCount);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handler_clamps_batch_size()
    {
        var repository = new FakeOrderRepository([], new Dictionary<Guid, Order>());
        var handler = CreateHandler(repository, new FakeUnitOfWork());

        await handler.Handle(new CancelExpiredPendingOrders(DateTimeOffset.UtcNow, 30, 5_000), CancellationToken.None);

        Assert.Equal(1_000, repository.BatchSize);
    }

    [Fact]
    public async Task Order_changed_before_processing_is_skipped()
    {
        var order = CreateConfirmedOrder();
        var repository = new FakeOrderRepository([order.Id], new Dictionary<Guid, Order> { [order.Id] = order });
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.Handle(new CancelExpiredPendingOrders(DateTimeOffset.UtcNow, 30, 10), CancellationToken.None);

        Assert.Equal(1, result.ScannedOrderCount);
        Assert.Equal(0, result.CancelledOrderCount);
        Assert.Equal(1, result.SkippedOrderCount);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task One_order_failure_does_not_corrupt_result_for_other_orders()
    {
        var firstOrder = CreatePendingInventoryOrder();
        var failedOrder = CreatePendingInventoryOrder();
        var lastOrder = CreatePendingInventoryOrder();
        var orderIds = new[] { firstOrder.Id, failedOrder.Id, lastOrder.Id };
        var orders = new Dictionary<Guid, Order>
        {
            [firstOrder.Id] = firstOrder,
            [failedOrder.Id] = failedOrder,
            [lastOrder.Id] = lastOrder
        };
        var repository = new FakeOrderRepository(orderIds, orders);
        var unitOfWork = new FakeUnitOfWork { ThrowOnSaveNumber = 2 };
        var handler = CreateHandler(repository, unitOfWork);
        var currentUtc = orderIds
            .Select(orderId => orders[orderId].UpdatedAt)
            .Max()
            .AddMinutes(31);

        var result = await handler.Handle(new CancelExpiredPendingOrders(currentUtc, 30, 10), CancellationToken.None);

        Assert.Equal(3, result.ScannedOrderCount);
        Assert.Equal(2, result.CancelledOrderCount);
        Assert.Equal(0, result.SkippedOrderCount);
        Assert.Equal(1, result.FailedOrderCount);
        Assert.Equal(3, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Cancellation_token_is_passed_to_dependencies()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var order = CreatePendingInventoryOrder();
        var repository = new FakeOrderRepository([order.Id], new Dictionary<Guid, Order> { [order.Id] = order });
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);
        var currentUtc = order.UpdatedAt.AddMinutes(31);

        await handler.Handle(new CancelExpiredPendingOrders(currentUtc, 30, 10), cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, repository.LastFindCancellationToken);
        Assert.Equal(cancellationTokenSource.Token, repository.LastGetCancellationToken);
        Assert.Equal(cancellationTokenSource.Token, unitOfWork.LastCancellationToken);
    }

    private static CancelExpiredPendingOrdersHandler CreateHandler(
        FakeOrderRepository repository,
        FakeUnitOfWork unitOfWork)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOrderRepository>(repository);
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        var provider = services.BuildServiceProvider();
        return new CancelExpiredPendingOrdersHandler(
            repository,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<CancelExpiredPendingOrdersHandler>.Instance);
    }

    private static Order CreatePendingInventoryOrder()
    {
        var order = CreateDraftOrder();
        order.RequestConfirmation();
        order.ClearDomainEvents();
        return order;
    }

    private static Order CreateConfirmedOrder()
    {
        var order = CreatePendingInventoryOrder();
        order.MarkReserved();
        order.ClearDomainEvents();
        return order;
    }

    private static Order CreateDraftOrder()
    {
        var product = Product.Create("sku", "Product", 100);
        return Order.Create(
            CustomerSnapshot.Create(Guid.NewGuid(), "A", "0901234567"),
            [new(ProductSnapshot.Create(product.Id, product.Sku, product.Name, product.Price, true), 1, 0)]);
    }

    private sealed class FakeOrderRepository(
        IReadOnlyCollection<Guid> expiredCancellableOrderIds,
        IReadOnlyDictionary<Guid, Order> orders) : IOrderRepository
    {
        public DateTimeOffset? ExpirationCutoff { get; private set; }
        public int? BatchSize { get; private set; }
        public CancellationToken LastFindCancellationToken { get; private set; }
        public CancellationToken LastGetCancellationToken { get; private set; }

        public Task<IReadOnlyCollection<Guid>> FindExpiredCancellableOrderIdsAsync(
            DateTimeOffset orderUpdatedBefore,
            int batchSize,
            CancellationToken cancellationToken = default)
        {
            ExpirationCutoff = orderUpdatedBefore;
            BatchSize = batchSize;
            LastFindCancellationToken = cancellationToken;
            return Task.FromResult(expiredCancellableOrderIds);
        }

        public Task<Order?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            LastGetCancellationToken = cancellationToken;
            return Task.FromResult(orders.GetValueOrDefault(id));
        }

        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(orders.GetValueOrDefault(id));
        }

        public Task<IReadOnlyList<Order>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Order>>([]);
        }

        public Task AddAsync(Order aggregate, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Update(Order aggregate) { }

        public void Delete(Order aggregate) { }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public int? ThrowOnSaveNumber { get; init; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            LastCancellationToken = cancellationToken;
            if (ThrowOnSaveNumber == SaveCount)
            {
                throw new InvalidOperationException("save failed");
            }

            return Task.FromResult(1);
        }
    }
}

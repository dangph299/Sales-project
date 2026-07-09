using Microsoft.Extensions.Logging;
using Sales.Application;
using Sales.Domain;

namespace Sales.Application.Tests;

public sealed class ConfirmOrderHandlerTests
{
    [Fact]
    public async Task Confirm_moves_order_to_pending_inventory_and_logs_start_and_completion()
    {
        var order = CreateOrder();
        var logger = new RecordingLogger<ConfirmOrderHandler>();
        var handler = new ConfirmOrderHandler(new FakeOrderRepository(order), new FakeUnitOfWork(), logger);

        var dto = await handler.Handle(new ConfirmOrder(order.Id, order.Version), CancellationToken.None);

        Assert.Equal("PendingInventory", dto.Status);
        Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Warning);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("ConfirmOrder started"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("ConfirmOrder completed"));
    }

    [Fact]
    public async Task Confirm_does_not_swallow_the_exception_when_the_order_is_missing()
    {
        // ConfirmOrderHandler itself no longer catches/logs - ErrorLoggingBehavior owns the single
        // Error log for every command, so the handler must let the exception propagate untouched.
        var logger = new RecordingLogger<ConfirmOrderHandler>();
        var handler = new ConfirmOrderHandler(new FakeOrderRepository(null), new FakeUnitOfWork(), logger);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new ConfirmOrder(Guid.NewGuid(), 1), CancellationToken.None));

        Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Warning);
    }

    private static Order CreateOrder()
    {
        var product = Product.Create("sku", "Product", 100);
        return Order.Create(
            CustomerSnapshot.Create(Guid.NewGuid(), "A", "0901234567"),
            [new(ProductSnapshot.Create(product.Id, product.Sku, product.Name, product.Price, true), 1, 0)]);
    }

    private sealed class FakeOrderRepository(Order? order) : IOrderRepository
    {
        public Task<Order?> GetWithLinesAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(order);
        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(order);
        public Task<IReadOnlyList<Order>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Order>>([]);
        public Task AddAsync(Order aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Order aggregate) { }
        public void Delete(Order aggregate) { }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}

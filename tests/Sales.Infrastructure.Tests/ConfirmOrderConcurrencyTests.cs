using BuildingBlocks.Contracts;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Common.Interfaces;
using Sales.Domain;

namespace Sales.Infrastructure.Tests;

[Trait("Category", "Reliability")]
[Collection("SalesReliabilityPostgres")]
public sealed class ConfirmOrderConcurrencyTests
{
    private readonly PostgresReliabilityFixture _fixture;

    public ConfirmOrderConcurrencyTests(PostgresReliabilityFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Two_confirms_with_the_same_ETag_let_exactly_one_reach_inventory()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        var executionContext = new TestExecutionContext();

        await using (var setup = new SalesDbContext(options, executionContext))
            await setup.Database.MigrateAsync();

        var customer = CustomerSnapshot.Create(Guid.NewGuid(), "Nguyen Van A", "0901234567");
        var product = ProductTestFactory.CreatePublishedProduct($"sku-{Guid.NewGuid():N}", "Keyboard", 100_000);
        var snapshot = ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, product.IsActive);
        var order = Order.Create(customer, [new(snapshot, 1, 0m)]);
        var orderId = order.Id;

        await using (var seed = new SalesDbContext(options, executionContext))
        {
            seed.Orders.Add(order);
            await seed.SaveChangesAsync();
        }

        // Two clients both fetched the order beforehand, so both hold the same ETag (Version == 1).
        await using var clientA = new SalesDbContext(options, executionContext);
        await using var clientB = new SalesDbContext(options, executionContext);

        var orderA = await clientA.Orders.Include(x => x.Lines).SingleAsync(x => x.Id == orderId);
        var orderB = await clientB.Orders.Include(x => x.Lines).SingleAsync(x => x.Id == orderId);
        Assert.Equal(1, orderA.Version);
        Assert.Equal(orderA.Version, orderB.Version);

        orderA.RequestConfirmation();
        orderB.RequestConfirmation();

        // Client A's POST /confirm with If-Match: "1" lands first and its outbox row (the call to Inventory) commits.
        await clientA.SaveChangesAsync();

        // Client B replays the same stale If-Match: "1"; the row already moved on, so this is what the
        // API exception handling turns into a 409 concurrency_conflict response.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => clientB.SaveChangesAsync());

        await using var verify = new SalesDbContext(options, executionContext);
        var persisted = await verify.Orders.SingleAsync(x => x.Id == orderId);
        Assert.Equal(OrderStatus.PendingInventory, persisted.Status);
        Assert.Equal(2, persisted.Version);

        var inventoryCalls = await verify.OutboxMessages
            .Where(x => x.Topic == KafkaTopics.OrderConfirmationRequested && x.Payload.Contains(orderId.ToString()))
            .ToListAsync();
        Assert.Single(inventoryCalls);
    }

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string Actor => "integration-test";
        public Guid CorrelationId => Guid.NewGuid();
    }
}

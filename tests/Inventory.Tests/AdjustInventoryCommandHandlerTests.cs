using Inventory.Application;
using Inventory.Domain;

namespace Inventory.Tests;

public sealed class AdjustInventoryCommandHandlerTests
{
    [Fact]
    public async Task Adjusting_an_existing_item_applies_the_delta_and_enqueues_an_audit_event()
    {
        var productId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, "sku", 10);
        var inventory = new FakeInventoryRepository(item);
        var outbox = new FakeOutbox();
        var handler = new AdjustInventoryCommandHandler(inventory, outbox);

        var snapshot = await handler.Handle(new AdjustInventoryCommand(productId, "sku", 5, "tester"), CancellationToken.None);

        Assert.Equal(15, snapshot.Available);
        Assert.Equal(1, outbox.InventoryAdjustedCount);
        Assert.False(inventory.AddCalled);
    }

    [Fact]
    public async Task Adjusting_a_missing_product_creates_a_new_item()
    {
        var productId = Guid.NewGuid();
        var inventory = new FakeInventoryRepository(existing: null);
        var outbox = new FakeOutbox();
        var handler = new AdjustInventoryCommandHandler(inventory, outbox);

        var snapshot = await handler.Handle(new AdjustInventoryCommand(productId, "sku", 3, "tester"), CancellationToken.None);

        Assert.Equal(3, snapshot.Available);
        Assert.True(inventory.AddCalled);
    }

    private sealed class FakeInventoryRepository(InventoryItem? existing) : IInventoryRepository
    {
        public bool AddCalled { get; private set; }

        public Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(existing);
        }

        public Task<IReadOnlyCollection<InventoryItem>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void Add(InventoryItem item)
        {
            AddCalled = true;
        }
    }

    private sealed class FakeOutbox : IInventoryEventOutbox
    {
        public int InventoryAdjustedCount { get; private set; }

        public void EnqueueInventoryAdjusted(Guid productId, long version, int quantityDelta, int available, string actor)
        {
            InventoryAdjustedCount++;
        }

        public void EnqueueStockReserved(Guid orderId, long orderVersion, Guid correlationId, Guid causationId)
        {
            throw new NotSupportedException();
        }

        public void EnqueueStockRejected(Guid orderId, long orderVersion, string reason, Guid correlationId, Guid causationId)
        {
            throw new NotSupportedException();
        }

        public void EnqueueStockReleased(Guid orderId, long orderVersion, Guid correlationId, Guid causationId)
        {
            throw new NotSupportedException();
        }
    }
}

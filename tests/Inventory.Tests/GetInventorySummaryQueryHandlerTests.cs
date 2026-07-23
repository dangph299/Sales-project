using Inventory.Application.Features.InventoryItems.DTOs;
using Inventory.Application.Features.InventoryItems.Interfaces;
using Inventory.Application.Features.InventoryItems.Queries;

namespace Inventory.Tests;

public sealed class GetInventorySummaryQueryHandlerTests
{
    [Fact]
    public async Task Handle_delegates_filter_to_read_service_and_returns_its_result()
    {
        var expected = new InventorySummary(3, 9, 1, 1, 1, 5);
        var read = new FakeInventoryItemReadService(expected);
        var handler = new GetInventorySummaryQueryHandler(read);

        var result = await handler.Handle(new GetInventorySummaryQuery(new InventorySummaryFilter(5)), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.NotNull(read.ReceivedFilter);
        Assert.Equal(5, read.ReceivedFilter!.LowStockThreshold);
    }

    private sealed class FakeInventoryItemReadService(InventorySummary summary) : IInventoryItemReadService
    {
        public InventorySummaryFilter? ReceivedFilter { get; private set; }

        public Task<InventorySnapshot?> GetAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<InventorySummary> GetSummaryAsync(InventorySummaryFilter filter, CancellationToken cancellationToken = default)
        {
            ReceivedFilter = filter;
            return Task.FromResult(summary);
        }
    }
}

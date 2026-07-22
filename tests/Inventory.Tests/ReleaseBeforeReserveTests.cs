using BuildingBlocks.Contracts;
using Inventory.Application;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Features.Reservations.Commands;
using Inventory.Domain;

namespace Inventory.Tests;

/// <summary>
/// Covers the release-before-reserve out-of-order scenario (H2): a cancellation/undo release is
/// processed before the reservation it cancels, then the delayed reserve arrives. The release must
/// leave a version-carrying tombstone so the stale reserve cannot silently hold stock.
/// </summary>
public sealed class ReleaseBeforeReserveTests
{
    [Fact]
    public async Task Release_without_reservation_records_a_released_tombstone_with_the_order_version()
    {
        var orderId = Guid.NewGuid();
        var reservations = new FakeReservationRepository();
        var handler = new ReleaseStockCommandHandler(
            new FakeInventoryRepository([]),
            reservations,
            new FakeOutbox());

        var result = await handler.Handle(
            new ReleaseStockCommand(Guid.NewGuid(), orderId, 2, Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal("ReleasedBeforeReserve", result);
        var tombstone = Assert.Single(reservations.Added);
        Assert.Equal(ReservationStatus.Released, tombstone.Status);
        Assert.Equal(2, tombstone.LastOrderVersion);
        Assert.Empty(tombstone.Lines);
    }

    [Fact]
    public async Task Delayed_older_reserve_after_release_tombstone_does_not_hold_stock()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, "sku", 10);
        var reservations = new FakeReservationRepository();

        // Release (order version 2) processed first, before any reservation exists.
        var releaseHandler = new ReleaseStockCommandHandler(
            new FakeInventoryRepository([item]),
            reservations,
            new FakeOutbox());
        await releaseHandler.Handle(
            new ReleaseStockCommand(Guid.NewGuid(), orderId, 2, Guid.NewGuid()),
            CancellationToken.None);

        // Delayed reserve carrying the older order version 1 now arrives.
        var reserveOutbox = new FakeOutbox();
        var reserveHandler = new ReserveStockCommandHandler(
            new FakeInventoryRepository([item]),
            reservations,
            reserveOutbox,
            new FakeMetrics());

        var result = await reserveHandler.Handle(
            new ReserveStockCommand(Guid.NewGuid(), orderId, 1, Guid.NewGuid(), [new OrderLineIntegration(productId, "sku", 3)]),
            CancellationToken.None);

        Assert.Equal(ErrorCodes.StaleReservation, result);
        Assert.Equal(10, item.Available);
        Assert.Equal(0, item.Reserved);
        Assert.Equal(0, reserveOutbox.StockReservedCount);
    }

    [Fact]
    public async Task Newer_reserve_after_release_tombstone_reactivates_and_holds_stock()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, "sku", 10);
        var reservations = new FakeReservationRepository();

        var releaseHandler = new ReleaseStockCommandHandler(
            new FakeInventoryRepository([item]),
            reservations,
            new FakeOutbox());
        await releaseHandler.Handle(
            new ReleaseStockCommand(Guid.NewGuid(), orderId, 2, Guid.NewGuid()),
            CancellationToken.None);

        // A brand-new confirmation for the same order (version 3) should still be able to reserve.
        var reserveOutbox = new FakeOutbox();
        var reserveHandler = new ReserveStockCommandHandler(
            new FakeInventoryRepository([item]),
            reservations,
            reserveOutbox,
            new FakeMetrics());

        var result = await reserveHandler.Handle(
            new ReserveStockCommand(Guid.NewGuid(), orderId, 3, Guid.NewGuid(), [new OrderLineIntegration(productId, "sku", 3)]),
            CancellationToken.None);

        Assert.Equal("Reserved", result);
        Assert.Equal(7, item.Available);
        Assert.Equal(3, item.Reserved);
        Assert.Equal(1, reserveOutbox.StockReservedCount);
        var tombstone = Assert.Single(reservations.Added);
        Assert.Equal(ReservationStatus.Active, tombstone.Status);
        Assert.Equal(3, tombstone.LastOrderVersion);
    }

    private sealed class FakeReservationRepository : IReservationRepository
    {
        public List<Reservation> Added { get; } = [];

        public Task<Reservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Added.SingleOrDefault(x => x.OrderId == orderId));
        }

        public void Add(Reservation reservation)
        {
            Added.Add(reservation);
        }
    }

    private sealed class FakeInventoryRepository(IReadOnlyCollection<InventoryItem> items) : IInventoryRepository
    {
        public Task<InventoryItem?> GetByProductVariantIdAsync(Guid productVariantId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(items.SingleOrDefault(x => x.ProductVariantId == productVariantId));
        }

        public Task<IReadOnlyCollection<InventoryItem>> GetByProductVariantIdsAsync(IEnumerable<Guid> productVariantIds, CancellationToken cancellationToken = default)
        {
            var ids = productVariantIds.ToHashSet();
            return Task.FromResult((IReadOnlyCollection<InventoryItem>)items.Where(x => ids.Contains(x.ProductVariantId)).ToArray());
        }

        public void Add(InventoryItem item)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeOutbox : IInventoryEventOutbox
    {
        public int StockReservedCount { get; private set; }

        public void EnqueueInventoryAdjusted(Guid productId, long version, int quantityDelta, int available, string actor)
        {
            throw new NotSupportedException();
        }

        public void EnqueueStockReserved(Guid orderId, long orderVersion, Guid correlationId, Guid causationId)
        {
            StockReservedCount++;
        }

        public void EnqueueStockRejected(Guid orderId, long orderVersion, string reason, Guid correlationId, Guid causationId)
        {
        }

        public void EnqueueStockReleased(Guid orderId, long orderVersion, Guid correlationId, Guid causationId)
        {
        }
    }

    private sealed class FakeMetrics : IInventoryMetrics
    {
        public void RecordInboxDuplicate() { }

        public void RecordInboxProcessed() { }

        public void RecordReservationRejected() { }

        public void RecordReservationReserved() { }
    }
}

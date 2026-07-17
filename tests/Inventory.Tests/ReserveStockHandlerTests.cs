using BuildingBlocks.Contracts;
using Inventory.Application;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Features.Reservations.Commands;
using Inventory.Domain;

namespace Inventory.Tests;

public sealed class ReserveStockHandlerTests
{
    [Fact]
    public async Task Newer_confirmation_replaces_active_reservation_lines_and_stock()
    {
        var oldProductId = Guid.NewGuid();
        var newProductId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var oldItem = InventoryItem.Create(oldProductId, "old", 10);
        oldItem.Reserve(1);
        var newItem = InventoryItem.Create(newProductId, "new", 5);
        var reservation = Reservation.Create(orderId, 1, [new ReservationRequestLine(oldProductId, "old", 1)]);
        var inventory = new FakeInventoryRepository([oldItem, newItem]);
        var reservations = new FakeReservationRepository(reservation);
        var outbox = new FakeOutbox();
        var handler = new ReserveStockCommandHandler(inventory, reservations, outbox, new FakeMetrics());

        var result = await handler.Handle(new ReserveStockCommand(
            Guid.NewGuid(),
            orderId,
            3,
            Guid.NewGuid(),
            [new OrderLineIntegration(newProductId, "new", 2)]), CancellationToken.None);

        Assert.Equal("ReservedAcknowledged", result);
        Assert.Equal(10, oldItem.Available);
        Assert.Equal(0, oldItem.Reserved);
        Assert.Equal(3, newItem.Available);
        Assert.Equal(2, newItem.Reserved);
        Assert.Equal(3, reservation.LastOrderVersion);
        var line = Assert.Single(reservation.Lines);
        Assert.Equal(newProductId, line.ProductId);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(1, outbox.StockReservedCount);
    }

    [Fact]
    public async Task Stale_confirmation_against_released_reservation_does_not_reserve_stock()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, "sku", 10);
        var reservation = Reservation.Create(orderId, 5, [new ReservationRequestLine(productId, "sku", 2)]);
        reservation.Release(6);
        var inventory = new FakeInventoryRepository([item]);
        var reservations = new FakeReservationRepository(reservation);
        var outbox = new FakeOutbox();
        var handler = new ReserveStockCommandHandler(inventory, reservations, outbox, new FakeMetrics());

        var result = await handler.Handle(new ReserveStockCommand(
            Guid.NewGuid(),
            orderId,
            3,
            Guid.NewGuid(),
            [new OrderLineIntegration(productId, "sku", 2)]), CancellationToken.None);

        Assert.Equal(ErrorCodes.StaleReservation, result);
        Assert.Equal(10, item.Available);
        Assert.Equal(0, item.Reserved);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(0, outbox.StockReservedCount);
    }

    [Fact]
    public async Task Newer_confirmation_reactivates_a_released_reservation_and_reserves_stock()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, "sku", 10);
        var reservation = Reservation.Create(orderId, 5, [new ReservationRequestLine(productId, "sku", 2)]);
        reservation.Release(6);
        var inventory = new FakeInventoryRepository([item]);
        var reservations = new FakeReservationRepository(reservation);
        var outbox = new FakeOutbox();
        var handler = new ReserveStockCommandHandler(inventory, reservations, outbox, new FakeMetrics());

        var result = await handler.Handle(new ReserveStockCommand(
            Guid.NewGuid(),
            orderId,
            7,
            Guid.NewGuid(),
            [new OrderLineIntegration(productId, "sku", 2)]), CancellationToken.None);

        Assert.Equal("Reserved", result);
        Assert.Equal(8, item.Available);
        Assert.Equal(2, item.Reserved);
        Assert.Equal(ReservationStatus.Active, reservation.Status);
        Assert.Equal(1, outbox.StockReservedCount);
    }

    [Fact]
    public async Task Stale_confirmation_against_active_reservation_is_ignored()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, "sku", 10);
        item.Reserve(2);
        var reservation = Reservation.Create(orderId, 5, [new ReservationRequestLine(productId, "sku", 2)]);
        var inventory = new FakeInventoryRepository([item]);
        var reservations = new FakeReservationRepository(reservation);
        var outbox = new FakeOutbox();
        var handler = new ReserveStockCommandHandler(inventory, reservations, outbox, new FakeMetrics());

        var result = await handler.Handle(new ReserveStockCommand(
            Guid.NewGuid(),
            orderId,
            5,
            Guid.NewGuid(),
            [new OrderLineIntegration(productId, "sku", 2)]), CancellationToken.None);

        Assert.Equal("AlreadyReserved", result);
        Assert.Equal(8, item.Available);
        Assert.Equal(2, item.Reserved);
        Assert.Equal(0, outbox.StockReservedCount);
    }

    private sealed class FakeInventoryRepository(IReadOnlyCollection<InventoryItem> items) : IInventoryRepository
    {
        public Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(items.SingleOrDefault(x => x.ProductId == productId));
        }

        public Task<IReadOnlyCollection<InventoryItem>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default)
        {
            var ids = productIds.ToHashSet();
            return Task.FromResult((IReadOnlyCollection<InventoryItem>)items.Where(x => ids.Contains(x.ProductId)).ToArray());
        }

        public void Add(InventoryItem item)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeReservationRepository(Reservation reservation) : IReservationRepository
    {
        public Task<Reservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Reservation?>(reservation.OrderId == orderId ? reservation : null);
        }

        public void Add(Reservation value)
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
            throw new NotSupportedException();
        }

        public void EnqueueStockReleased(Guid orderId, long orderVersion, Guid correlationId, Guid causationId)
        {
            throw new NotSupportedException();
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

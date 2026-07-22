using BuildingBlocks.Contracts;
using Inventory.Application;
using Inventory.Application.Common.Interfaces;
using Inventory.Application.Features.Reservations.Commands;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Inventory.Infrastructure.Tests;

/// <summary>
/// End-to-end reliability test for the release-before-reserve out-of-order gap (H2) against a real
/// PostgreSQL: a release processed before its reservation persists a version-carrying tombstone, and a
/// delayed, older reserve is then rejected as stale without holding stock for the cancelled order.
/// </summary>
[Trait("Category", "Reliability")]
[Collection("InventoryReliabilityPostgres")]
public sealed class ReleaseBeforeReservePostgresTests
{
    private readonly InventoryPostgresReliabilityFixture _fixture;

    public ReleaseBeforeReservePostgresTests(InventoryPostgresReliabilityFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Release_before_reserve_tombstone_blocks_a_stale_reserve()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await ResetDatabaseAsync();

        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await using (var db = NewContext())
        {
            new InventoryRepository(db).Add(InventoryItem.Create(productId, "sku-1", 10));
            await db.SaveChangesAsync();
        }

        // Release (order version 2) is processed before any reservation exists.
        await using (var db = NewContext())
        {
            var releaseHandler = new ReleaseStockCommandHandler(
                new InventoryRepository(db), new ReservationRepository(db), new InventoryEventOutbox(db));
            var releaseResult = await releaseHandler.Handle(
                new ReleaseStockCommand(Guid.NewGuid(), orderId, 2, Guid.NewGuid()), CancellationToken.None);
            await db.SaveChangesAsync();
            Assert.Equal("ReleasedBeforeReserve", releaseResult);
        }

        // The delayed reserve carries the older order version 1 and must be rejected as stale.
        await using (var db = NewContext())
        {
            var reserveHandler = new ReserveStockCommandHandler(
                new InventoryRepository(db), new ReservationRepository(db), new InventoryEventOutbox(db), new NoopMetrics());
            var reserveResult = await reserveHandler.Handle(
                new ReserveStockCommand(Guid.NewGuid(), orderId, 1, Guid.NewGuid(), [new OrderLineIntegration(productId, "sku-1", 3)]),
                CancellationToken.None);
            await db.SaveChangesAsync();
            Assert.Equal(ErrorCodes.StaleReservation, reserveResult);
        }

        await using (var verify = NewContext())
        {
            var item = await verify.Items.AsNoTracking().SingleAsync(x => x.ProductVariantId == productId);
            Assert.Equal(10, item.Available);
            Assert.Equal(0, item.Reserved);

            var reservation = await verify.Reservations.AsNoTracking().SingleAsync(x => x.OrderId == orderId);
            Assert.Equal(ReservationStatus.Released, reservation.Status);
        }
    }

    private async Task ResetDatabaseAsync()
    {
        await using var db = NewContext();
        await db.Database.MigrateAsync();
        await db.Reservations.ExecuteDeleteAsync();
        await db.Items.ExecuteDeleteAsync();
        await db.Outbox.ExecuteDeleteAsync();
    }

    private InventoryDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new InventoryDbContext(options);
    }

    private sealed class NoopMetrics : IInventoryMetrics
    {
        public void RecordInboxDuplicate() { }

        public void RecordInboxProcessed() { }

        public void RecordReservationRejected() { }

        public void RecordReservationReserved() { }
    }
}

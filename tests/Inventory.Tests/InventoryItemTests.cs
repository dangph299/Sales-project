using Inventory.Domain;

namespace Inventory.Tests;

public sealed class InventoryItemTests
{
    [Fact]
    public void Reserve_and_release_never_lose_stock()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "SKU-01", 10);
        item.Reserve(4);
        item.Release(4);
        Assert.Equal(10, item.Available);
        Assert.Equal(0, item.Reserved);
    }

    [Fact]
    public void Cannot_reserve_more_than_available()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "SKU-01", 2);
        Assert.Throws<InvalidOperationException>(() => item.Reserve(3));
    }

    [Fact]
    public void Reservation_cannot_be_released_twice()
    {
        var reservation = Reservation.Create(Guid.NewGuid(), 1, [new ReservationRequestLine(Guid.NewGuid(), "sku", 1)]);
        Assert.True(reservation.Release(2));
        Assert.Throws<InvalidOperationException>(() => reservation.Release(3));
        Assert.Equal(ReservationStatus.Released, reservation.Status);
    }

    [Fact]
    public void Released_reservation_can_be_reactivated_for_repeat_confirmation()
    {
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var reservation = Reservation.Create(Guid.NewGuid(), 1, [new ReservationRequestLine(firstProductId, "sku-1", 1)]);

        reservation.Release(2);
        Assert.True(reservation.Reactivate(3, [new ReservationRequestLine(secondProductId, "sku-2", 2)]));

        Assert.Equal(ReservationStatus.Active, reservation.Status);
        var line = Assert.Single(reservation.Lines);
        Assert.Equal(secondProductId, line.ProductId);
        Assert.Equal(2, line.Quantity);
    }

    [Fact]
    public void Reservation_ignores_stale_release_after_newer_confirmation()
    {
        var productId = Guid.NewGuid();
        var reservation = Reservation.Create(Guid.NewGuid(), 7, [new ReservationRequestLine(productId, "sku", 1)]);

        Assert.True(reservation.ReplaceActive(10, [new ReservationRequestLine(productId, "sku", 1)]));

        Assert.False(reservation.Release(9));
        Assert.Equal(ReservationStatus.Active, reservation.Status);
        Assert.Equal(10, reservation.LastOrderVersion);
    }

    [Fact]
    public void IsStale_reflects_last_applied_order_version()
    {
        var reservation = Reservation.Create(Guid.NewGuid(), 5, [new ReservationRequestLine(Guid.NewGuid(), "sku", 1)]);

        Assert.True(reservation.IsStale(5));
        Assert.True(reservation.IsStale(4));
        Assert.False(reservation.IsStale(6));
    }

    [Fact]
    public void Create_stores_the_identifier_as_the_product_variant_id()
    {
        var productVariantId = Guid.NewGuid();

        var item = InventoryItem.Create(productVariantId, "sku", 10);

        Assert.Equal(productVariantId, item.ProductVariantId);
    }

    [Fact]
    public void Create_sets_audit_timestamps()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "sku", 10);

        Assert.NotEqual(default, item.CreatedAt);
        Assert.Equal(item.CreatedAt, item.UpdatedAt);
    }

    [Fact]
    public void Zero_adjustment_does_not_touch_inventory_item()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "sku", 10);
        var version = item.Version;
        var updatedAt = item.UpdatedAt;

        item.Adjust(0);

        Assert.Equal(version, item.Version);
        Assert.Equal(updatedAt, item.UpdatedAt);
    }

    [Fact]
    public void Reservation_status_changes_touch_reservation()
    {
        var reservation = Reservation.Create(Guid.NewGuid(), 1, [new ReservationRequestLine(Guid.NewGuid(), "sku", 1)]);
        var version = reservation.Version;

        Assert.True(reservation.Release(2));

        Assert.True(reservation.Version > version);
        Assert.NotEqual(default, reservation.UpdatedAt);
    }
}

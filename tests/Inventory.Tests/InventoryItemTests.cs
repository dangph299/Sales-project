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
        var reservation = Reservation.Create(Guid.NewGuid(), [new ReservationRequestLine(Guid.NewGuid(), "sku", 1)]);
        reservation.Release();
        Assert.Throws<InvalidOperationException>(reservation.Release);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
    }
}

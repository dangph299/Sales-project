namespace Sales.Domain.Tests;

public sealed class OrderTests
{
    [Fact]
    public void Create_snapshots_data_and_rounds_vnd_away_from_zero()
    {
        var customer = CustomerSnapshot.Create(Guid.NewGuid(), "Nguyen Van A", "090-123-4567");
        var product = ProductTestFactory.CreatePublishedProduct("sku-1", "Keyboard", 1001);
        var snapshot = ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, product.IsActive);

        var order = Order.Create(customer, [new(snapshot, 3, 12.5m)]);

        Assert.Equal("0901234567", order.CustomerPhone);
        Assert.Equal(2628m, order.Total.Amount);
        Assert.Equal(3, order.TotalQuantity);
        Assert.Equal(1, order.Version);
        Assert.Contains(order.GetDomainEvents(), x => x is OrderCreatedDomainEvent);
    }

    [Fact]
    public void Confirm_and_undo_follows_inventory_state_machine()
    {
        var order = CreateOrder();
        order.RequestConfirmation();
        Assert.Contains(order.GetDomainEvents(), x => x is OrderConfirmationRequestedDomainEvent);
        order.MarkReserved();
        order.UndoConfirmed();
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Contains(order.GetDomainEvents(), x => x is OrderUndoComfirmedDomainEvent);
    }

    [Fact]
    public void Confirmed_sell_through_discontinued_order_cannot_be_undone()
    {
        var product = ProductTestFactory.CreatePublishedProduct("sku", "Product", 100);
        var variant = ProductTestFactory.PrimaryVariant(product);
        var snapshot = ProductSnapshot.Create(
            product.Id,
            variant.Id,
            product.ProductCode,
            product.Name,
            variant.Sku,
            "BLK",
            "Black",
            "M",
            variant.Price,
            isActive: true,
            isSellThroughDiscontinued: true);
        var order = Order.Create(
            CustomerSnapshot.Create(Guid.NewGuid(), "A", "0901234567"),
            [new(snapshot, 1, 0)]);
        order.RequestConfirmation();
        order.MarkReserved();

        Assert.Throws<DomainException>(order.UndoConfirmed);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Expired_pending_inventory_order_is_cancelled_and_raises_release_event()
    {
        var order = CreateOrder();
        order.RequestConfirmation();
        order.ClearDomainEvents();

        var wasOrderCancelled = order.CancelDueToExpiration(order.UpdatedAt.AddSeconds(1));

        Assert.True(wasOrderCancelled);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Single(order.GetDomainEvents(), x => x is OrderUndoComfirmedDomainEvent);
    }

    [Fact]
    public void Draft_order_is_cancelled_by_expiration_without_release_event()
    {
        var order = CreateOrder();
        order.ClearDomainEvents();

        var wasOrderCancelled = order.CancelDueToExpiration(order.UpdatedAt.AddSeconds(1));

        Assert.True(wasOrderCancelled);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Empty(order.GetDomainEvents());
    }

    [Fact]
    public void Inventory_rejected_order_is_cancelled_by_expiration_without_release_event()
    {
        var order = CreateOrder();
        order.RequestConfirmation();
        order.RejectInventory("out of stock");
        order.ClearDomainEvents();

        var wasOrderCancelled = order.CancelDueToExpiration(order.UpdatedAt.AddSeconds(1));

        Assert.True(wasOrderCancelled);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Empty(order.GetDomainEvents());
    }

    [Fact]
    public void Order_not_past_expiration_is_not_cancelled()
    {
        var order = CreateOrder();
        order.RequestConfirmation();
        order.ClearDomainEvents();

        var wasOrderCancelled = order.CancelDueToExpiration(order.UpdatedAt.AddSeconds(-1));

        Assert.False(wasOrderCancelled);
        Assert.Equal(OrderStatus.PendingInventory, order.Status);
        Assert.Empty(order.GetDomainEvents());
    }

    [Fact]
    public void Expiration_cancellation_is_idempotent()
    {
        var order = CreateOrder();
        order.RequestConfirmation();
        var expirationCutoff = order.UpdatedAt.AddSeconds(1);
        order.ClearDomainEvents();

        Assert.True(order.CancelDueToExpiration(expirationCutoff));
        Assert.False(order.CancelDueToExpiration(expirationCutoff));

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Single(order.GetDomainEvents(), x => x is OrderUndoComfirmedDomainEvent);
    }

    [Fact]
    public void Confirmed_order_is_not_cancelled_by_expiration()
    {
        var order = CreateOrder();
        order.RequestConfirmation();
        order.MarkReserved();
        order.ClearDomainEvents();

        var wasOrderCancelled = order.CancelDueToExpiration(order.UpdatedAt.AddSeconds(1));

        Assert.False(wasOrderCancelled);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Empty(order.GetDomainEvents());
    }

    [Fact]
    public void Replace_lines_increments_version_and_raises_domain_event()
    {
        var order = CreateOrder();
        var replacementProduct = ProductTestFactory.CreatePublishedProduct("new", "Replacement", 200);
        var replacement = ProductSnapshot.Create(replacementProduct.Id, replacementProduct.Sku, replacementProduct.Name, ProductTestFactory.PrimaryVariant(replacementProduct).Price, true);
        order.ClearDomainEvents();

        order.ReplaceLines([new(replacement, 2, 10)]);

        Assert.Equal(2, order.Version);
        Assert.Single(order.GetDomainEvents(), x => x is OrderLinesReplacedDomainEvent);
    }

    [Fact]
    public void Replace_lines_updates_existing_product_line_in_place()
    {
        var order = CreateOrder();
        var line = Assert.Single(order.Lines);
        var lineId = line.Id;
        var snapshot = ProductSnapshot.Create(line.ProductId, line.Sku, line.ProductName, line.UnitPrice, true);

        order.ReplaceLines([new(snapshot, 4, 25)]);

        var replaced = Assert.Single(order.Lines);
        Assert.Equal(lineId, replaced.Id);
        Assert.Equal(4, replaced.Quantity);
        Assert.Equal(25, replaced.DiscountPercent);
    }

    [Fact]
    public void Confirmed_order_cannot_be_edited()
    {
        var order = CreateOrder();
        order.RequestConfirmation();
        order.MarkReserved();
        var product = ProductTestFactory.CreatePublishedProduct("other", "Other", 10);
        var snapshot = ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, true);
        Assert.Throws<DomainException>(() => order.ReplaceLines([new(snapshot, 1, 0)]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantity_must_be_positive(int quantity)
    {
        var customer = CustomerSnapshot.Create(Guid.NewGuid(), "A", "0901234567");
        var product = ProductTestFactory.CreatePublishedProduct("sku", "Product", 100);
        var snapshot = ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, true);
        Assert.Throws<DomainException>(() => Order.Create(customer, [new(snapshot, quantity, 0)]));
    }

    private static Order CreateOrder()
    {
        var product = ProductTestFactory.CreatePublishedProduct("sku", "Product", 100);
        return Order.Create(
            CustomerSnapshot.Create(Guid.NewGuid(), "A", "0901234567"),
            [new(ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, true), 1, 0)]);
    }
}

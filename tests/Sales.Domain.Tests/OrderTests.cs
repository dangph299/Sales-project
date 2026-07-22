namespace Sales.Domain.Tests;

public sealed class OrderTests
{
    [Fact]
    public void Create_snapshots_data_and_rounds_vnd_away_from_zero()
    {
        var customer = OrderCustomerSnapshot.Create(Guid.NewGuid(), "Nguyen Van A", "090-123-4567", null, null);
        var product = ProductTestFactory.CreatePublishedProduct("sku-1", "Keyboard", 1001);
        var snapshot = ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, product.IsActive);

        var order = Order.Create(OrderTestFactory.NextOrderCode(), customer, [new(snapshot, 3, 12.5m)]);

        Assert.Equal("090-123-4567", order.CustomerPhone);
        Assert.Equal("0901234567", order.NormalizedCustomerPhone);
        Assert.Equal("7654321090", order.ReversedCustomerPhone);
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
        var order = Order.Create(OrderTestFactory.NextOrderCode(), OrderCustomerSnapshot.Create(Guid.NewGuid(), "A", "0901234567", null, null),
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
        var customer = OrderCustomerSnapshot.Create(Guid.NewGuid(), "A", "0901234567", null, null);
        var product = ProductTestFactory.CreatePublishedProduct("sku", "Product", 100);
        var snapshot = ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, true);
        Assert.Throws<DomainException>(() => Order.Create(OrderTestFactory.NextOrderCode(), customer, [new(snapshot, quantity, 0)]));
    }

    [Fact]
    public void Create_records_the_requested_customer_details_verbatim()
    {
        var customerId = Guid.NewGuid();
        var product = ProductTestFactory.CreatePublishedProduct("sku", "Product", 100);
        var orderCustomerSnapshot = OrderCustomerSnapshot.Create(
            customerId,
            "Nguyen Van B",
            "0912-345-678",
            "b@example.com",
            "12 Le Loi");

        var order = Order.Create(
            "ORD-0000123",
            orderCustomerSnapshot,
            [new(ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, true), 1, 0)]);

        Assert.Equal("ORD-0000123", order.OrderCode);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal("Nguyen Van B", order.CustomerName);
        Assert.Equal("0912-345-678", order.CustomerPhone);
        Assert.Equal("0912345678", order.NormalizedCustomerPhone);
        Assert.Equal("8765432190", order.ReversedCustomerPhone);
        Assert.Equal("b@example.com", order.CustomerEmail);
        Assert.Equal("12 Le Loi", order.CustomerAddress);
    }

    [Fact]
    public void UpdateCustomerSnapshot_rewrites_all_three_phone_values_together()
    {
        var order = CreateOrder();

        order.UpdateCustomerSnapshot(OrderCustomerSnapshot.Create(
            order.CustomerId,
            "Nguyen Van B",
            "091 234 5678",
            null,
            null));

        Assert.Equal("091 234 5678", order.CustomerPhone);
        Assert.Equal("0912345678", order.NormalizedCustomerPhone);
        Assert.Equal("8765432190", order.ReversedCustomerPhone);
        Assert.Equal(
            CustomerPhoneNormalizer.Reverse(order.NormalizedCustomerPhone),
            order.ReversedCustomerPhone);
    }

    [Fact]
    public void UpdateCustomerSnapshot_replaces_the_name_email_and_address()
    {
        var order = CreateOrder();

        order.UpdateCustomerSnapshot(OrderCustomerSnapshot.Create(
            order.CustomerId,
            "Nguyen Van B",
            "0901234567",
            "b@example.com",
            "34 Tran Hung Dao"));

        Assert.Equal("Nguyen Van B", order.CustomerName);
        Assert.Equal("b@example.com", order.CustomerEmail);
        Assert.Equal("34 Tran Hung Dao", order.CustomerAddress);
    }

    [Fact]
    public void UpdateCustomerSnapshot_keeps_the_customer_the_order_was_placed_for()
    {
        var order = CreateOrder();
        var originalCustomerId = order.CustomerId;
        var unrelatedCustomerId = Guid.NewGuid();

        // Even handed a snapshot naming a different customer, the order keeps its own link: only
        // the editable fields are applied.
        order.UpdateCustomerSnapshot(OrderCustomerSnapshot.Create(
            unrelatedCustomerId,
            "Nguyen Van B",
            "0912345678",
            null,
            null));

        Assert.Equal(originalCustomerId, order.CustomerId);
        Assert.NotEqual(unrelatedCustomerId, order.CustomerId);
    }

    [Fact]
    public void UpdateCustomerSnapshot_bumps_the_version_for_optimistic_concurrency()
    {
        var order = CreateOrder();
        var versionBeforeUpdate = order.Version;

        order.UpdateCustomerSnapshot(OrderCustomerSnapshot.Create(
            order.CustomerId,
            "Nguyen Van B",
            "0912345678",
            null,
            null));

        Assert.True(order.Version > versionBeforeUpdate);
    }

    [Fact]
    public void UpdateCustomerSnapshot_is_rejected_once_the_order_leaves_draft()
    {
        var order = CreateOrder();
        order.RequestConfirmation();

        Assert.Throws<DomainException>(() => order.UpdateCustomerSnapshot(OrderCustomerSnapshot.Create(
            order.CustomerId,
            "Nguyen Van B",
            "0912345678",
            null,
            null)));
    }

    [Fact]
    public void Several_orders_may_record_the_same_customer_phone()
    {
        var customerId = Guid.NewGuid();
        var product = ProductTestFactory.CreatePublishedProduct("sku", "Product", 100);
        var productSnapshot = ProductSnapshot.Create(
            product.Id,
            product.Sku,
            product.Name,
            ProductTestFactory.PrimaryVariant(product).Price,
            true);

        var firstOrder = Order.Create(
            OrderTestFactory.NextOrderCode(),
            OrderCustomerSnapshot.Create(customerId, "A", "0901234567", null, null),
            [new(productSnapshot, 1, 0)]);
        var secondOrder = Order.Create(
            OrderTestFactory.NextOrderCode(),
            OrderCustomerSnapshot.Create(customerId, "A", "0901234567", null, null),
            [new(productSnapshot, 1, 0)]);

        Assert.Equal(firstOrder.NormalizedCustomerPhone, secondOrder.NormalizedCustomerPhone);
        Assert.NotEqual(firstOrder.Id, secondOrder.Id);
        Assert.NotEqual(firstOrder.OrderCode, secondOrder.OrderCode);
    }

    private static Order CreateOrder()
    {
        var product = ProductTestFactory.CreatePublishedProduct("sku", "Product", 100);
        return Order.Create(OrderTestFactory.NextOrderCode(), OrderCustomerSnapshot.Create(Guid.NewGuid(), "A", "0901234567", null, null),
            [new(ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, true), 1, 0)]);
    }
}

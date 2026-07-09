using Sales.Domain;

namespace Sales.Domain.Tests;

public sealed class OrderTests
{
    [Fact]
    public void Create_snapshots_data_and_rounds_vnd_away_from_zero()
    {
        var customer = CustomerSnapshot.Create(Guid.NewGuid(), "Nguyen Van A", "090-123-4567");
        var product = Product.Create("sku-1", "Keyboard", 1001);
        var snapshot = ProductSnapshot.Create(product.Id, product.Sku, product.Name, product.Price, product.IsActive);

        var order = Order.Create(customer, [new(snapshot, 3, 12.5m)]);

        Assert.Equal("0901234567", order.CustomerPhone);
        Assert.Equal(2628m, order.Total.Amount);
        Assert.Equal(3, order.TotalQuantity);
        Assert.Equal(1, order.Version);
        Assert.Contains(order.GetDomainEvents(), x => x is OrderCreatedDomainEvent);
    }

    [Fact]
    public void Confirm_follows_inventory_state_machine()
    {
        var order = CreateOrder();
        order.RequestConfirmation();
        Assert.Contains(order.GetDomainEvents(), x => x is OrderConfirmationRequestedDomainEvent);
        order.MarkReserved();
        order.Cancel();
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Replace_lines_increments_version_and_raises_domain_event()
    {
        var order = CreateOrder();
        var replacementProduct = Product.Create("new", "Replacement", 200);
        var replacement = ProductSnapshot.Create(replacementProduct.Id, replacementProduct.Sku, replacementProduct.Name, replacementProduct.Price, true);
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
        var product = Product.Create("other", "Other", 10);
        var snapshot = ProductSnapshot.Create(product.Id, product.Sku, product.Name, product.Price, true);
        Assert.Throws<DomainException>(() => order.ReplaceLines([new(snapshot, 1, 0)]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantity_must_be_positive(int quantity)
    {
        var customer = CustomerSnapshot.Create(Guid.NewGuid(), "A", "0901234567");
        var product = Product.Create("sku", "Product", 100);
        var snapshot = ProductSnapshot.Create(product.Id, product.Sku, product.Name, product.Price, true);
        Assert.Throws<DomainException>(() => Order.Create(customer, [new(snapshot, quantity, 0)]));
    }

    private static Order CreateOrder()
    {
        var product = Product.Create("sku", "Product", 100);
        return Order.Create(
            CustomerSnapshot.Create(Guid.NewGuid(), "A", "0901234567"),
            [new(ProductSnapshot.Create(product.Id, product.Sku, product.Name, product.Price, true), 1, 0)]);
    }
}

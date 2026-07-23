using Sales.Domain;

namespace Sales.Domain.Tests;

public sealed class AuditTouchTests
{
    [Fact]
    public void Customer_email_or_address_update_touches_customer()
    {
        var customer = Customer.Create("CUS001", "Nguyen Van A", "0901234567", "a@example.com", "Old");
        var version = customer.Version;

        customer.Update(customer.Name, customer.Phone, "b@example.com", "Old");

        Assert.True(customer.Version > version);
    }

    [Fact]
    public void Customer_no_op_update_does_not_touch_customer()
    {
        var customer = Customer.Create("CUS002", "Nguyen Van A", "0901234567", "a@example.com", "Old");
        var version = customer.Version;
        var updatedAt = customer.UpdatedAt;

        customer.Update(" Nguyen Van A ", "0901234567", " a@example.com ", " Old ");

        Assert.Equal(version, customer.Version);
        Assert.Equal(updatedAt, customer.UpdatedAt);
    }

    [Fact]
    public void Category_no_op_update_does_not_touch_category()
    {
        var category = Category.Create("CAT001", "Shirts", "Everyday", null, 10);
        var version = category.Version;
        var updatedAt = category.UpdatedAt;

        category.Update(" Shirts ", " Everyday ", null, 10);

        Assert.Equal(version, category.Version);
        Assert.Equal(updatedAt, category.UpdatedAt);
    }

    [Fact]
    public void Product_variant_no_op_update_does_not_touch_product_or_variant()
    {
        var product = Product.Create("PRD001", "Shirt", null, Guid.NewGuid());
        var color = Color.Create(Guid.NewGuid(), "BLK", "Black", "#000000");
        var size = Size.Create(Guid.NewGuid(), "M", "Medium", 20);
        var variant = product.AddVariant(color, size, 100, EProductVariantStatus.Draft);
        var productVersion = product.Version;
        var variantVersion = variant.Version;

        product.UpdateVariant(variant.Id, color, size, 100, EProductVariantStatus.Draft);

        Assert.Equal(productVersion, product.Version);
        Assert.Equal(variantVersion, variant.Version);
    }

    [Fact]
    public void Order_no_op_customer_snapshot_update_does_not_touch_order()
    {
        var snapshot = OrderCustomerSnapshot.Create(Guid.NewGuid(), "Customer", "0901234567", "a@example.com", "Address");
        var product = ProductTestFactory.CreatePublishedProduct("SKU", "Keyboard", 100);
        var order = Order.Create(
            OrderTestFactory.NextOrderCode(),
            snapshot,
            [new(ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, true), 1, 0)]);
        var version = order.Version;
        var updatedAt = order.UpdatedAt;

        order.UpdateCustomerSnapshot(snapshot);

        Assert.Equal(version, order.Version);
        Assert.Equal(updatedAt, order.UpdatedAt);
    }

    [Fact]
    public void Order_no_op_replace_lines_does_not_touch_or_raise_event()
    {
        var product = ProductTestFactory.CreatePublishedProduct("SKU", "Keyboard", 100);
        var item = new OrderLineItem(
            ProductSnapshot.Create(product.Id, product.Sku, product.Name, ProductTestFactory.PrimaryVariant(product).Price, true),
            1,
            0);
        var order = Order.Create(
            OrderTestFactory.NextOrderCode(),
            OrderCustomerSnapshot.Create(Guid.NewGuid(), "Customer", "0901234567", null, null),
            [item]);
        order.ClearDomainEvents();
        var version = order.Version;
        var updatedAt = order.UpdatedAt;

        order.ReplaceLines([item]);

        Assert.Equal(version, order.Version);
        Assert.Equal(updatedAt, order.UpdatedAt);
        Assert.Empty(order.GetDomainEvents());
    }
}

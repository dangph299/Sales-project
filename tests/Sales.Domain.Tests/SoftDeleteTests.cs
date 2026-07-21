namespace Sales.Domain.Tests;

public sealed class SoftDeleteTests
{
    [Fact]
    public void Product_delete_stamps_metadata_and_blocks_updates()
    {
        var product = Product.Create("sku-1", "Keyboard", null, Guid.NewGuid());
        var originalUpdatedAt = product.UpdatedAt;

        product.Delete("admin");

        Assert.True(product.IsDelete);
        Assert.False(product.IsActive);
        Assert.Equal("admin", product.DeleteByUser);
        Assert.NotNull(product.DeletedAt);
        Assert.True(product.UpdatedAt >= originalUpdatedAt);
        Assert.Throws<DomainException>(() => product.Update("Mouse", null, Guid.NewGuid()));
    }

    [Fact]
    public void Product_delete_accepts_published_product()
    {
        var product = ProductTestFactory.CreatePublishedProduct("sku-1", "Keyboard", 100);

        product.Delete("admin");

        Assert.True(product.IsDelete);
    }

    [Fact]
    public void Product_delete_accepts_discontinued_product()
    {
        var product = ProductTestFactory.CreatePublishedProduct("sku-1", "Keyboard", 100);
        product.Discontinue();

        product.Delete("admin");

        Assert.True(product.IsDelete);
    }

    [Fact]
    public void Customer_delete_stamps_metadata_and_blocks_updates()
    {
        var customer = Customer.Create("Nguyen Van A", "0901234567");
        var originalUpdatedAt = customer.UpdatedAt;

        customer.Delete("admin");

        Assert.True(customer.IsDelete);
        Assert.Equal("admin", customer.DeleteByUser);
        Assert.NotNull(customer.DeletedAt);
        Assert.True(customer.UpdatedAt >= originalUpdatedAt);
        Assert.Throws<DomainException>(() => customer.Update("Nguyen Van B", "0901234568"));
    }
}

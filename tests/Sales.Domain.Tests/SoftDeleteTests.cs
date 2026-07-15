namespace Sales.Domain.Tests;

public sealed class SoftDeleteTests
{
    [Fact]
    public void Product_delete_stamps_metadata_and_blocks_updates()
    {
        var product = Product.Create("sku-1", "Keyboard", 100);
        var originalUpdatedAt = product.UpdatedAt;

        product.Delete("admin");

        Assert.True(product.IsDelete);
        Assert.False(product.IsActive);
        Assert.Equal("admin", product.DeleteByUser);
        Assert.NotNull(product.DeletedAt);
        Assert.True(product.UpdatedAt >= originalUpdatedAt);
        Assert.Throws<DomainException>(() => product.Update("Mouse", 120, true));
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

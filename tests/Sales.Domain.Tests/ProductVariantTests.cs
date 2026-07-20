using Sales.Domain;

namespace Sales.Domain.Tests;

public sealed class ProductVariantTests
{
    [Fact]
    public void Product_requires_category()
    {
        Assert.Throws<DomainException>(() => Product.Create("PRD001", "Basic T-Shirt", null, Guid.Empty));
    }

    [Fact]
    public void Variant_rejects_negative_price()
    {
        var product = Product.Create("PRD001", "Basic T-Shirt", null, Guid.NewGuid());
        product.Publish();
        var color = Color.Create(Guid.NewGuid(), "blk", "Black", "#000000");
        var size = Size.Create(Guid.NewGuid(), "m", "Medium", 40);

        Assert.Throws<DomainException>(() => product.AddVariant(color, size, -1));
    }

    [Fact]
    public void Variant_sku_is_generated_and_normalized()
    {
        var product = Product.Create(" prd001 ", "Basic T-Shirt", null, Guid.NewGuid());
        product.Publish();
        var color = Color.Create(Guid.NewGuid(), " blk ", "Black", "#000000");
        var size = Size.Create(Guid.NewGuid(), " m ", "Medium", 40);

        var productVariant = product.AddVariant(color, size, 150000);

        Assert.Equal("PRD001-BLK-M", productVariant.Sku);
    }

    [Fact]
    public void Product_rejects_duplicate_color_size_variant()
    {
        var product = Product.Create("PRD001", "Basic T-Shirt", null, Guid.NewGuid());
        product.Publish();
        var color = Color.Create(Guid.NewGuid(), "BLK", "Black", "#000000");
        var size = Size.Create(Guid.NewGuid(), "M", "Medium", 40);

        product.AddVariant(color, size, 150000);

        Assert.Throws<DomainException>(() => product.AddVariant(color, size, 160000));
    }
}

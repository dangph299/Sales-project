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
    public void Product_create_and_update_preserve_description()
    {
        var categoryId = Guid.NewGuid();
        var product = Product.Create("PRD001", "Basic T-Shirt", "  Initial description  ", categoryId);

        Assert.Equal("Initial description", product.Description);

        product.Update("Basic T-Shirt", "  Updated description  ", categoryId);

        Assert.Equal("Updated description", product.Description);
    }

    [Fact]
    public void Product_blank_description_is_normalized_to_null()
    {
        var categoryId = Guid.NewGuid();
        var product = Product.Create("PRD001", "Basic T-Shirt", "Initial description", categoryId);

        product.Update("Basic T-Shirt", "   ", categoryId);

        Assert.Null(product.Description);
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
    public void Draft_product_can_add_draft_variant()
    {
        var product = Product.Create("PRD001", "Basic T-Shirt", null, Guid.NewGuid());
        var color = Color.Create(Guid.NewGuid(), "BLK", "Black", "#000000");
        var size = Size.Create(Guid.NewGuid(), "M", "Medium", 40);

        var productVariant = product.AddVariant(color, size, 150000, EProductVariantStatus.Draft);

        Assert.Equal(EProductVariantStatus.Draft, productVariant.Status);
        Assert.Single(product.Variants);
    }

    [Fact]
    public void Draft_product_can_add_published_variant()
    {
        var product = Product.Create("PRD001", "Basic T-Shirt", null, Guid.NewGuid());
        var color = Color.Create(Guid.NewGuid(), "BLK", "Black", "#000000");
        var size = Size.Create(Guid.NewGuid(), "M", "Medium", 40);

        var productVariant = product.AddVariant(color, size, 150000, EProductVariantStatus.Published);

        Assert.Equal(EProductVariantStatus.Published, productVariant.Status);
    }

    [Fact]
    public void Product_can_be_published_again_after_discontinue()
    {
        var product = Product.Create("PRD001", "Basic T-Shirt", null, Guid.NewGuid());
        product.Publish();
        product.Discontinue();

        product.Publish();

        Assert.Equal(EProductStatus.Published, product.Status);
    }

    [Fact]
    public void Draft_product_cannot_be_discontinued()
    {
        var product = Product.Create("PRD001", "Basic T-Shirt", null, Guid.NewGuid());

        Assert.Throws<DomainException>(product.Discontinue);
    }

    [Fact]
    public void Variant_can_be_published_again_after_discontinue()
    {
        var product = Product.Create("PRD001", "Basic T-Shirt", null, Guid.NewGuid());
        product.Publish();
        var color = Color.Create(Guid.NewGuid(), "BLK", "Black", "#000000");
        var size = Size.Create(Guid.NewGuid(), "M", "Medium", 40);
        var variant = product.AddVariant(color, size, 150000, EProductVariantStatus.Published);
        product.DiscontinueVariant(variant.Id);

        product.PublishVariant(variant.Id);

        Assert.Equal(EProductVariantStatus.Published, variant.Status);
    }

    [Fact]
    public void Draft_variant_cannot_be_discontinued()
    {
        var product = Product.Create("PRD001", "Basic T-Shirt", null, Guid.NewGuid());
        var color = Color.Create(Guid.NewGuid(), "BLK", "Black", "#000000");
        var size = Size.Create(Guid.NewGuid(), "M", "Medium", 40);
        var variant = product.AddVariant(color, size, 150000, EProductVariantStatus.Draft);

        Assert.Throws<DomainException>(() => product.DiscontinueVariant(variant.Id));
    }

    [Fact]
    public void Product_can_soft_delete_variant()
    {
        var product = Product.Create("PRD001", "Basic T-Shirt", null, Guid.NewGuid());
        var color = Color.Create(Guid.NewGuid(), "BLK", "Black", "#000000");
        var size = Size.Create(Guid.NewGuid(), "M", "Medium", 40);
        var variant = product.AddVariant(color, size, 150000, EProductVariantStatus.Draft);

        product.DeleteVariant(variant.Id, "admin");

        Assert.True(variant.IsDelete);
        Assert.Equal("admin", variant.DeleteByUser);
    }

    [Fact]
    public void Product_can_soft_delete_discontinued_variant_when_product_is_published()
    {
        var product = Product.Create("PRD001", "Basic T-Shirt", null, Guid.NewGuid());
        product.Publish();
        var color = Color.Create(Guid.NewGuid(), "BLK", "Black", "#000000");
        var size = Size.Create(Guid.NewGuid(), "M", "Medium", 40);
        var variant = product.AddVariant(color, size, 150000, EProductVariantStatus.Published);
        product.DiscontinueVariant(variant.Id);

        product.DeleteVariant(variant.Id, "admin");

        Assert.True(variant.IsDelete);
        Assert.Equal("admin", variant.DeleteByUser);
    }

    [Fact]
    public void Product_rejects_delete_published_variant()
    {
        var product = Product.Create("PRD001", "Basic T-Shirt", null, Guid.NewGuid());
        product.Publish();
        var color = Color.Create(Guid.NewGuid(), "BLK", "Black", "#000000");
        var size = Size.Create(Guid.NewGuid(), "M", "Medium", 40);
        var variant = product.AddVariant(color, size, 150000, EProductVariantStatus.Published);

        Assert.Throws<DomainException>(() => product.DeleteVariant(variant.Id, "admin"));
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

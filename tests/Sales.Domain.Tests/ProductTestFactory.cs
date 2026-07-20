using Sales.Domain;

namespace Sales.Domain.Tests;

internal static class ProductTestFactory
{
    public static Product CreatePublishedProduct(string productCode, string name, decimal price)
    {
        var product = Product.Create(productCode, name, null, Guid.NewGuid());
        product.Publish();
        product.AddVariant(CreateBlackColor(), CreateMediumSize(), price, EProductVariantStatus.Published);
        return product;
    }

    public static ProductVariant PrimaryVariant(Product product)
    {
        return product.Variants.Single();
    }

    private static Color CreateBlackColor()
    {
        return Color.Create(Guid.NewGuid(), "BLK", "Black", "#000000");
    }

    private static Size CreateMediumSize()
    {
        return Size.Create(Guid.NewGuid(), "M", "Medium", 40);
    }
}

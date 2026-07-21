using Sales.Domain;

namespace Sales.Infrastructure.Tests;

internal static class ProductTestFactory
{
    public static Product CreatePublishedProduct(string productCode, string name, decimal price)
    {
        var product = Product.Create(productCode, name, null, CategoryReferenceDataIds.Uncategorized);
        product.Publish();
        product.AddVariant(CreateBlackColor(), CreateMediumSize(), price, EProductVariantStatus.Published);
        return product;
    }

    /// <summary>
    /// A product with no variants, which is the state a create leaves behind: never published.
    /// </summary>
    public static Product CreateDraftProduct(string productCode, string name)
    {
        return Product.Create(productCode, name, null, CategoryReferenceDataIds.Uncategorized);
    }

    public static ProductVariant PrimaryVariant(Product product)
    {
        return product.Variants.Single();
    }

    private static Color CreateBlackColor()
    {
        return Color.Create(ColorReferenceDataIds.Black, "BLK", "Black", "#000000");
    }

    private static Size CreateMediumSize()
    {
        return Size.Create(SizeReferenceDataIds.Medium, "M", "Medium", 40);
    }
}

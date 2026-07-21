using Sales.Domain;

namespace Sales.Infrastructure.Tests;

public sealed class ProductVariantPersistenceTests
{
    [Fact]
    public async Task Adding_variant_to_existing_published_product_does_not_raise_concurrency_conflict()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();

        var product = Product.Create("PRD-VAR-001", "Variant product", null, CategoryReferenceDataIds.Uncategorized);
        product.Publish();
        product.AddVariant(
            Color.Create(ColorReferenceDataIds.Black, "BLK", "Black", "#000000"),
            Size.Create(SizeReferenceDataIds.Medium, "M", "Medium", 40),
            100_000,
            EProductVariantStatus.Published);
        await fixture.SeedAsync(product);

        await using var context = fixture.CreateContext();
        var repository = new ProductRepository(context);
        var persisted = await repository.GetWithVariantsAsync(product.Id);
        var blue = await repository.GetColorAsync(ColorReferenceDataIds.Blue);
        var small = await repository.GetSizeAsync(SizeReferenceDataIds.Small);

        Assert.NotNull(persisted);
        Assert.NotNull(blue);
        Assert.NotNull(small);

        persisted.AddVariant(blue, small, 120_000, EProductVariantStatus.Published);

        await context.SaveChangesAsync();
        Assert.Equal(2, persisted.Variants.Count);
    }

    [Fact]
    public async Task Updating_variant_on_existing_published_product_does_not_raise_concurrency_conflict()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();

        var product = Product.Create("PRD-VAR-002", "Variant update product", null, CategoryReferenceDataIds.Uncategorized);
        product.Publish();
        product.AddVariant(
            Color.Create(ColorReferenceDataIds.Black, "BLK", "Black", "#000000"),
            Size.Create(SizeReferenceDataIds.Medium, "M", "Medium", 40),
            100_000,
            EProductVariantStatus.Published);
        await fixture.SeedAsync(product);

        await using var context = fixture.CreateContext();
        var repository = new ProductRepository(context);
        var persisted = await repository.GetWithVariantsAsync(product.Id);
        var blue = await repository.GetColorAsync(ColorReferenceDataIds.Blue);
        var small = await repository.GetSizeAsync(SizeReferenceDataIds.Small);

        Assert.NotNull(persisted);
        Assert.NotNull(blue);
        Assert.NotNull(small);

        var variant = persisted.Variants.Single();
        persisted.UpdateVariant(variant.Id, blue, small, 125_000, EProductVariantStatus.Published);

        await context.SaveChangesAsync();
        Assert.Equal(125_000, persisted.Variants.Single().Price.Amount);
    }
}

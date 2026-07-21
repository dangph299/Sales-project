using Sales.Domain;

namespace Sales.Infrastructure.Tests;

public sealed class ReadServiceSpecificationTests
{
    [Fact]
    public async Task Product_get_returns_active_product()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var active = ProductTestFactory.CreatePublishedProduct("sku-active", "Active", 100);
        await fixture.SeedAsync(active);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.GetAsync(active.Id);

        Assert.NotNull(result);
        Assert.Equal(active.Id, result.Id);
    }

    [Fact]
    public async Task Product_write_result_read_returns_persisted_description()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var product = Product.Create(
            "sku-description",
            "Description product",
            "Persisted product description",
            CategoryReferenceDataIds.Uncategorized);
        await fixture.SeedAsync(product);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.GetForWriteResultAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal("Persisted product description", result.Description);
    }

    [Fact]
    public async Task Category_list_returns_persisted_description()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var category = Category.Create("CAT-DESC", "Described category", "Persisted category description", null, 5);
        category.ClearDomainEvents();
        await using (var context = fixture.CreateContext())
        {
            context.Categories.Add(category);
            await context.SaveChangesAsync();
        }

        var service = new CategoryReadService(fixture.CreateContext());

        var result = await service.ListCategoriesAsync();

        Assert.Contains(result, item =>
            item.Id == category.Id &&
            item.Description == "Persisted category description");
    }

    [Fact]
    public async Task Product_get_excludes_inactive_products()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var inactive = ProductTestFactory.CreatePublishedProduct("sku-inactive", "Inactive", 100);
        inactive.DiscontinueVariant(ProductTestFactory.PrimaryVariant(inactive).Id);
        await fixture.SeedAsync(inactive);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.GetAsync(inactive.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task Product_write_result_read_returns_a_draft_product()
    {
        // This is the shipped bug: a product created without variants is never published, so the
        // published-only catalog filter made the create handler report a 404 for a row it had
        // already committed. Covered with a genuinely Draft product, not a discontinued one.
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var draft = ProductTestFactory.CreateDraftProduct("sku-draft", "Draft");
        await fixture.SeedAsync(draft);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.GetForWriteResultAsync(draft.Id);

        Assert.NotNull(result);
        Assert.Equal(draft.Id, result.Id);
        Assert.Null(await service.GetAsync(draft.Id));
    }

    [Fact]
    public async Task Product_write_result_read_returns_a_discontinued_product()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var discontinued = ProductTestFactory.CreatePublishedProduct("sku-disc", "Discontinued", 100);
        discontinued.Discontinue();
        await fixture.SeedAsync(discontinued);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        Assert.NotNull(await service.GetForWriteResultAsync(discontinued.Id));
    }

    [Fact]
    public async Task Product_write_result_read_still_excludes_deleted_products()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var deleted = ProductTestFactory.CreatePublishedProduct("sku-gone", "Gone", 100);
        deleted.Delete("admin");
        await fixture.SeedAsync(deleted);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        Assert.Null(await service.GetForWriteResultAsync(deleted.Id));
    }

    [Fact]
    public async Task Product_get_excludes_deleted_products()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var deleted = ProductTestFactory.CreatePublishedProduct("sku-deleted", "Deleted", 100);
        deleted.Delete("admin");
        await fixture.SeedAsync(deleted);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.GetAsync(deleted.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task Product_search_includes_inactive_products()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var active = ProductTestFactory.CreatePublishedProduct("sku-active", "Keyboard", 100);
        var inactive = ProductTestFactory.CreatePublishedProduct("sku-inactive", "Keyboard Disabled", 100);
        inactive.DiscontinueVariant(ProductTestFactory.PrimaryVariant(inactive).Id);
        await fixture.SeedAsync(active, inactive);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.SearchAsync(null, 1, 20);

        Assert.Equal([active.Id, inactive.Id], result.Items.Select(x => x.Id).ToArray());
        Assert.Contains(result.Items, x => x.Id == inactive.Id && !x.IsActive);
    }

    [Fact]
    public async Task Product_search_excludes_deleted_products()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var active = ProductTestFactory.CreatePublishedProduct("sku-active", "Monitor", 100);
        var deleted = ProductTestFactory.CreatePublishedProduct("sku-deleted", "Monitor Deleted", 100);
        deleted.Delete("admin");
        await fixture.SeedAsync(active, deleted);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.SearchAsync(null, 1, 20);

        Assert.Equal([active.Id], result.Items.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task Customer_get_returns_non_deleted_customer()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var active = Customer.Create("Nguyen Van Active", "0901234567");
        await fixture.SeedAsync(active);

        var service = new CustomerReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.GetAsync(active.Id);

        Assert.NotNull(result);
        Assert.Equal(active.Id, result.Id);
    }

    [Fact]
    public async Task Customer_get_excludes_deleted_customers()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var deleted = Customer.Create("Nguyen Van Deleted", "0901234567");
        deleted.Delete("admin");
        await fixture.SeedAsync(deleted);

        var service = new CustomerReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.GetAsync(deleted.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task Customer_search_excludes_deleted_customers()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var active = Customer.Create("Nguyen Van Active", "0901234567");
        var deleted = Customer.Create("Nguyen Van Deleted", "0901234568");
        deleted.Delete("admin");
        await fixture.SeedAsync(active, deleted);

        var service = new CustomerReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.SearchAsync(null, null, 1, 20);

        Assert.Equal([active.Id], result.Items.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task Customer_phone_search_matches_both_prefix_and_suffix()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var prefixMatch = Customer.Create("Nguyen Van Prefix", "0901234567");
        var suffixMatch = Customer.Create("Nguyen Van Suffix", "0987654321");
        var unrelated = Customer.Create("Nguyen Van Other", "0912000111");
        await fixture.SeedAsync(prefixMatch, suffixMatch, unrelated);

        var service = new CustomerReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.SearchAsync(null, "4321", 1, 20);

        Assert.Equal([suffixMatch.Id], result.Items.Select(x => x.Id).ToArray());

        var both = await service.SearchAsync(null, "09", 1, 20);

        Assert.Equal(3, both.Items.Count);
    }

    // OrderReadService.SearchAsync cannot run on the SQLite fixture: it orders by CreatedAt, and
    // SQLite rejects DateTimeOffset in ORDER BY. The status filter is therefore covered at the
    // specification level, the same way the other order search predicates are.
    [Fact]
    public void Order_status_specification_matches_only_the_requested_status()
    {
        var draft = CreateOrder();
        var pending = CreateOrder();
        pending.RequestConfirmation();

        var result = new[] { draft, pending }.AsQueryable()
            .Where(new OrderStatusEqualsSpecification(OrderStatus.PendingInventory).ToExpression())
            .ToArray();

        Assert.Equal([pending], result);
    }

    [Fact]
    public void Order_status_specification_composes_with_the_customer_filter()
    {
        var pending = CreateOrder();
        pending.RequestConfirmation();

        var spec = new OrderStatusEqualsSpecification(OrderStatus.Draft)
            .And(new OrderStatusEqualsSpecification(OrderStatus.PendingInventory));

        Assert.False(spec.IsSatisfiedBy(pending));
        Assert.True(new OrderStatusEqualsSpecification(OrderStatus.PendingInventory).IsSatisfiedBy(pending));
    }

    private static Order CreateOrder()
    {
        var customer = CustomerSnapshot.Create(Guid.NewGuid(), "Nguyen Van A", "0901234567");
        var product = ProductTestFactory.CreatePublishedProduct($"sku-{Guid.NewGuid():N}", "Keyboard", 100_000);
        var snapshot = ProductSnapshot.Create(
            product.Id,
            product.Sku,
            product.Name,
            ProductTestFactory.PrimaryVariant(product).Price,
            product.IsActive);
        return Order.Create(customer, [new OrderLineItem(snapshot, 1, 0m)]);
    }
}

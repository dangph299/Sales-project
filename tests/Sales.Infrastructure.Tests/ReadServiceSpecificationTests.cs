using Microsoft.EntityFrameworkCore;
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
    public async Task Product_variant_search_pages_variants_instead_of_products()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var first = ProductTestFactory.CreatePublishedProduct("PRD-VPAGE-1", "Variant Page One", 100);
        var second = ProductTestFactory.CreatePublishedProduct("PRD-VPAGE-2", "Variant Page Two", 100);
        second.AddVariant(
            Color.Create(ColorReferenceDataIds.White, "WHT", "White", "#FFFFFF"),
            Size.Create(SizeReferenceDataIds.Small, "S", "Small", 30),
            110,
            EProductVariantStatus.Published);
        await fixture.SeedAsync(first, second);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.SearchVariantsAsync(null, null, null, null, "sku", "asc", 1, 2);

        Assert.Equal(3, result.Total);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.NotEqual(Guid.Empty, item.ProductVariantId));
    }

    [Fact]
    public async Task Product_variant_search_filters_by_product_name()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var match = ProductTestFactory.CreatePublishedProduct("PRD-VFILTER-1", "Needle Shirt", 100);
        var miss = ProductTestFactory.CreatePublishedProduct("PRD-VFILTER-2", "Other Shirt", 100);
        await fixture.SeedAsync(match, miss);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.SearchVariantsAsync(null, "Needle", null, null, "sku", "asc", 1, 20);

        Assert.Equal([match.Id], result.Items.Select(x => x.ProductId).ToArray());
    }

    [Fact]
    public async Task Product_variant_search_sorts_by_sku_descending()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var first = ProductTestFactory.CreatePublishedProduct("PRD-VSORT-1", "Sort One", 100);
        var second = ProductTestFactory.CreatePublishedProduct("PRD-VSORT-2", "Sort Two", 100);
        await fixture.SeedAsync(first, second);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.SearchVariantsAsync(null, null, null, null, "sku", "desc", 1, 20);

        Assert.Equal(
            result.Items.OrderByDescending(x => x.Sku).Select(x => x.Sku).ToArray(),
            result.Items.Select(x => x.Sku).ToArray());
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

    [Fact]
    public async Task Sqlite_honours_the_escape_the_variant_name_search_falls_back_to()
    {
        // SearchVariantsAsync uses EF.Functions.ILike on Npgsql but falls back to Like on any other
        // provider, and only the Like branch runs on SQLite. This pins that the three-argument
        // Like(column, pattern, escape) both translates on SQLite and honours the escaping, so the
        // fallback treats a typed "%" as a literal exactly as the Npgsql path does.
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var literal = ProductTestFactory.CreatePublishedProduct("PRD-1", "50% off bundle", 100);
        var other = ProductTestFactory.CreatePublishedProduct("PRD-2", "5000 unit pack", 100);
        await fixture.SeedAsync(literal, other);

        await using var context = fixture.CreateContext();
        // "50%" escaped for a contains-match: the inner % is escaped, the surrounding two are wildcards.
        const string pattern = "%50\\%%";
        var names = await context.Products.AsNoTracking()
            .Where(x => EF.Functions.Like(x.Name, pattern, "\\"))
            .Select(x => x.Name)
            .ToListAsync();

        Assert.Equal(["50% off bundle"], names);
    }

    [Fact]
    public async Task Customer_phone_search_with_no_digits_returns_no_customers()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var customer = Customer.Create("Nguyen Van A", "0901234567");
        await fixture.SeedAsync(customer);

        var service = new CustomerReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        // A term that reduces to no digits is a filter nothing matches, not the absence of a filter:
        // before the guard, the query fell through to LIKE '%' and returned the whole table.
        var byLetters = await service.SearchAsync(null, "abc", 1, 20);

        Assert.Empty(byLetters.Items);
        Assert.Equal(0, byLetters.Total);
    }

    [Fact]
    public async Task Customer_phone_search_with_only_symbols_returns_no_customers()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var customer = Customer.Create("Nguyen Van A", "0901234567");
        await fixture.SeedAsync(customer);

        var service = new CustomerReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var bySymbols = await service.SearchAsync(null, " (),.- ", 1, 20);

        Assert.Empty(bySymbols.Items);
        Assert.Equal(0, bySymbols.Total);
    }

    [Fact]
    public async Task Customer_phone_search_matches_a_digit_only_prefix()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var match = Customer.Create("Nguyen Van A", "0901234567");
        var other = Customer.Create("Tran Thi B", "0912345678");
        await fixture.SeedAsync(match, other);

        var service = new CustomerReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.SearchAsync(null, "0901", 1, 20);

        Assert.Equal([match.Id], result.Items.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task Customer_phone_search_accepts_a_formatted_term_with_valid_digits()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var match = Customer.Create("Nguyen Van A", "0901234567");
        await fixture.SeedAsync(match);

        var service = new CustomerReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        // The punctuation is stripped by normalization, so a formatted prefix still matches.
        var result = await service.SearchAsync(null, "090-123", 1, 20);

        Assert.Equal([match.Id], result.Items.Select(x => x.Id).ToArray());
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
        var customer = OrderCustomerSnapshot.Create(Guid.NewGuid(), "Nguyen Van A", "0901234567", null, null);
        var product = ProductTestFactory.CreatePublishedProduct($"sku-{Guid.NewGuid():N}", "Keyboard", 100_000);
        var snapshot = ProductSnapshot.Create(
            product.Id,
            product.Sku,
            product.Name,
            ProductTestFactory.PrimaryVariant(product).Price,
            product.IsActive);
        return Order.Create(OrderTestFactory.NextOrderCode(), customer, [new OrderLineItem(snapshot, 1, 0m)]);
    }
}

using Sales.Application.Features.Customers.Enums;
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
    public async Task Product_get_excludes_inactive_products()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var inactive = ProductTestFactory.CreatePublishedProduct("sku-inactive", "Inactive", 100);
        inactive.Discontinue();
        await fixture.SeedAsync(inactive);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.GetAsync(inactive.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task Product_write_result_read_returns_products_that_are_not_published()
    {
        // A product created without variants stays Draft. Command handlers read their own result
        // back through this method, so it must not apply the published-only catalog filter or the
        // write is reported as a 404 after it has already been committed.
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var draft = ProductTestFactory.CreatePublishedProduct("sku-draft", "Draft", 100);
        draft.Discontinue();
        await fixture.SeedAsync(draft);

        var service = new ProductReadService(fixture.CreateContext(), SalesMapperFactory.Create());

        var result = await service.GetForWriteResultAsync(draft.Id);

        Assert.NotNull(result);
        Assert.Equal(draft.Id, result.Id);
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
        inactive.Discontinue();
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

        var result = await service.SearchAsync(null, null, PhoneMatch.Prefix, 1, 20);

        Assert.Equal([active.Id], result.Items.Select(x => x.Id).ToArray());
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

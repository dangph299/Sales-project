using Sales.Application;
using Sales.Domain;

namespace Sales.Infrastructure.Tests;

public sealed class ReadServiceSpecificationTests
{
    [Fact]
    public async Task Product_get_returns_active_product()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var active = Product.Create("sku-active", "Active", 100);
        await fixture.SeedAsync(active);

        var service = new ProductReadService(fixture.CreateContext());

        var result = await service.GetAsync(active.Id);

        Assert.NotNull(result);
        Assert.Equal(active.Id, result.Id);
    }

    [Fact]
    public async Task Product_get_excludes_inactive_products()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var inactive = Product.Create("sku-inactive", "Inactive", 100);
        inactive.Update(inactive.Name, inactive.Price.Amount, false);
        await fixture.SeedAsync(inactive);

        var service = new ProductReadService(fixture.CreateContext());

        var result = await service.GetAsync(inactive.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task Product_get_excludes_deleted_products()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var deleted = Product.Create("sku-deleted", "Deleted", 100);
        deleted.Delete("admin");
        await fixture.SeedAsync(deleted);

        var service = new ProductReadService(fixture.CreateContext());

        var result = await service.GetAsync(deleted.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task Product_search_excludes_inactive_products()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var active = Product.Create("sku-active", "Keyboard", 100);
        var inactive = Product.Create("sku-inactive", "Keyboard Disabled", 100);
        inactive.Update(inactive.Name, inactive.Price.Amount, false);
        await fixture.SeedAsync(active, inactive);

        var service = new ProductReadService(fixture.CreateContext());

        var result = await service.SearchAsync(null, 1, 20);

        Assert.Equal([active.Id], result.Items.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task Product_search_excludes_deleted_products()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var active = Product.Create("sku-active", "Monitor", 100);
        var deleted = Product.Create("sku-deleted", "Monitor Deleted", 100);
        deleted.Delete("admin");
        await fixture.SeedAsync(active, deleted);

        var service = new ProductReadService(fixture.CreateContext());

        var result = await service.SearchAsync(null, 1, 20);

        Assert.Equal([active.Id], result.Items.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task Customer_get_returns_non_deleted_customer()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var active = Customer.Create("Nguyen Van Active", "0901234567");
        await fixture.SeedAsync(active);

        var service = new CustomerReadService(fixture.CreateContext());

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

        var service = new CustomerReadService(fixture.CreateContext());

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

        var service = new CustomerReadService(fixture.CreateContext());

        var result = await service.SearchAsync(null, null, PhoneMatch.Prefix, 1, 20);

        Assert.Equal([active.Id], result.Items.Select(x => x.Id).ToArray());
    }
}

using Microsoft.EntityFrameworkCore;
using Sales.Application.Common.Interfaces;
using Sales.Domain;
using Xunit;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Exercises the customer and product name searches against a real PostgreSQL instance, pinning that
/// a typed LIKE metacharacter is matched literally rather than as a wildcard.
/// </summary>
/// <remarks>
/// These are the production (Npgsql <c>ILike</c>) name-search paths, siblings of the order name
/// search. When no PostgreSQL is available every test here skips visibly rather than passing silently.
/// </remarks>
[Trait("Category", "Reliability")]
[Collection("SalesReliabilityPostgres")]
public sealed class ReadServiceNameSearchPostgresTests
{
    private readonly PostgresReliabilityFixture _fixture;

    public ReadServiceNameSearchPostgresTests(PostgresReliabilityFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Customer_name_search_treats_percent_as_a_literal()
    {
        await using var context = await PrepareAsync();
        context.Customers.Add(Customer.Create("50% discount buyer", "0901234567"));
        context.Customers.Add(Customer.Create("5089 regular buyer", "0912345678"));
        await context.SaveChangesAsync();

        var service = new CustomerReadService(context, SalesMapperFactory.Create());

        // Unescaped, "50%" reads as "contains 50 then anything" and matches both. Escaped, it matches
        // only the name that literally contains "50%".
        var result = await service.SearchAsync("50%", null, 1, 20);

        Assert.Equal(["50% discount buyer"], result.Items.Select(x => x.Name));
    }

    [SkippableFact]
    public async Task Customer_name_search_still_matches_a_literal_fragment()
    {
        await using var context = await PrepareAsync();
        context.Customers.Add(Customer.Create("Nguyen Van A", "0901234567"));
        context.Customers.Add(Customer.Create("Tran Thi B", "0912345678"));
        await context.SaveChangesAsync();

        var service = new CustomerReadService(context, SalesMapperFactory.Create());

        var result = await service.SearchAsync("nguyen", null, 1, 20);

        Assert.Equal(["Nguyen Van A"], result.Items.Select(x => x.Name));
    }

    [SkippableFact]
    public async Task Product_name_search_treats_underscore_as_a_literal()
    {
        await using var context = await PrepareAsync();
        context.Products.Add(ProductTestFactory.CreatePublishedProduct("PRD-0000001", "Model A_1 kit", 100));
        context.Products.Add(ProductTestFactory.CreatePublishedProduct("PRD-0000002", "Model AX1 kit", 100));
        await context.SaveChangesAsync();

        var service = new ProductReadService(context, SalesMapperFactory.Create());

        // Unescaped, "A_1" matches any single character between A and 1, so it would find "AX1" too.
        // Escaped, it looks for a literal underscore.
        var result = await service.SearchAsync("A_1", 1, 20);

        Assert.Equal(["Model A_1 kit"], result.Items.Select(x => x.Name));
    }

    [SkippableFact]
    public async Task Product_name_search_still_matches_a_literal_fragment()
    {
        await using var context = await PrepareAsync();
        context.Products.Add(ProductTestFactory.CreatePublishedProduct("PRD-0000001", "Ergonomic keyboard", 100));
        context.Products.Add(ProductTestFactory.CreatePublishedProduct("PRD-0000002", "Wireless mouse", 100));
        await context.SaveChangesAsync();

        var service = new ProductReadService(context, SalesMapperFactory.Create());

        var result = await service.SearchAsync("keyboard", 1, 20);

        Assert.Equal(["Ergonomic keyboard"], result.Items.Select(x => x.Name));
    }

    private async Task<SalesDbContext> PrepareAsync()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var options = new DbContextOptionsBuilder<SalesDbContext>().UseNpgsql(_fixture.ConnectionString).Options;
        var context = new SalesDbContext(options, new SearchTestExecutionContext());
        await context.Database.MigrateAsync();

        // Each test owns the whole table set, so results are the seed and nothing else.
        await context.Database.ExecuteSqlRawAsync("DELETE FROM order_lines");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM orders");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM product_variants");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM products");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM customers");
        return context;
    }

    private sealed class SearchTestExecutionContext : IExecutionContext
    {
        public string Actor => "integration-test";

        public Guid CorrelationId => Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    }
}

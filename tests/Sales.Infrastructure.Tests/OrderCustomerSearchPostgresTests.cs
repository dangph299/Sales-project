using BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sales.Application.Common.Interfaces;
using Sales.Domain;
using Xunit;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Exercises order customer search against a real PostgreSQL instance.
/// </summary>
/// <remarks>
/// When no PostgreSQL is available every test here skips visibly rather than passing silently.
/// </remarks>
[Trait("Category", "Reliability")]
[Collection("SalesReliabilityPostgres")]
public sealed class OrderCustomerSearchPostgresTests
{
    private readonly PostgresReliabilityFixture _fixture;

    public OrderCustomerSearchPostgresTests(PostgresReliabilityFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Customer_name_search_reads_the_order_snapshot()
    {
        await using var context = await PrepareAsync();
        await SeedOrderAsync(context, "ORD-0000901", "Nguyen Van A", "0901234567");
        await SeedOrderAsync(context, "ORD-0000902", "Tran Thi B", "0912345678");

        var results = await SearchAsync(context, customerName: "nguyen");

        Assert.Equal(["ORD-0000901"], results.Items.Select(x => x.OrderCode));
    }

    [SkippableFact]
    public async Task Phone_search_matches_from_the_start_of_the_number()
    {
        await using var context = await PrepareAsync();
        await SeedOrderAsync(context, "ORD-0000901", "A", "0901234567");
        await SeedOrderAsync(context, "ORD-0000902", "B", "0909999999");
        await SeedOrderAsync(context, "ORD-0000903", "C", "0912345678");

        var results = await SearchAsync(context, customerPhone: "090");

        Assert.Equal(["ORD-0000901", "ORD-0000902"], results.Items.Select(x => x.OrderCode).Order());
    }

    [SkippableFact]
    public async Task Phone_search_matches_from_the_end_of_the_number()
    {
        await using var context = await PrepareAsync();
        await SeedOrderAsync(context, "ORD-0000901", "A", "0901234567");
        await SeedOrderAsync(context, "ORD-0000902", "B", "0911114567");
        await SeedOrderAsync(context, "ORD-0000903", "C", "0912345678");

        // The user is not asked which end they remembered, so this finds both without a match mode.
        var results = await SearchAsync(context, customerPhone: "4567");

        Assert.Equal(["ORD-0000901", "ORD-0000902"], results.Items.Select(x => x.OrderCode).Order());
    }

    [SkippableFact]
    public async Task An_order_matching_both_ends_is_returned_once()
    {
        await using var context = await PrepareAsync();
        // Starts with 090 and ends with 090, so both halves of the predicate hold.
        await SeedOrderAsync(context, "ORD-0000901", "A", "090123090");

        var results = await SearchAsync(context, customerPhone: "090");

        Assert.Equal(["ORD-0000901"], results.Items.Select(x => x.OrderCode));
        Assert.Equal(1, results.Total);
    }

    [SkippableTheory]
    [InlineData("090 123 45")]
    [InlineData("090.123.45")]
    [InlineData("090-123-45")]
    [InlineData("(090) 123 45")]
    public async Task Phone_search_ignores_whatever_punctuation_the_user_typed(string customerPhoneSearchTerm)
    {
        await using var context = await PrepareAsync();
        await SeedOrderAsync(context, "ORD-0000901", "A", "0901234567");

        var results = await SearchAsync(context, customerPhone: customerPhoneSearchTerm);

        Assert.Equal(["ORD-0000901"], results.Items.Select(x => x.OrderCode));
    }

    [SkippableFact]
    public async Task Phone_search_does_not_match_digits_in_the_middle_of_the_number()
    {
        await using var context = await PrepareAsync();
        await SeedOrderAsync(context, "ORD-0000901", "A", "0901234567");

        // "1234" sits in the middle: it is neither end, so searching both ends must still miss it.
        // A Contains-style match would find it, and would not be able to use either index.
        var results = await SearchAsync(context, customerPhone: "1234");

        Assert.Empty(results.Items);
    }

    [SkippableFact]
    public async Task Order_number_search_reaches_orders_outside_the_first_page()
    {
        await using var context = await PrepareAsync();
        for (var index = 1; index <= 30; index++)
        {
            await SeedOrderAsync(context, $"ORD-{index:D7}", $"Customer {index}", "0901234567");
        }

        // The old client-side filter only ever matched within the page already loaded, so an order
        // this far down was invisible. Asking for one row proves the database did the filtering.
        var results = await SearchAsync(context, orderNumber: "ORD-0000027", pageSize: 5);

        Assert.Equal(["ORD-0000027"], results.Items.Select(x => x.OrderCode));
        Assert.Equal(1, results.Total);
    }

    [SkippableFact]
    public async Task Renaming_the_customer_leaves_the_order_snapshot_and_its_search_alone()
    {
        await using var context = await PrepareAsync();
        var customer = await SeedCustomerAsync(context, "CUS901", "Nguyen Van A", "0901234567");
        await SeedOrderAsync(context, "ORD-0000901", "Nguyen Van A", "0901234567", customer.Id);

        customer.Update("Tran Thi B", "0987654321");
        await context.SaveChangesAsync();

        var byOldName = await SearchAsync(context, customerName: "Nguyen");
        var byOldPhone = await SearchAsync(context, customerPhone: "0901");
        var byNewName = await SearchAsync(context, customerName: "Tran Thi B");

        Assert.Equal(["ORD-0000901"], byOldName.Items.Select(x => x.OrderCode));
        Assert.Equal(["ORD-0000901"], byOldPhone.Items.Select(x => x.OrderCode));
        Assert.Empty(byNewName.Items);
        Assert.Equal("Nguyen Van A", byOldName.Items.Single().CustomerName);
    }

    [SkippableFact]
    public async Task Soft_deleting_the_customer_leaves_the_order_findable_and_intact()
    {
        await using var context = await PrepareAsync();
        var customer = await SeedCustomerAsync(context, "CUS901", "Nguyen Van A", "0901234567");
        await SeedOrderAsync(context, "ORD-0000901", "Nguyen Van A", "0901234567", customer.Id);

        customer.Delete("integration-test");
        await context.SaveChangesAsync();

        var results = await SearchAsync(context, customerPhone: "0901");

        var order = Assert.Single(results.Items);
        Assert.Equal("Nguyen Van A", order.CustomerName);
        Assert.Equal("0901234567", order.CustomerPhone);
    }

    [SkippableFact]
    public async Task Order_search_never_joins_the_customer_table()
    {
        await using var context = await PrepareAsync();

        var sql = context.Orders
            .Include(x => x.Lines)
            .AsNoTracking()
            .Where(new OrderCustomerNameContainsSpecification("nguyen").ToExpression())
            .Where(new OrderCustomerPhoneMatchesSpecification("0901").ToExpression())
            .ToQueryString();

        Assert.DoesNotContain("customers", sql, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Several_orders_may_share_one_customer_phone_number()
    {
        await using var context = await PrepareAsync();
        var customer = await SeedCustomerAsync(context, "CUS901", "Nguyen Van A", "0901234567");
        await SeedOrderAsync(context, "ORD-0000901", "Nguyen Van A", "0901234567", customer.Id);
        await SeedOrderAsync(context, "ORD-0000902", "Nguyen Van A", "0901234567", customer.Id);

        var results = await SearchAsync(context, customerPhone: "0901234567");

        Assert.Equal(2, results.Total);
    }

    [SkippableFact]
    public async Task Customer_lookup_matches_a_phone_prefix_and_excludes_deleted_customers()
    {
        await using var context = await PrepareAsync();
        await SeedCustomerAsync(context, "CUS901", "Nguyen Van A", "0901234567");
        await SeedCustomerAsync(context, "CUS902", "Tran Thi B", "0901239999");
        var deletedCustomer = await SeedCustomerAsync(context, "CUS903", "Le Van C", "0901238888");
        deletedCustomer.Delete("integration-test");
        await context.SaveChangesAsync();

        var readService = new CustomerReadService(context, SalesMapperFactory.Create());
        var suggestions = await readService.LookupByPhonePrefixAsync("090123", 10);

        Assert.Equal(2, suggestions.Count);
        Assert.DoesNotContain(suggestions, x => x.Name == "Le Van C");
    }

    [SkippableFact]
    public async Task Customer_lookup_honours_its_result_limit()
    {
        await using var context = await PrepareAsync();
        for (var index = 0; index < 8; index++)
        {
            await SeedCustomerAsync(context, $"CUS9{index:D2}", $"Customer {index}", $"09012345{index:D2}");
        }

        var readService = new CustomerReadService(context, SalesMapperFactory.Create());
        var suggestions = await readService.LookupByPhonePrefixAsync("0901", 3);

        Assert.Equal(3, suggestions.Count);
    }

    [SkippableFact]
    public async Task Customer_lookup_returns_nothing_when_the_term_holds_no_digit()
    {
        await using var context = await PrepareAsync();
        await SeedCustomerAsync(context, "CUS901", "Nguyen Van A", "0901234567");

        var readService = new CustomerReadService(context, SalesMapperFactory.Create());

        Assert.Empty(await readService.LookupByPhonePrefixAsync("abc", 10));
        Assert.Empty(await readService.LookupByPhonePrefixAsync("", 10));
    }

    [SkippableFact]
    public async Task Order_number_search_treats_percent_as_a_literal()
    {
        await using var context = await PrepareAsync();
        await SeedOrderAsync(context, "ORD-0000901", "A", "0901234567");
        await SeedOrderAsync(context, "ORD-0000902", "B", "0912345678");

        // Unescaped, "%" is a wildcard that matches every order code. Escaped, it matches only a code
        // that literally contains "%", of which there are none.
        var results = await SearchAsync(context, orderNumber: "%");

        Assert.Empty(results.Items);
    }

    [SkippableFact]
    public async Task Order_number_search_treats_underscore_as_a_literal()
    {
        await using var context = await PrepareAsync();
        await SeedOrderAsync(context, "ORD-0000901", "A", "0901234567");

        // Unescaped, "_" matches any single character, so "ORD-000090_" would match "ORD-0000901".
        // Escaped, it looks for a literal underscore, which the code has not got.
        var results = await SearchAsync(context, orderNumber: "ORD-000090_");

        Assert.Empty(results.Items);
    }

    [SkippableFact]
    public async Task Order_number_search_still_matches_a_literal_prefix()
    {
        await using var context = await PrepareAsync();
        await SeedOrderAsync(context, "ORD-0000901", "A", "0901234567");
        await SeedOrderAsync(context, "ORD-0000902", "B", "0912345678");

        var results = await SearchAsync(context, orderNumber: "ORD-000090");

        Assert.Equal(["ORD-0000901", "ORD-0000902"], results.Items.Select(x => x.OrderCode).Order());
    }

    [SkippableFact]
    public async Task Customer_name_search_treats_percent_as_a_literal()
    {
        await using var context = await PrepareAsync();
        await SeedOrderAsync(context, "ORD-0000901", "50% discount buyer", "0901234567");
        await SeedOrderAsync(context, "ORD-0000902", "5089 regular buyer", "0912345678");

        // Unescaped, "50%" reads as "contains 50 then anything" and matches both. Escaped, it matches
        // only the name that literally contains "50%".
        var results = await SearchAsync(context, customerName: "50%");

        Assert.Equal(["ORD-0000901"], results.Items.Select(x => x.OrderCode));
    }

    [SkippableFact]
    public async Task Customer_name_search_treats_backslash_as_a_literal()
    {
        await using var context = await PrepareAsync();
        await SeedOrderAsync(context, "ORD-0000901", @"a\b partner", "0901234567");
        await SeedOrderAsync(context, "ORD-0000902", "ab partner", "0912345678");

        var results = await SearchAsync(context, customerName: @"a\b");

        Assert.Equal(["ORD-0000901"], results.Items.Select(x => x.OrderCode));
    }

    [SkippableFact]
    public async Task Customer_name_search_still_matches_a_literal_fragment()
    {
        await using var context = await PrepareAsync();
        await SeedOrderAsync(context, "ORD-0000901", "Nguyen Van A", "0901234567");
        await SeedOrderAsync(context, "ORD-0000902", "Tran Thi B", "0912345678");

        var results = await SearchAsync(context, customerName: "nguyen");

        Assert.Equal(["ORD-0000901"], results.Items.Select(x => x.OrderCode));
    }

    private async Task<PagedResult<Application.Features.Orders.DTOs.OrderDto>> SearchAsync(
        SalesDbContext context,
        string? orderNumber = null,
        string? customerName = null,
        string? customerPhone = null,
        int pageSize = 50)
    {
        var readService = new OrderReadService(context, SalesMapperFactory.Create());
        return await readService.SearchAsync(
            orderNumber,
            customerName,
            customerPhone,
            null,
            null,
            null,
            1,
            pageSize);
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
        await context.Database.ExecuteSqlRawAsync("DELETE FROM customers");
        return context;
    }

    private static async Task<Customer> SeedCustomerAsync(
        SalesDbContext context,
        string customerCode,
        string customerName,
        string customerPhone)
    {
        var customer = Customer.Create(customerCode, customerName, customerPhone);
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    private static async Task SeedOrderAsync(
        SalesDbContext context,
        string orderCode,
        string customerName,
        string customerPhone,
        Guid? customerId = null)
    {
        var product = ProductTestFactory.CreatePublishedProduct($"PRD{orderCode}", "Product", 100);
        var variant = ProductTestFactory.PrimaryVariant(product);
        var order = Order.Create(
            orderCode,
            OrderCustomerSnapshot.Create(customerId ?? Guid.NewGuid(), customerName, customerPhone, null, null),
            [
                new OrderLineItem(
                    ProductSnapshot.Create(
                        product.Id,
                        variant.Id,
                        product.ProductCode,
                        product.Name,
                        variant.Sku,
                        "BLK",
                        "Black",
                        "M",
                        variant.Price,
                        true),
                    1,
                    0)
            ]);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }

    private sealed class SearchTestExecutionContext : IExecutionContext
    {
        public string Actor => "integration-test";

        public Guid CorrelationId => Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    }
}

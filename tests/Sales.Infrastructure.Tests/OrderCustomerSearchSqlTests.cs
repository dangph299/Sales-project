using Microsoft.EntityFrameworkCore;
using Sales.Application.Common.Interfaces;
using Sales.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Pins the SQL the order search specifications generate.
/// </summary>
public sealed class OrderCustomerSearchSqlTests
{
    private readonly ITestOutputHelper _output;

    public OrderCustomerSearchSqlTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Phone_search_reads_both_ends_in_one_predicate()
    {
        var sql = BuildSql(new OrderCustomerPhoneMatchesSpecification("4567"));

        _output.WriteLine(sql);
        // Both halves in one WHERE, so an order matching both ends still comes back once, and each
        // half stays anchored so its index remains usable.
        Assert.Contains("\"NormalizedCustomerPhone\" LIKE ", sql, StringComparison.Ordinal);
        Assert.Contains("\"ReversedCustomerPhone\" LIKE ", sql, StringComparison.Ordinal);
        Assert.Contains(" OR ", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("UNION", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LIKE '%", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Phone_search_reverses_the_term_rather_than_the_column()
    {
        var sql = BuildSql(new OrderCustomerPhoneMatchesSpecification("4567"));

        // reverse() applied to the column would be correct and unindexable.
        Assert.DoesNotContain("reverse(", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Order_code_search_anchors_the_wildcard_at_the_end()
    {
        var sql = BuildSql(new OrderCodeStartsWithSpecification("ORD-0000001"));

        _output.WriteLine(sql);
        Assert.Contains("\"OrderCode\" LIKE ", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("LIKE '%", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Customer_name_search_uses_ilike_against_the_order_snapshot()
    {
        var sql = BuildSql(new OrderCustomerNameContainsSpecification("nguyen"));

        _output.WriteLine(sql);
        Assert.Contains("ILIKE", sql, StringComparison.Ordinal);
        Assert.Contains("\"CustomerName\"", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Phone_search_never_joins_the_customer_table()
    {
        var sql = BuildSql(new OrderCustomerPhoneMatchesSpecification("0901"));

        Assert.DoesNotContain("customers", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSql(Specification<Order> specification)
    {
        // A connection string is required to pick the provider, never to open a socket:
        // ToQueryString only compiles the expression tree.
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql("Host=localhost;Database=sql-shape-only")
            .Options;
        using var context = new SalesDbContext(options, new SqlTestExecutionContext());
        return context.Orders.AsNoTracking().Where(specification.ToExpression()).ToQueryString();
    }

    private sealed class SqlTestExecutionContext : IExecutionContext
    {
        public string Actor => "sql-shape-test";

        public Guid CorrelationId => Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    }
}

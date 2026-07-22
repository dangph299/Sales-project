using Microsoft.EntityFrameworkCore;
using Sales.Application.Common.Interfaces;
using Sales.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Pins the SQL the phone search specifications generate.
/// </summary>
/// <remarks>
/// EF Core builds this SQL without opening a connection, so unlike the execution-plan tests this
/// runs everywhere. It guards the property the indexes depend on: both phone searches must compile
/// to a <c>LIKE 'digits%'</c> with the wildcard only at the end. A refactor that reintroduced
/// <c>Contains</c> or <c>EndsWith</c> would still return correct rows and would still pass a
/// behavioural test, while quietly turning every phone search into a sequential scan.
/// </remarks>
public sealed class OrderCustomerSearchSqlTests
{
    private readonly ITestOutputHelper _output;

    public OrderCustomerSearchSqlTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Phone_prefix_search_anchors_the_wildcard_at_the_end()
    {
        var sql = BuildSql(new OrderCustomerPhoneMatchesSpecification("090123", OrderCustomerPhoneMatchMode.Prefix));

        _output.WriteLine(sql);
        Assert.Contains("\"NormalizedCustomerPhone\" LIKE ", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("\"ReversedCustomerPhone\" LIKE ", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("LIKE '%", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Phone_suffix_search_reads_the_reversed_column_instead_of_a_leading_wildcard()
    {
        var sql = BuildSql(new OrderCustomerPhoneMatchesSpecification("4567", OrderCustomerPhoneMatchMode.Suffix));

        _output.WriteLine(sql);
        Assert.Contains("\"ReversedCustomerPhone\" LIKE ", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("\"NormalizedCustomerPhone\" LIKE ", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("LIKE '%", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_code_search_anchors_the_wildcard_at_the_end()
    {
        var sql = BuildSql(new OrderCodeStartsWithSpecification("ORD001"));

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

    [Theory]
    [InlineData(OrderCustomerPhoneMatchMode.Prefix)]
    [InlineData(OrderCustomerPhoneMatchMode.Suffix)]
    public void Phone_search_never_joins_the_customer_table(OrderCustomerPhoneMatchMode customerPhoneMatchMode)
    {
        var sql = BuildSql(new OrderCustomerPhoneMatchesSpecification("0901", customerPhoneMatchMode));

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

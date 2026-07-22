using Microsoft.EntityFrameworkCore;
using Sales.Application.Common.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Checks that the order and customer searches can use their indexes rather than scanning.
/// </summary>
[Trait("Category", "Reliability")]
[Collection("SalesReliabilityPostgres")]
public sealed class OrderPhoneIndexPlanPostgresTests
{
    // Enough rows that the planner prefers an index to a sequential scan. On a small table it would
    // correctly choose a seq scan however good the index is, and the test would say nothing.
    private const int SeededRowCount = 5_000;

    private readonly PostgresReliabilityFixture _fixture;
    private readonly ITestOutputHelper _output;

    public OrderPhoneIndexPlanPostgresTests(PostgresReliabilityFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [SkippableFact]
    public async Task Order_phone_prefix_search_can_use_the_normalized_phone_index()
    {
        await using var context = await SeedAsync();

        var plan = await ExplainAsync(
            context,
            """
            SELECT "Id" FROM orders WHERE "NormalizedCustomerPhone" LIKE '09000014%'
            """);

        AssertIndexScan(plan, "IX_orders_NormalizedCustomerPhone");
    }

    [SkippableFact]
    public async Task Order_phone_suffix_search_can_use_the_reversed_phone_index()
    {
        await using var context = await SeedAsync();

        var plan = await ExplainAsync(
            context,
            """
            SELECT "Id" FROM orders WHERE "ReversedCustomerPhone" LIKE '0041%'
            """);

        AssertIndexScan(plan, "IX_orders_ReversedCustomerPhone");
    }

    [SkippableFact]
    public async Task Searching_both_ends_at_once_can_use_both_phone_indexes()
    {
        await using var context = await SeedAsync();

        // The shape the order search actually runs now that the user is not asked which end they
        // remembered. The risk of an OR is that the planner gives up and scans the table once
        // instead of combining the two indexes.
        var plan = await ExplainAsync(
            context,
            """
            SELECT "Id" FROM orders
            WHERE "NormalizedCustomerPhone" LIKE '09000014%' OR "ReversedCustomerPhone" LIKE '0041%'
            """);

        _output.WriteLine(plan);
        Assert.Contains("IX_orders_NormalizedCustomerPhone", plan, StringComparison.Ordinal);
        Assert.Contains("IX_orders_ReversedCustomerPhone", plan, StringComparison.Ordinal);
        Assert.Contains("BitmapOr", plan, StringComparison.Ordinal);
        Assert.DoesNotContain("Seq Scan", plan, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task Order_code_search_can_use_the_order_code_index()
    {
        await using var context = await SeedAsync();

        var plan = await ExplainAsync(
            context,
            """
            SELECT "Id" FROM orders WHERE "OrderCode" LIKE 'ORD-00014%'
            """);

        AssertIndexScan(plan, "IX_orders_OrderCode");
    }

    [SkippableFact]
    public async Task Customer_phone_prefix_lookup_can_use_the_normalized_phone_index()
    {
        await using var context = await SeedAsync();

        // The NOT "IsDelete" predicate is what keeps the partial index eligible; without it the
        // planner cannot prove the query is covered.
        var plan = await ExplainAsync(
            context,
            """
            SELECT "Id" FROM customers
            WHERE "NormalizedPhone" LIKE '09000014%' AND NOT "IsDelete"
            """);

        AssertIndexScan(plan, "IX_customers_NormalizedPhone");
    }

    [SkippableFact]
    public async Task Customer_phone_suffix_lookup_can_use_the_reversed_phone_index()
    {
        await using var context = await SeedAsync();

        var plan = await ExplainAsync(
            context,
            """
            SELECT "Id" FROM customers
            WHERE "ReversedPhone" LIKE '0041%' AND NOT "IsDelete"
            """);

        AssertIndexScan(plan, "IX_customers_ReversedPhone");
    }

    private void AssertIndexScan(string plan, string expectedIndexName)
    {
        _output.WriteLine(plan);

        Assert.Contains(expectedIndexName, plan, StringComparison.Ordinal);
        Assert.True(
            plan.Contains("Index Scan", StringComparison.Ordinal)
            || plan.Contains("Index Only Scan", StringComparison.Ordinal)
            || plan.Contains("Bitmap Index Scan", StringComparison.Ordinal),
            $"Expected an index scan on {expectedIndexName}, but the planner chose:{Environment.NewLine}{plan}");
    }

    private static async Task<string> ExplainAsync(SalesDbContext context, string sql)
    {
        // Concatenated rather than interpolated: EXPLAIN cannot take the statement as a parameter,
        // and every caller below passes a literal written in this file.
        var explainStatement = "EXPLAIN (ANALYZE, BUFFERS) " + sql;
        var planLines = await context.Database
            .SqlQueryRaw<string>(explainStatement)
            .ToListAsync();
        return string.Join(Environment.NewLine, planLines);
    }

    private async Task<SalesDbContext> SeedAsync()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var options = new DbContextOptionsBuilder<SalesDbContext>().UseNpgsql(_fixture.ConnectionString).Options;
        var context = new SalesDbContext(options, new PlanTestExecutionContext());
        await context.Database.MigrateAsync();
        await context.Database.ExecuteSqlRawAsync("DELETE FROM order_lines");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM orders");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM customers");

        // Seeded in SQL rather than through the aggregates: this test is about the planner, and
        // five thousand round trips through EF would dominate its runtime for no benefit.
        await context.Database.ExecuteSqlRawAsync(
            $"""
            INSERT INTO customers ("Id","CustomerCode","Name","Phone","NormalizedPhone","ReversedPhone","Status","CreatedAt","UpdatedAt","IsDelete","Version")
            SELECT gen_random_uuid(),
                   'CUS' || lpad(sequence_number::text, 7, '0'),
                   'Customer ' || sequence_number,
                   '09' || lpad(sequence_number::text, 8, '0'),
                   '09' || lpad(sequence_number::text, 8, '0'),
                   reverse('09' || lpad(sequence_number::text, 8, '0')),
                   'Normal', now(), now(), false, 1
            FROM generate_series(1, {SeededRowCount}) AS sequence_number
            """);

        await context.Database.ExecuteSqlRawAsync(
            $"""
            INSERT INTO orders ("Id","OrderCode","CustomerId","CustomerName","CustomerPhone","NormalizedCustomerPhone","ReversedCustomerPhone","CreatedAt","UpdatedAt","Status","Version")
            SELECT gen_random_uuid(),
                   'ORD-' || lpad(sequence_number::text, 7, '0'),
                   gen_random_uuid(),
                   'Customer ' || sequence_number,
                   '09' || lpad(sequence_number::text, 8, '0'),
                   '09' || lpad(sequence_number::text, 8, '0'),
                   reverse('09' || lpad(sequence_number::text, 8, '0')),
                   now(), now(), 'Draft', 1
            FROM generate_series(1, {SeededRowCount}) AS sequence_number
            """);

        // Without fresh statistics the planner works from defaults and its choice means nothing.
        await context.Database.ExecuteSqlRawAsync("ANALYZE orders");
        await context.Database.ExecuteSqlRawAsync("ANALYZE customers");
        return context;
    }

    private sealed class PlanTestExecutionContext : IExecutionContext
    {
        public string Actor => "integration-test";

        public Guid CorrelationId => Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    }
}

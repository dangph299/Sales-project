using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Sales.Application.Common.Interfaces;
using Xunit;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Proves the snapshot migration either backfills legacy orders correctly or refuses to run.
/// </summary>
/// <remarks>
/// The refusal cases matter more than the happy path. Truncating an over-long phone number would
/// silently file two different customers' orders under one search value, and blanking an
/// unparseable one would drop the order out of every phone search without anyone noticing, so the
/// migration must stop and say so instead. Only a real PostgreSQL instance can establish this: the
/// backfill is raw SQL and the failure is a <c>RAISE EXCEPTION</c>.
/// </remarks>
[Trait("Category", "Reliability")]
[Collection("SalesReliabilityPostgres")]
public sealed class OrderCustomerSnapshotMigrationPostgresTests
{
    /// <summary>The migration immediately before the one under test.</summary>
    private const string PreviousMigration = "20260721045911_SellThroughDiscontinuedOrderLine";

    private const string SnapshotMigration = "20260722040626_AddOrderCustomerSnapshotAndOrderCode";

    private readonly PostgresReliabilityFixture _fixture;

    public OrderCustomerSnapshotMigrationPostgresTests(PostgresReliabilityFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Backfill_derives_the_normalized_and_reversed_phone_from_the_legacy_value()
    {
        await using var context = await MigrateToPreviousAsync();
        await InsertLegacyOrderAsync(context, "0901234567");

        await Migrator(context)
            .MigrateAsync(SnapshotMigration);

        var backfilled = await ReadBackfilledOrderAsync(context);
        Assert.Equal("0901234567", backfilled.NormalizedCustomerPhone);
        Assert.Equal("7654321090", backfilled.ReversedCustomerPhone);
    }

    [SkippableFact]
    public async Task Backfill_assigns_order_codes_in_creation_order_and_seeds_the_sequence_past_them()
    {
        await using var context = await MigrateToPreviousAsync();
        await InsertLegacyOrderAsync(context, "0901234567", createdAt: DateTimeOffset.UtcNow.AddDays(-2));
        await InsertLegacyOrderAsync(context, "0912345678", createdAt: DateTimeOffset.UtcNow.AddDays(-1));

        await Migrator(context)
            .MigrateAsync(SnapshotMigration);

        var orderCodes = await context.Database
            .SqlQuery<string>($"SELECT \"OrderCode\" AS \"Value\" FROM orders ORDER BY \"CreatedAt\"")
            .ToListAsync();
        Assert.Equal(["ORD001", "ORD002"], orderCodes);

        // The next generated code must not collide with a backfilled one.
        var nextSequenceNumber = await context.Database
            .SqlQuery<long>($"SELECT nextval('order_code_seq') AS \"Value\"")
            .SingleAsync();
        Assert.Equal(3, nextSequenceNumber);
    }

    [SkippableTheory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345678")]
    [InlineData("1234567890123456")]
    [InlineData("not-a-phone")]
    public async Task Migration_refuses_to_run_against_a_phone_it_cannot_normalize(string? legacyCustomerPhone)
    {
        await using var context = await MigrateToPreviousAsync();
        await InsertLegacyOrderAsync(context, legacyCustomerPhone);

        var failure = await Assert.ThrowsAsync<PostgresException>(() =>
            Migrator(context).MigrateAsync(SnapshotMigration));

        Assert.Contains("does not normalize to 9-15 digits", failure.MessageText, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task A_refused_migration_leaves_the_legacy_phone_exactly_as_it_was()
    {
        await using var context = await MigrateToPreviousAsync();
        await InsertLegacyOrderAsync(context, "1234567890123456");

        await Assert.ThrowsAsync<PostgresException>(() =>
            Migrator(context).MigrateAsync(SnapshotMigration));

        // Neither truncated to fifteen digits nor blanked.
        var legacyCustomerPhone = await context.Database
            .SqlQuery<string>($"SELECT \"CustomerPhone\" AS \"Value\" FROM orders")
            .SingleAsync();
        Assert.Equal("1234567890123456", legacyCustomerPhone);
    }

    private static IMigrator Migrator(SalesDbContext context)
    {
        return context.Database.GetService<IMigrator>();
    }

    private async Task<SalesDbContext> MigrateToPreviousAsync()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        var options = new DbContextOptionsBuilder<SalesDbContext>().UseNpgsql(_fixture.ConnectionString).Options;
        var context = new SalesDbContext(options, new MigrationTestExecutionContext());

        // Start from an empty database so the migration under test runs for real rather than being
        // reported as already applied.
        await context.Database.EnsureDeletedAsync();
        await Migrator(context).MigrateAsync(PreviousMigration);
        return context;
    }

    private static async Task InsertLegacyOrderAsync(
        SalesDbContext context,
        string? legacyCustomerPhone,
        DateTimeOffset? createdAt = null)
    {
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO orders ("Id","CustomerId","CustomerName","CustomerPhone","CreatedAt","UpdatedAt","Status","Version")
            VALUES (gen_random_uuid(), gen_random_uuid(), 'Legacy Customer', {legacyCustomerPhone},
                    {createdAt ?? DateTimeOffset.UtcNow}, now(), 'Draft', 1)
            """);
    }

    private static async Task<(string NormalizedCustomerPhone, string ReversedCustomerPhone)> ReadBackfilledOrderAsync(
        SalesDbContext context)
    {
        var normalizedCustomerPhone = await context.Database
            .SqlQuery<string>($"SELECT \"NormalizedCustomerPhone\" AS \"Value\" FROM orders")
            .SingleAsync();
        var reversedCustomerPhone = await context.Database
            .SqlQuery<string>($"SELECT \"ReversedCustomerPhone\" AS \"Value\" FROM orders")
            .SingleAsync();
        return (normalizedCustomerPhone, reversedCustomerPhone);
    }

    private sealed class MigrationTestExecutionContext : IExecutionContext
    {
        public string Actor => "integration-test";

        public Guid CorrelationId => Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Sales.Application.Common.Interfaces;
using Xunit;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Covers the snapshot migration: it either backfills legacy orders correctly or refuses to run and
/// leaves the data untouched.
/// </summary>
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
        Assert.Equal(["ORD-0000001", "ORD-0000002"], orderCodes);

        // The next generated code must not collide with a backfilled one: two legacy orders leave
        // the next order at ORD-0000003, the same way 532 would leave it at ORD-0000533.
        Assert.Equal("ORD-0000003", await NextGeneratedOrderCodeAsync(context));
    }

    [SkippableFact]
    public async Task First_order_code_on_an_empty_database_is_ORD_0000001()
    {
        await using var context = await MigrateToPreviousAsync();

        await Migrator(context).MigrateAsync(SnapshotMigration);

        // Nothing to backfill, so the sequence must be left untouched at its start rather than
        // seeded past a count of zero.
        Assert.Equal("ORD-0000001", await NextGeneratedOrderCodeAsync(context));
    }

    [SkippableFact]
    public async Task Orders_created_in_the_same_instant_are_numbered_by_id()
    {
        await using var context = await MigrateToPreviousAsync();
        var sharedCreatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var laterId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var earlierId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        await InsertLegacyOrderAsync(context, "0901234567", createdAt: sharedCreatedAt, id: laterId);
        await InsertLegacyOrderAsync(context, "0912345678", createdAt: sharedCreatedAt, id: earlierId);

        await Migrator(context).MigrateAsync(SnapshotMigration);

        // CreatedAt alone leaves the order of these two up to the storage layer, which would make
        // the codes differ between a rehearsal run and the real one. Id breaks the tie.
        var orderCodesById = await context.Database
            .SqlQuery<string>($"SELECT \"OrderCode\" AS \"Value\" FROM orders ORDER BY \"Id\"")
            .ToListAsync();
        Assert.Equal(["ORD-0000001", "ORD-0000002"], orderCodesById);
    }

    private static async Task<string> NextGeneratedOrderCodeAsync(SalesDbContext context)
    {
        var generator = new OrderCodeGenerator(new SequentialCodeGenerator(context));
        return await generator.NextCodeAsync(CancellationToken.None);
    }

    // NULL is not among these: the pre-migration schema already declares orders.CustomerPhone NOT
    // NULL, so a null phone is a row the database would refuse long before this migration ran.
    [SkippableTheory]
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

        var options = new DbContextOptionsBuilder<SalesDbContext>().UseNpgsql(MigrationDatabase()).Options;
        var context = new SalesDbContext(options, new MigrationTestExecutionContext());

        // Start from an empty database so the migration under test runs for real rather than being
        // reported as already applied.
        await context.Database.EnsureDeletedAsync();
        await Migrator(context).MigrateAsync(PreviousMigration);
        return context;
    }

    /// <summary>
    /// Builds a connection string for this class's own database.
    /// </summary>
    /// <remarks>
    /// Half of these tests deliberately leave the schema one migration behind, holding a row the
    /// next migration refuses to touch. Sharing the suite's database would hand every other test a
    /// schema that can no longer be migrated forward, so the migration tests get a database of
    /// their own. Each test drops and recreates it, so nothing carries over between them either.
    /// </remarks>
    private string MigrationDatabase()
    {
        var suiteConnectionString = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString);
        return new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            Database = suiteConnectionString.Database + "_migration"
        }.ConnectionString;
    }

    private static async Task InsertLegacyOrderAsync(
        SalesDbContext context,
        string? legacyCustomerPhone,
        DateTimeOffset? createdAt = null,
        Guid? id = null)
    {
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO orders ("Id","CustomerId","CustomerName","CustomerPhone","CreatedAt","UpdatedAt","Status","Version")
            VALUES ({id ?? Guid.NewGuid()}, gen_random_uuid(), 'Legacy Customer', {legacyCustomerPhone},
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

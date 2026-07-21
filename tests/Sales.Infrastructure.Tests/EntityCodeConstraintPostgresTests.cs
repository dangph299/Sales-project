using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Sales.Application.Common.Interfaces;
using Xunit;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Proves the database rejects duplicate business codes on its own. The sequence already makes
/// duplicates impossible in normal operation, so these constraints exist as the last line of
/// defence against anything that writes without going through the generator.
/// </summary>
[Trait("Category", "Reliability")]
[Collection("SalesReliabilityPostgres")]
public sealed class EntityCodeConstraintPostgresTests
{
    private readonly PostgresReliabilityFixture _fixture;

    public EntityCodeConstraintPostgresTests(PostgresReliabilityFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Duplicate_product_code_is_rejected_by_the_database()
    {
        await AssertDuplicateIsRejectedAsync(
            "products",
            """
            INSERT INTO products ("Id","ProductCode","Name","CategoryId","Status","CreatedAt","UpdatedAt","IsDelete","Version")
            VALUES (gen_random_uuid(),'PRD777','duplicate probe',gen_random_uuid(),'Draft',now(),now(),false,1)
            """);
    }

    [SkippableFact]
    public async Task Duplicate_category_code_is_rejected_by_the_database()
    {
        await AssertDuplicateIsRejectedAsync(
            "categories",
            """
            INSERT INTO categories ("Id","CategoryCode","Name","SortOrder","Status","CreatedAt","UpdatedAt","IsDelete","Version")
            VALUES (gen_random_uuid(),'CAT777',concat('duplicate probe ', gen_random_uuid()),1,'Draft',now(),now(),false,1)
            """);
    }

    [SkippableFact]
    public async Task Duplicate_customer_code_is_rejected_by_the_database()
    {
        await AssertDuplicateIsRejectedAsync(
            "customers",
            """
            INSERT INTO customers ("Id","CustomerCode","Name","Phone","NormalizedPhone","ReversedPhone","Status","CreatedAt","UpdatedAt","IsDelete","Version")
            VALUES (gen_random_uuid(),'CUS777','duplicate probe',concat('090', substr(gen_random_uuid()::text, 1, 7)),
                    concat('090', substr(gen_random_uuid()::text, 1, 7)), '', 'Normal', now(), now(), false, 1)
            """);
    }

    private async Task AssertDuplicateIsRejectedAsync(string tableName, string insertSql)
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync($"DELETE FROM {tableName}");

        // The first insert establishes the code; only the second one must be refused, and by the
        // database rather than by any application-level check.
        await db.Database.ExecuteSqlRawAsync(insertSql);

        var duplicateInsert = await Assert.ThrowsAsync<PostgresException>(
            () => db.Database.ExecuteSqlRawAsync(insertSql));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicateInsert.SqlState);
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<SalesDbContext>(options => options.UseNpgsql(_fixture.ConnectionString));
        services.AddSingleton<IExecutionContext, TestExecutionContext>();
        return services.BuildServiceProvider();
    }

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string Actor => "integration-test";

        public Guid CorrelationId => Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    }
}

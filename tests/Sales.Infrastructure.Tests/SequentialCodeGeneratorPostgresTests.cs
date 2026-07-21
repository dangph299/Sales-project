using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sales.Application.Common.Interfaces;
using Sales.Application.Features.Customers.Interfaces;
using Sales.Application.Features.Products.Interfaces;
using Xunit;

namespace Sales.Infrastructure.Tests;

/// <summary>
/// Exercises the backend-assigned business codes against a real PostgreSQL, because the allocation
/// depends on <c>nextval</c>: no in-memory or SQLite provider can prove that concurrent callers
/// receive distinct numbers.
/// </summary>
[Trait("Category", "Reliability")]
[Collection("SalesReliabilityPostgres")]
public sealed class SequentialCodeGeneratorPostgresTests
{
    private readonly PostgresReliabilityFixture _fixture;

    public SequentialCodeGeneratorPostgresTests(PostgresReliabilityFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableTheory]
    [InlineData("CUS")]
    [InlineData("PRD")]
    [InlineData("CAT")]
    public async Task First_code_of_a_reset_sequence_is_number_one(string prefix)
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var provider = BuildProvider();
        var codeSequence = SequenceForPrefix(prefix);
        await MigrateAndRestartSequenceAsync(provider, codeSequence, 1);

        var code = await AllocateAsync(provider, codeSequence);

        Assert.Equal($"{prefix}001", code);
    }

    [SkippableFact]
    public async Task Code_after_seventeen_is_eighteen_and_is_padded_to_three_digits()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var provider = BuildProvider();
        await MigrateAndRestartSequenceAsync(provider, EntityCodeSequence.Product, 18);

        var code = await AllocateAsync(provider, EntityCodeSequence.Product);

        Assert.Equal("PRD018", code);
    }

    [SkippableFact]
    public async Task Sequence_runs_past_nine_hundred_ninety_nine_into_four_digits()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var provider = BuildProvider();
        await MigrateAndRestartSequenceAsync(provider, EntityCodeSequence.Product, 999);

        var lastThreeDigitCode = await AllocateAsync(provider, EntityCodeSequence.Product);
        var firstFourDigitCode = await AllocateAsync(provider, EntityCodeSequence.Product);

        Assert.Equal("PRD999", lastThreeDigitCode);
        Assert.Equal("PRD1000", firstFourDigitCode);
    }

    [SkippableFact]
    public async Task Each_prefix_advances_independently()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var provider = BuildProvider();
        foreach (var codeSequence in EntityCodeSequence.All)
        {
            await MigrateAndRestartSequenceAsync(provider, codeSequence, 1);
        }

        await AllocateAsync(provider, EntityCodeSequence.Product);
        await AllocateAsync(provider, EntityCodeSequence.Product);

        var customerCode = await AllocateAsync(provider, EntityCodeSequence.Customer);
        var categoryCode = await AllocateAsync(provider, EntityCodeSequence.Category);

        Assert.Equal("CUS001", customerCode);
        Assert.Equal("CAT001", categoryCode);
    }

    [SkippableFact]
    public async Task Malformed_and_foreign_prefixed_codes_in_the_table_do_not_affect_allocation()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var provider = BuildProvider();
        await MigrateAndRestartSequenceAsync(provider, EntityCodeSequence.Product, 5);

        // The generator never reads the business table, so even codes that could not be parsed are
        // irrelevant to it. Inserting them proves allocation does not consult them.
        await ExecuteAsync(
            provider,
            """
            INSERT INTO products ("Id","ProductCode","Name","CategoryId","Status","CreatedAt","UpdatedAt","IsDelete","Version")
            VALUES (gen_random_uuid(),'PRDABC','malformed',gen_random_uuid(),'Draft',now(),now(),false,1),
                   (gen_random_uuid(),'PRD999999999999999','overflowing',gen_random_uuid(),'Draft',now(),now(),false,1),
                   (gen_random_uuid(),'XYZ777','foreign prefix',gen_random_uuid(),'Draft',now(),now(),false,1)
            """);

        var code = await AllocateAsync(provider, EntityCodeSequence.Product);

        Assert.Equal("PRD005", code);
    }

    [SkippableFact]
    public async Task Concurrent_allocations_never_hand_out_the_same_code()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        const int concurrentRequests = 50;
        await using var provider = BuildProvider();
        await MigrateAndRestartSequenceAsync(provider, EntityCodeSequence.Product, 1);

        // Each allocation runs in its own scope, so each uses its own DbContext and connection —
        // the same shape as concurrent HTTP requests, including across API instances.
        var allocations = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                await using var scope = provider.CreateAsyncScope();
                var generator = scope.ServiceProvider.GetRequiredService<IProductCodeGenerator>();
                return await generator.NextCodeAsync(CancellationToken.None);
            }));

        var codes = await Task.WhenAll(allocations);

        Assert.Equal(concurrentRequests, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            Enumerable.Range(1, concurrentRequests).Select(number => $"PRD{number:D3}").OrderBy(code => code, StringComparer.Ordinal),
            codes.OrderBy(code => code, StringComparer.Ordinal));
    }

    [SkippableFact]
    public async Task Allocated_code_is_not_reused_after_the_row_is_hard_deleted()
    {
        Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

        await using var provider = BuildProvider();
        await MigrateAndRestartSequenceAsync(provider, EntityCodeSequence.Product, 17);

        var seventeen = await AllocateAsync(provider, EntityCodeSequence.Product);
        var eighteen = await AllocateAsync(provider, EntityCodeSequence.Product);
        await ExecuteAsync(provider, $"DELETE FROM products WHERE \"ProductCode\" = '{eighteen}'");

        var afterDelete = await AllocateAsync(provider, EntityCodeSequence.Product);

        Assert.Equal("PRD017", seventeen);
        Assert.Equal("PRD018", eighteen);
        Assert.Equal("PRD019", afterDelete);
    }

    private static EntityCodeSequence SequenceForPrefix(string prefix)
    {
        return EntityCodeSequence.All.Single(codeSequence => codeSequence.Prefix == prefix);
    }

    private static async Task<string> AllocateAsync(ServiceProvider provider, EntityCodeSequence codeSequence)
    {
        await using var scope = provider.CreateAsyncScope();
        var generator = scope.ServiceProvider.GetRequiredService<SequentialCodeGenerator>();
        return await generator.NextCodeAsync(codeSequence, CancellationToken.None);
    }

    private static async Task ExecuteAsync(ServiceProvider provider, string sql)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task MigrateAndRestartSequenceAsync(
        ServiceProvider provider,
        EntityCodeSequence codeSequence,
        long nextValue)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        await db.Database.MigrateAsync();
        await db.Products.ExecuteDeleteAsync();
        await db.Database.ExecuteSqlRawAsync(
            $"ALTER SEQUENCE {codeSequence.SequenceName} RESTART WITH {nextValue}");
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<SalesDbContext>(options => options.UseNpgsql(_fixture.ConnectionString));
        services.AddSingleton<IExecutionContext, TestExecutionContext>();
        services.AddScoped<SequentialCodeGenerator>();
        services.AddScoped<ICustomerCodeGenerator, CustomerCodeGenerator>();
        services.AddScoped<IProductCodeGenerator, ProductCodeGenerator>();
        services.AddScoped<ICategoryCodeGenerator, CategoryCodeGenerator>();
        return services.BuildServiceProvider();
    }

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string Actor => "integration-test";

        public Guid CorrelationId => Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    }
}

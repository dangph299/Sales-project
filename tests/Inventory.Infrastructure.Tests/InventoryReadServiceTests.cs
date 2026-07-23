using System.Data.Common;
using Inventory.Domain;
using Mapster;
using MapsterMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Inventory.Infrastructure.Tests;

public sealed class InventoryReadServiceTests
{
    [Fact]
    public async Task Batch_read_returns_requested_inventory_and_zero_snapshots_for_missing_rows()
    {
        var existingId = Guid.NewGuid();
        var missingId = Guid.NewGuid();
        await using var fixture = await SqliteInventoryFixture.CreateAsync();
        await fixture.SeedAsync(InventoryItem.Create(existingId, "sku-1", 10));

        var service = new InventoryReadService(fixture.CreateContext(), CreateMapper());

        var result = await service.GetByProductVariantIdsAsync([existingId, missingId]);

        Assert.Equal([existingId, missingId], result.Select(x => x.ProductId).ToArray());
        Assert.Equal(10, result.Single(x => x.ProductId == existingId).Available);
        var missing = result.Single(x => x.ProductId == missingId);
        Assert.Equal(0, missing.Available);
        Assert.Equal(0, missing.Reserved);
        Assert.Equal(0, missing.Version);
    }

    [Fact]
    public async Task Batch_read_deduplicates_ids_and_does_not_return_unrequested_inventory()
    {
        var requestedId = Guid.NewGuid();
        var unrequestedId = Guid.NewGuid();
        await using var fixture = await SqliteInventoryFixture.CreateAsync();
        await fixture.SeedAsync(
            InventoryItem.Create(requestedId, "sku-1", 10),
            InventoryItem.Create(unrequestedId, "sku-2", 20));

        var service = new InventoryReadService(fixture.CreateContext(), CreateMapper());

        var result = await service.GetByProductVariantIdsAsync([requestedId, requestedId]);

        Assert.Single(result);
        Assert.Equal(requestedId, result.Single().ProductId);
    }

    [Fact]
    public async Task Empty_batch_returns_no_items_without_querying_database()
    {
        await using var fixture = await SqliteInventoryFixture.CreateAsync();
        var service = new InventoryReadService(fixture.CreateContext(), CreateMapper());

        var result = await service.GetByProductVariantIdsAsync([]);

        Assert.Empty(result);
        Assert.Equal(0, fixture.CommandCounter.Count);
    }

    [Fact]
    public async Task Batch_read_uses_one_database_query_for_many_ids()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        await using var fixture = await SqliteInventoryFixture.CreateAsync();
        await fixture.SeedAsync(
            InventoryItem.Create(firstId, "sku-1", 10),
            InventoryItem.Create(secondId, "sku-2", 20));
        fixture.CommandCounter.Reset();
        var service = new InventoryReadService(fixture.CreateContext(), CreateMapper());

        var result = await service.GetByProductVariantIdsAsync([firstId, secondId]);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, fixture.CommandCounter.Count);
    }

    private static IMapper CreateMapper()
    {
        return new Mapper(new TypeAdapterConfig());
    }

    private sealed class SqliteInventoryFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<InventoryDbContext> _options;

        private SqliteInventoryFixture(
            SqliteConnection connection,
            DbContextOptions<InventoryDbContext> options,
            CountingCommandInterceptor commandCounter)
        {
            _connection = connection;
            _options = options;
            CommandCounter = commandCounter;
        }

        public CountingCommandInterceptor CommandCounter { get; }

        public static async Task<SqliteInventoryFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var commandCounter = new CountingCommandInterceptor();
            var options = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(commandCounter)
                .Options;
            var fixture = new SqliteInventoryFixture(connection, options, commandCounter);
            await using var context = fixture.CreateContext();
            await context.Database.EnsureCreatedAsync();
            commandCounter.Reset();
            return fixture;
        }

        public InventoryDbContext CreateContext()
        {
            return new InventoryDbContext(_options);
        }

        public async Task SeedAsync(params InventoryItem[] items)
        {
            await using var context = CreateContext();
            context.Items.AddRange(items);
            await context.SaveChangesAsync();
            CommandCounter.Reset();
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }

    private sealed class CountingCommandInterceptor : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public void Reset()
        {
            Count = 0;
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Count++;
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}

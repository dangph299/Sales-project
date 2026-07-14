using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sales.Application;
using Sales.Domain;

namespace Sales.Infrastructure.Tests;

public sealed class CachedProductReadServiceTests
{
    [Fact]
    public async Task Get_returns_valid_cached_product_without_calling_inner_service()
    {
        var product = ProductDto(Guid.NewGuid(), isActive: true, isDelete: false);
        var cache = new RecordingProductCache(product);
        var service = new CachedProductReadService(CreateUnavailableInner(), cache);

        var result = await service.GetAsync(product.Id);

        Assert.Same(product, result);
        Assert.False(cache.Removed);
        Assert.Empty(cache.Stored);
    }

    [Fact]
    public async Task Get_on_cache_miss_calls_inner_service_and_caches_valid_result()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var product = Product.Create("sku-active", "Keyboard", 100);
        await fixture.SeedAsync(product);
        var cache = new RecordingProductCache();
        var service = new CachedProductReadService(new ProductReadService(fixture.CreateContext()), cache);

        var result = await service.GetAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal([product.Id], cache.Stored.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task Get_removes_inactive_cached_product_and_falls_back_to_inner_service()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var product = Product.Create("sku-active", "Keyboard", 100);
        await fixture.SeedAsync(product);
        var stale = ProductDto(product.Id, isActive: false, isDelete: false);
        var cache = new RecordingProductCache(stale);
        var service = new CachedProductReadService(new ProductReadService(fixture.CreateContext()), cache);

        var result = await service.GetAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal(product.Id, cache.RemovedId);
        Assert.Equal([product.Id], cache.Stored.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task Get_removes_deleted_cached_product_and_falls_back_to_inner_service()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var product = Product.Create("sku-active", "Keyboard", 100);
        await fixture.SeedAsync(product);
        var stale = ProductDto(product.Id, isActive: true, isDelete: true);
        var cache = new RecordingProductCache(stale);
        var service = new CachedProductReadService(new ProductReadService(fixture.CreateContext()), cache);

        var result = await service.GetAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal(product.Id, cache.RemovedId);
        Assert.Equal([product.Id], cache.Stored.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task Get_returns_active_database_product_when_cache_contains_stale_inactive_version()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var product = Product.Create("sku-active", "Keyboard", 100);
        await fixture.SeedAsync(product);
        var stale = ProductDto(product.Id, isActive: false, isDelete: false);
        var cache = new RecordingProductCache(stale);
        var service = new CachedProductReadService(new ProductReadService(fixture.CreateContext()), cache);

        var result = await service.GetAsync(product.Id);

        Assert.NotNull(result);
        Assert.True(result.IsActive);
        Assert.False(result.IsDelete);
        Assert.Equal(product.Id, result.Id);
    }

    [Fact]
    public async Task Get_returns_null_when_cache_is_stale_and_database_has_no_active_product()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var id = Guid.NewGuid();
        var stale = ProductDto(id, isActive: false, isDelete: false);
        var cache = new RecordingProductCache(stale);
        var service = new CachedProductReadService(new ProductReadService(fixture.CreateContext()), cache);

        var result = await service.GetAsync(id);

        Assert.Null(result);
        Assert.Equal(id, cache.RemovedId);
        Assert.Empty(cache.Stored);
    }

    [Fact]
    public async Task Get_does_not_cache_null_results()
    {
        await using var fixture = await SqliteSalesFixture.CreateAsync();
        var cache = new RecordingProductCache();
        var service = new CachedProductReadService(new ProductReadService(fixture.CreateContext()), cache);

        var result = await service.GetAsync(Guid.NewGuid());

        Assert.Null(result);
        Assert.Empty(cache.Stored);
    }

    private static ProductDto ProductDto(Guid id, bool isActive, bool isDelete)
    {
        return new ProductDto(id, "SKU", "Keyboard", 100, isActive, 1, DateTimeOffset.UtcNow, isDelete, null, isDelete ? DateTimeOffset.UtcNow : null);
    }

    private static ProductReadService CreateUnavailableInner()
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        return new ProductReadService(new SalesDbContext(options, new TestExecutionContext()));
    }

    private sealed class RecordingProductCache(ProductDto? value = null) : IProductCache
    {
        public List<ProductDto> Stored { get; } = [];
        public bool Removed { get; private set; }
        public Guid? RemovedId { get; private set; }

        public Task<ProductDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(value is not null && value.Id == id ? value : null);
        }

        public Task SetAsync(ProductDto product, CancellationToken cancellationToken = default)
        {
            Stored.Add(product);
            value = product;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Removed = true;
            RemovedId = id;
            value = null;
            return Task.CompletedTask;
        }
    }

    private sealed class SqliteSalesFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<SalesDbContext> _options;
        private readonly TestExecutionContext _executionContext = new();

        private SqliteSalesFixture(SqliteConnection connection, DbContextOptions<SalesDbContext> options)
        {
            _connection = connection;
            _options = options;
        }

        public static async Task<SqliteSalesFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SalesDbContext>()
                .UseSqlite(connection)
                .Options;
            var fixture = new SqliteSalesFixture(connection, options);
            await using var context = fixture.CreateContext();
            await context.Database.EnsureCreatedAsync();
            return fixture;
        }

        public SalesDbContext CreateContext()
        {
            return new SalesDbContext(_options, _executionContext);
        }

        public async Task SeedAsync(params Product[] products)
        {
            await using var context = CreateContext();
            foreach (var product in products)
            {
                product.ClearDomainEvents();
            }

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }

    private sealed class TestExecutionContext : IExecutionContext
    {
        public string Actor => "test";
        public Guid CorrelationId => Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    }
}

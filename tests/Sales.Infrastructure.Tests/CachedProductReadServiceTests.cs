using BuildingBlocks.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var inner = new RecordingProductReadService();
        var service = new CachedProductReadService(inner, cache);

        var result = await service.GetAsync(product.Id);

        Assert.Same(product, result);
        Assert.Equal(0, inner.GetCalls);
        Assert.False(cache.Removed);
        Assert.Empty(cache.Stored);
    }

    [Fact]
    public async Task Get_on_cache_miss_calls_inner_service()
    {
        var product = ProductDto(Guid.NewGuid(), isActive: true, isDelete: false);
        var cache = new RecordingProductCache();
        var inner = new RecordingProductReadService(product);
        var service = new CachedProductReadService(inner, cache);

        var result = await service.GetAsync(product.Id);

        Assert.Same(product, result);
        Assert.Equal(1, inner.GetCalls);
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

    [Fact]
    public async Task Get_passes_cancellation_token_to_cache_and_inner_service()
    {
        var id = Guid.NewGuid();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cache = new RecordingProductCache();
        var inner = new RecordingProductReadService();
        var service = new CachedProductReadService(inner, cache);

        await service.GetAsync(id, cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, cache.LastGetToken);
        Assert.Equal(cancellationTokenSource.Token, inner.LastGetToken);
    }

    [Fact]
    public void Product_read_service_registration_resolves_decorator_without_circular_dependency()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sales"] = "Host=localhost;Database=sales_test;Username=test;Password=test",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["Kafka:Brokers:0"] = "localhost:9092"
            })
            .Build();

        services.AddSalesInfrastructure(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IProductReadService>();

        Assert.IsType<CachedProductReadService>(service);
    }

    private static ProductDto ProductDto(Guid id, bool isActive, bool isDelete)
    {
        return new ProductDto(id, "SKU", "Keyboard", 100, isActive, 1, DateTimeOffset.UtcNow, isDelete, null, isDelete ? DateTimeOffset.UtcNow : null);
    }

    private sealed class RecordingProductCache(ProductDto? value = null) : IProductCache
    {
        public List<ProductDto> Stored { get; } = [];
        public bool Removed { get; private set; }
        public Guid? RemovedId { get; private set; }
        public CancellationToken LastGetToken { get; private set; }

        public Task<ProductDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            LastGetToken = cancellationToken;
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

    private sealed class RecordingProductReadService(ProductDto? value = null) : IProductReadService
    {
        public int GetCalls { get; private set; }
        public CancellationToken LastGetToken { get; private set; }

        public Task<ProductDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            GetCalls++;
            LastGetToken = cancellationToken;
            return Task.FromResult(value is not null && value.Id == id ? value : null);
        }

        public Task<PagedResult<ProductDto>> SearchAsync(
            string? name,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Search is not used by these tests.");
        }
    }
}

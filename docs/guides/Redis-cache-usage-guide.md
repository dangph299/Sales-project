# Redis cache usage guide trong Sales Management

Tài liệu này giải thích riêng cách project đang dùng Redis: Redis nằm ở đâu, khởi tạo thế nào, cache-aside hoạt động ra sao, distributed lock dùng để làm gì, và nếu phát triển tiếp thì nên tái sử dụng/mở rộng như thế nào. Cùng phong cách với [kafka-usage-guide.md](kafka-usage-guide.md).

## Tóm tắt nhanh

- Redis chỉ dùng trong **Sales.Api/Sales.Infrastructure** (Inventory và AuditLog không dùng) cho đúng 2 việc: cache-aside cho `ProductDto` (mục 5) và distributed lock cho Hangfire job `sales-cleanup` (mục 6).
- Cache key: `catalog:product:{id:N}`, TTL 10 phút. Lock key: `lock:jobs:sales-cleanup`, TTL 5 phút, release bằng Lua script compare-and-delete (không dùng `DEL` thẳng — mục 6 giải thích vì sao).
- Không dùng RedLock.net, không dùng Redis cho session/SignalR/rate limiting.
- Muốn thêm cache-aside cho entity mới, làm theo 4 bước ở mục 8.

## 1. Redis dùng để làm gì trong project?

Redis hiện chỉ được dùng trong **Sales.Api/Sales.Infrastructure** (Inventory và AuditLog không dùng Redis), cho đúng 2 mục đích:

- **Cache-aside** cho `ProductDto` (giảm tải PostgreSQL khi đọc chi tiết sản phẩm).
- **Distributed lock** cho Hangfire recurring job `sales-cleanup`, chống nhiều instance Sales.Api chạy trùng job dọn Inbox/Outbox cũ.

Redis **không** dùng cho session, SignalR backplane, hay rate limiting trong project này.

## 2. Package Redis đang dùng

Khai báo trong `src/Services/Sales/Sales.Infrastructure/Sales.Infrastructure.csproj`:

```text
Microsoft.Extensions.Caching.StackExchangeRedis  10.0.0
StackExchange.Redis                              2.9.32
```

Hai package phục vụ 2 mục đích khác nhau:

| Package | Interface | Dùng cho |
|---|---|---|
| `Microsoft.Extensions.Caching.StackExchangeRedis` | `IDistributedCache` | Cache-aside (`CacheService<T>`) |
| `StackExchange.Redis` | `IConnectionMultiplexer` / `IDatabase` | Distributed lock (`MaintenanceJobs`) |

## 3. Redis khởi tạo trong DI như thế nào?

Đăng ký nằm ở `src/Services/Sales/Sales.Infrastructure/DependencyInjection.cs`:

```csharp
private static IServiceCollection AddSalesCaching(this IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<IProductCache, ProductCache>();
    services.AddStackExchangeRedisCache(options => options.Configuration = configuration.GetConnectionString("Redis"));
    services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));
    return services;
}
```

Gọi từ `AddSalesInfrastructure()`. Cùng một connection string (`ConnectionStrings:Redis`) được dùng cho cả `IDistributedCache` lẫn `IConnectionMultiplexer`.

`IConnectionMultiplexer` đăng ký **Singleton** vì đây là kết nối multiplexed dùng lại xuyên suốt lifetime app (đúng khuyến nghị của StackExchange.Redis, không tạo mới mỗi request).

## 4. Redis broker nằm ở đâu?

`docker/docker-compose.yml`:

```yaml
redis:
  image: redis:8-alpine
  ports: ["6379:6379"]
  healthcheck:
    test: ["CMD", "redis-cli", "ping"]
    interval: 5s
    timeout: 3s
    retries: 20
```

Không mount `redis.conf` riêng, không bật persistence/auth — dùng default cho local MVP.

`sales-api` khai báo `depends_on: redis: { condition: service_healthy }` nên chờ Redis healthy trước khi start. `inventory-api` và `audit-worker` không phụ thuộc Redis.

Connection string trong `src/Services/Sales/Sales.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "Redis": "redis:6379"
}
```

`redis:6379` là DNS name trong mạng Docker Compose, không có password.

## 5. Cache-aside cho Product hoạt động thế nào?

### 5.1 Interface

`src/Services/Sales/Sales.Application/Interfaces/ICacheService.cs`:

```csharp
public interface ICacheService<T>
{
    Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetAsync(T value, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
```

`src/Services/Sales/Sales.Application/Interfaces/IProductCache.cs`:

```csharp
public interface IProductCache : ICacheService<ProductDto>;
```

Port nằm ở Application (đúng dependency rule: Application định nghĩa port, Infrastructure implement).

### 5.2 Generic base implementation

`src/Services/Sales/Sales.Infrastructure/ExternalServices/CacheService.cs`:

```csharp
public abstract class CacheService<T> : ICacheService<T>
{
    private readonly IDistributedCache _cache;

    protected CacheService(IDistributedCache cache) => _cache = cache;

    protected virtual TimeSpan Ttl => TimeSpan.FromMinutes(10);
    protected abstract string KeyPrefix { get; }
    protected abstract Guid GetId(T value);

    public async Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var value = await _cache.GetStringAsync(Key(id), cancellationToken);
        return value is null ? default : JsonSerializer.Deserialize<T>(value);
    }

    public Task SetAsync(T value, CancellationToken cancellationToken = default) =>
        _cache.SetStringAsync(Key(GetId(value)), JsonSerializer.Serialize(value),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl }, cancellationToken);

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken = default) =>
        _cache.RemoveAsync(Key(id), cancellationToken);

    protected virtual string Key(Guid id) => $"{KeyPrefix}:{id:N}";
}
```

Đây là abstraction tái sử dụng được cho mọi entity cần cache-aside sau này — chỉ cần kế thừa và khai `KeyPrefix`/`GetId`.

### 5.3 Product-specific implementation

`src/Services/Sales/Sales.Infrastructure/ExternalServices/ProductCache.cs`:

```csharp
public sealed class ProductCache : CacheService<ProductDto>, IProductCache
{
    public ProductCache(IDistributedCache cache) : base(cache) { }

    protected override string KeyPrefix => "catalog:product";
    protected override Guid GetId(ProductDto value) => value.Id;
}
```

- **Cache key**: `catalog:product:{id:N}` (GUID không dấu gạch ngang), ví dụ `catalog:product:0f1e2d3c4b5a11eeaa5500155d9c1234`.
- **TTL**: 10 phút, absolute expiration tính từ lúc set (`Ttl` không override, dùng default của `CacheService<T>`).
- **Lưu trữ**: JSON string qua `IDistributedCache.SetStringAsync`/`GetStringAsync` → Redis `STRING` type.

### 5.4 Call site — flow đọc/ghi/xóa cache

| Handler | File | Thao tác |
|---|---|---|
| `GetProductHandler` | `Sales.Application/Queries/Products/GetProductHandler.cs` | `GetAsync` — nếu miss thì query DB rồi `SetAsync` |
| `CreateProductHandler` | `Sales.Application/Commands/Products/CreateProductHandler.cs` | `SetAsync` — warm cache ngay khi tạo |
| `UpdateProductHandler` | `Sales.Application/Commands/Products/UpdateProductHandler.cs` | `RemoveAsync` — invalidate, lần đọc kế tiếp sẽ miss và tự nạp lại |

Đọc (cache-aside GET):

```csharp
public async Task<ProductDto> Handle(GetProduct request, CancellationToken ct)
{
    var cached = await cache.GetAsync(request.Id, ct);
    if (cached is not null) return cached;
    var product = await readService.GetAsync(request.Id, ct) ?? throw new NotFoundException("Product", request.Id);
    await cache.SetAsync(product, ct);
    return product;
}
```

Ghi khi tạo mới:

```csharp
var dto = product.ToDto();
await cache.SetAsync(dto, ct);
return dto;
```

Xóa khi update (chọn invalidate thay vì set lại, để tránh set nhầm dữ liệu cũ nếu có race giữa 2 request update):

```csharp
await unitOfWork.SaveChangesAsync(ct);
await cache.RemoveAsync(product.Id, ct);
return product.ToDto();
```

Soft delete Product có handler riêng (`DeleteProductHandler`) và handler này gọi `IProductCache.RemoveAsync(product.Id)` sau khi lưu DB. Vì vậy cache-aside không trả lại product đã xóa sau khi `DELETE /api/products/{id}`. Create vẫn warm cache, update vẫn invalidate cache.

## 6. Distributed lock cho Hangfire cleanup job

`src/Services/Sales/Sales.Infrastructure/Hangfire/MaintenanceJobs.cs`:

```csharp
public sealed class MaintenanceJobs(SalesDbContext db, IConnectionMultiplexer redis)
{
    public async Task CleanupAsync()
    {
        var cache = redis.GetDatabase();
        var token = Guid.NewGuid().ToString("N");
        const string key = "lock:jobs:sales-cleanup";
        if (!await cache.StringSetAsync(key, token, TimeSpan.FromMinutes(5), When.NotExists)) return;
        try
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-14);
            await db.InboxMessages.Where(x => x.ProcessedAt < cutoff).ExecuteDeleteAsync();
            await db.OutboxMessages.Where(x => x.ProcessedAt != null && x.ProcessedAt < cutoff).ExecuteDeleteAsync();
        }
        finally
        {
            await cache.ScriptEvaluateAsync(
                "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end",
                [key], [token]);
        }
    }
}
```

Đây là lock tự viết trực tiếp trên `StackExchange.Redis`, **không dùng thư viện RedLock.net**.

| Thành phần | Giá trị |
|---|---|
| Lock key | `lock:jobs:sales-cleanup` (1 key cố định cho toàn bộ job) |
| Acquire | `SET key token NX PX 300000` qua `StringSetAsync(..., When.NotExists)` |
| Token | `Guid.NewGuid()` random mỗi lần acquire, dùng để release an toàn |
| TTL | 5 phút — tự hết hạn nếu process crash giữa chừng, tránh deadlock vĩnh viễn |
| Acquire thất bại | `return` ngay, không log, không retry — job coi như no-op ở lần chạy này, Hangfire sẽ tự chạy lại theo `Cron.Daily` |
| Release | Lua script compare-and-delete trong `finally`, chỉ xóa key nếu value vẫn đúng bằng token của chính mình |

**Vì sao cần Lua script khi release thay vì `KeyDeleteAsync` thẳng?** Nếu chỉ `DEL` đơn thuần, có thể xảy ra race: instance A giữ lock 5 phút, job chạy lâu hơn 5 phút, lock tự hết hạn, instance B acquire được lock mới; sau đó A chạy xong và `DEL` — vô tình xóa mất lock của B. Script Lua đảm bảo A chỉ xóa lock nếu value vẫn là token của A.

Job được đăng ký trong `src/Services/Sales/Sales.Api/Program.cs`:

```csharp
RecurringJob.AddOrUpdate<MaintenanceJobs>("sales-cleanup", "maintenance", x => x.CleanupAsync(), Cron.Daily);
```

**Vì sao cần Redis lock nếu Hangfire tự quản lý recurring job?** Hangfire không đảm bảo chỉ 1 instance chạy 1 recurring job nếu bạn scale `sales-api` ra nhiều container cùng trỏ vào 1 Hangfire storage — nhiều instance có thể fire job gần như đồng thời khi tick trùng giờ. Redis lock đảm bảo chỉ 1 instance thực sự dọn dữ liệu.

**Vì sao không dùng Redis lock cho Order concurrency?** Xem `project-presentation.md` mục 18 — correctness của `Order` dựa vào `Version` + optimistic concurrency + DB transaction, không phải Redis lock. Redis lock ở đây chỉ dùng cho *scheduled job coordination*, không phải business invariant.

## 7. Lệnh kiểm tra nhanh Redis local

```bash
sudo docker compose -f docker/docker-compose.yml up -d --build
docker exec -it $(docker ps -qf name=redis) redis-cli
```

Xem cache key của 1 product:

```text
127.0.0.1:6379> KEYS catalog:product:*
127.0.0.1:6379> HGETALL catalog:product:36f211724d674a579c7423e770c5b953
127.0.0.1:6379> TTL catalog:product:36f211724d674a579c7423e770c5b953
```

Xem lock cleanup job (bình thường sẽ không thấy key này trừ khi đang chạy job, vì key tự xóa ngay khi job xong):

```text
127.0.0.1:6379> GET lock:jobs:sales-cleanup
127.0.0.1:6379> TTL lock:jobs:sales-cleanup
```

## 8. Cách thêm cache-aside cho entity mới

Ví dụ muốn cache `CustomerDto`:

1. Thêm interface `ICustomerCache : ICacheService<CustomerDto>` trong `Sales.Application/Interfaces/`.
2. Thêm class `CustomerCache : CacheService<CustomerDto>, ICustomerCache` trong `Sales.Infrastructure/ExternalServices/`, khai `KeyPrefix => "catalog:customer"`.
3. Đăng ký `services.AddScoped<ICustomerCache, CustomerCache>();` trong `AddSalesCaching`.
4. Gọi `GetAsync`/`SetAsync`/`RemoveAsync` trong `GetCustomerHandler`/`CreateCustomerHandler`/`UpdateCustomerHandler` theo đúng pattern của Product ở mục 5.4.

## 9. Nên cải thiện trong production

Local MVP đang ổn cho bài thực hành, nhưng production nên thêm:

- Bật Redis AUTH/TLS, không dùng kết nối không mật khẩu như hiện tại.
- Cân nhắc RedLock.net (multi-node Redlock algorithm) nếu chạy Redis cluster nhiều node thay vì 1 node đơn — SET NX/Lua hiện tại chỉ an toàn tuyệt đối với 1 node Redis.
- Thêm prefix theo environment (`prod:catalog:product:...`) nếu dùng chung 1 Redis instance cho nhiều environment.
- Thêm metric Redis cache hit/miss (hiện chưa có custom metric nào cho cache, khác với Kafka đã có `sales.outbox.*`/`sales.inbox.*` — xem hướng dẫn thêm counter mới ở [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 9).
- Cân nhắc cache-aside cho `CustomerDto`/`OrderDto` nếu profiling cho thấy đọc nhiều, hiện tại chỉ Product có cache.

# Cache Conventions

## What exists

Redis is used by Sales and Dashboard.Bff:

1. a cache-aside read cache for `ProductDto`
2. a distributed lock for the daily cleanup job
3. the Dashboard.Bff `dashboard:snapshot` cache

Inventory and AuditLog have no Redis dependency.

## Registration

```csharp
services.AddScoped<IProductCache, ProductCache>();
services.AddStackExchangeRedisCache(o => o.Configuration = configuration.GetConnectionString("Redis"));
services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(...));
```

`IDistributedCache` backs the cache-aside path; `IConnectionMultiplexer` is used only for the lock primitives.

## Ports

```
ICacheService<T>            Application, generic cache-aside port
  IProductCache             Application, marker for ProductDto
    CacheService<T>         Infrastructure, IDistributedCache implementation
      ProductCache          Infrastructure, KeyPrefix "catalog:product"
```

`CacheService<T>` supplies get/set/remove and JSON serialization; a subclass supplies `KeyPrefix` and `GetId`, and may override `Ttl` (default 10 minutes).

## Keys and TTL

| Cache | Key | TTL |
|---|---|---|
| Product read model | `catalog:product:<guid:N>` | 10 minutes absolute |
| Cleanup lock | `lock:jobs:sales-cleanup` | 5 minutes |
| Dashboard snapshot | `dashboard:snapshot` | 5 minutes absolute |

## Read path

`CachedProductReadService` decorates `ProductReadService` and is what `IProductReadService` resolves to:

| Method | Cached |
|---|---|
| `GetAsync(id)` | yes — check cache, verify the entry is still active (otherwise evict), fall back to the database and warm the cache |
| `GetForWriteResultAsync(id)` | **no** — a command must read back the product it just wrote whatever status it landed in, and the cache only holds published products |
| `SearchAsync(...)` | **no** — paginated, filterable results are not cached |

## Invalidation

Every write path that changes the cached shape calls `productCache.RemoveAsync(product.Id, ct)` **after** `SaveChangesAsync`:

`CreateProduct` (via read-back), `UpdateProduct`, `DeleteProduct`, `AddProductVariant`, `UpdateProductVariant`, `DeactivateProductVariant`, `DeleteProductVariant`.

Remove-and-repopulate, never update-in-place. Invalidating before the save would leave a window where a concurrent read repopulates the cache from the pre-commit state.

The read path adds a second safety net: a cached entry that is no longer `IsActive` or is soft-deleted is evicted on read rather than served.

## Distributed lock

`MaintenanceCleanupJob`:

```csharp
var acquired = await cache.StringSetAsync(key, token, ttl, When.NotExists);
if (!acquired) return;
try { /* cleanup */ }
finally { await cache.ScriptEvaluateAsync(releaseScript, [key], [token]); }
```

The Lua release script deletes the key only if it still holds *this* run's token, so an expired lock acquired by another instance is never deleted by the previous owner.

Inventory does the same job with `pg_try_advisory_xact_lock`, keeping Redis out of that context.

## Rules of thumb

- Cache DTOs, never aggregates or entities.
- Cache reads, never command results.
- Never cache paginated search results.
- Serialization is `System.Text.Json` with default options — a cached DTO must round-trip through it.
- A distributed lock is an optimisation, not a correctness guarantee.

## Related

- Deep dive: [../guides/Redis-cache-usage-guide.md](../guides/Redis-cache-usage-guide.md)
- Rules: [../project/backend/redis-rule.md](../project/backend/redis-rule.md)

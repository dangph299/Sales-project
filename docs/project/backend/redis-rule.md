# Redis Rules

Redis is used by Sales for product caching and the cleanup lock, and by Dashboard.Bff for the aggregated dashboard snapshot. Inventory and AuditLog do not use Redis.

## Registration

```csharp
services.AddScoped<IProductCache, ProductCache>();
services.AddStackExchangeRedisCache(o => o.Configuration = configuration.GetConnectionString("Redis"));
services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));
```

- `IDistributedCache` for cache-aside. `IConnectionMultiplexer` only for lock primitives.
- Never inject `IConnectionMultiplexer` into a feature service to do caching.

## Cache-aside

- Every cache is a port in Application (`ICacheService<T>` → `IProductCache`) implemented in Infrastructure by deriving from `CacheService<T>`.
- A subclass supplies `KeyPrefix` and `GetId`, and may override `Ttl` (default 10 minutes).
- Key format: `<prefix>:<guid:N>` — e.g. `catalog:product:0f8c…`.
- Reads go through a decorator (`CachedProductReadService`) registered in front of the real read service. Handlers and controllers never call the cache directly for reads.
- The decorator only caches `GetAsync`. `SearchAsync` and `GetForWriteResultAsync` always hit the database — the cache holds published products only, and a write must be able to read back a Draft product it just created.

## Invalidation

- Every command that changes cached state calls `productCache.RemoveAsync(id, ct)` **after** `SaveChangesAsync`, never before.
- Invalidate on: create/update product, add/update/deactivate/delete variant, delete product.
- Never update the cache in place from a command. Remove and let the next read repopulate.
- A cached entry that is no longer active is removed on read (`CachedProductReadService.GetAsync`), so a stale-but-present entry cannot serve an unpublished product.

## Distributed lock

`MaintenanceCleanupJob` guards the Sales cleanup run:

- `StringSetAsync(key, token, ttl, When.NotExists)` to acquire; skip the run if not acquired.
- Release with a Lua compare-and-delete so an expired lock owned by someone else is never deleted.
- Key: `lock:jobs:<job-name>`. TTL 5 minutes.
- A Redis lock is an optimisation, never a correctness guarantee. The operation under it must be safe to run twice.

Inventory uses a Postgres advisory lock (`pg_try_advisory_xact_lock`) instead — Inventory has no Redis dependency. Keep it that way.

Dashboard.Bff uses `IDashboardSnapshotCache` over `IDistributedCache` when Redis is configured, with an in-memory fallback for local/dev scenarios where Redis is unavailable. The key is `dashboard:snapshot`.

## Rules

- Never cache a command result, an aggregate, or an entity. Cache DTOs only.
- Never cache paginated search results.
- Never let a cache failure fail a request — but do not add silent `catch` blocks either; the current code lets `IDistributedCache` surface errors and they are handled as 500s.
- Serialization is `System.Text.Json` with default options. Cached DTOs must be round-trippable (see [serialization-rule.md](serialization-rule.md)).

## Related

- Deep dive: [../../guides/Redis-cache-usage-guide.md](../../guides/Redis-cache-usage-guide.md)
- Reference: [../../tech/cache-conventions.md](../../tech/cache-conventions.md)

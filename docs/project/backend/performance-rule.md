# Performance Rules

## Queries

- `AsNoTracking()` on every read path.
- Project to the DTO in the query where possible; load entities only when you need behavior.
- Never N+1. Load related data with one bulk query keyed by `Contains(ids)` and group in memory (`ProductReadService.LoadVariants`).
- `Skip/Take` before materializing; `LongCountAsync` on the same filtered query for the total.
- Clamp page size — `Paging.Normalize` caps at 100.
- Add an index for every filter and sort column used by a search endpoint. Text search uses `pg_trgm` GIN indexes with `EF.Functions.ILike`.
- Avoid `IgnoreQueryFilters()`; it defeats the soft-delete index filters.

## Bulk operations

- `ExecuteDeleteAsync` / `ExecuteUpdateAsync` for maintenance and lease claiming — never load rows just to modify or delete them.
- The outbox claims a batch with a single `ExecuteUpdateAsync` lease, then loads only the claimed rows.
- Batch sizes are bounded and configurable: outbox 100, inbox re-drive `RedriveBatchSize` (50), expired-order scan clamped to 1000.

## Caching

- Cache only what is read far more often than written, and only DTOs. See [redis-rule.md](redis-rule.md).
- Application-lifetime reference data (colors, sizes, categories) is cached in the frontend store, not fetched per component.

## Allocation

- Reflection results are cached per closed generic type (`EnumExtensions.EnumDescriptionCache<TEnum>`) — never reflect per call on a hot path.
- Prefer `Stopwatch.GetTimestamp()`/`GetElapsedTime` over allocating a `Stopwatch` in high-frequency behaviors.
- Use collection expressions and arrays over `List<T>` for fixed result sets.

## Background work

- Never do long work in a request. Push it to Hangfire (Sales) or a hosted service.
- Poll intervals are configurable and clamped: outbox `Outbox:PollIntervalMilliseconds` (default 2000, clamped 100–60000), inbox re-drive `InboxConsumer:RedrivePollSeconds` (default 15, clamped 1–3600).
- The outbox prefers an in-process signal (`IOutboxSignal.Notify()` on save) over polling, with polling as the recovery fallback. Keep both.

## Kafka

- Partition key is the aggregate id, so a hot aggregate serializes on one partition. Do not use a random key to "spread load" — ordering is a correctness requirement.
- Consumer throughput is tuned with `WithBufferSize` / `WithWorkersCount` (currently 100/4 for services, 200/4 for the audit worker).

## Measurement before optimisation

- `PerformanceBehavior` logs any command/query over 500 ms at Warning. Investigate those before adding a cache.
- Outbox backlog and dead letters are observable gauges (`<service>.outbox.backlog`, `.deadletters`). Use them, do not guess.

## Related

- [async-rule.md](async-rule.md)
- [database-rule.md](database-rule.md)

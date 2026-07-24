# Background Processing

Four mechanisms run work outside a request. They are deliberately different tools for different guarantees.

## 1. Hangfire recurring jobs

Sales stores Hangfire data in PostgreSQL `hangfire`; Inventory stores Hangfire data in PostgreSQL `inventory_hangfire`; Dashboard.Bff stores its refresh job data in PostgreSQL `dashboard`. Sales queues are `critical`, `default`, `maintenance`; Inventory queues are `default`, `maintenance`; Dashboard.Bff uses `default`. Sales exposes the dashboard UI at `/hangfire`, restricted to loopback.

| Job id | Class | Default schedule | Queue | Work |
|---|---|---|---|---|
| `sales-cleanup` | `MaintenanceCleanupJob` | `0 0 * * *` | `maintenance` | delete processed inbox/outbox rows older than 14 days |
| `orders:cancel-expired` | `CancelExpiredPendingOrdersJob` | `*/5 * * * *` | `critical` | cancel open orders idle past `ExpirationMinutes` |
| `messaging:replay-dead-letter` | `ReplayDeadLetterJob` | `*/15 * * * *` | `maintenance` | reset inbound dead-letter rows so `InboxRedriveService` can replay them |
| `messaging:kafka-lag-monitor` | `KafkaLagMonitorJob` | `*/5 * * * *` | `maintenance` | read committed offsets and latest offsets for Sales' Kafka consumer group |
| `messaging:inbox-cleanup` | `InboxCleanupJob` | `0 1 * * *` | `maintenance` | delete processed inbox rows older than the configured retention |
| `messaging:failed-outbox-retry` | `FailedOutboxRetryJob` | `*/15 * * * *` | `maintenance` | reset terminal failed outbox rows so `OutboxPublisherService` can retry them |
| `messaging:outbox-pending-monitor` | `OutboxPendingMonitorJob` | `*/5 * * * *` | `maintenance` | record outbox backlog, oldest pending age, and terminal failed count |
| `inventory-dead-letter-replay` | `ReplayDeadLetterJob` | `*/15 * * * *` | `maintenance` | reset Inventory inbound dead-letter rows so `InventoryInboxRedriveService` can replay them |
| `inventory-kafka-lag-monitor` | `KafkaLagMonitorJob` | `*/5 * * * *` | `maintenance` | read committed offsets and latest offsets for Inventory's Kafka consumer group |
| `inventory-inbox-cleanup` | `InboxCleanupJob` | `0 1 * * *` | `maintenance` | delete processed Inventory inbox rows older than the configured retention |
| `inventory-failed-outbox-retry` | `FailedOutboxRetryJob` | `*/15 * * * *` | `maintenance` | reset Inventory terminal failed outbox rows so `OutboxPublisherService` can retry them |
| `inventory-outbox-pending-monitor` | `OutboxPendingMonitorJob` | `*/5 * * * *` | `maintenance` | record Inventory outbox backlog, oldest pending age, and terminal failed count |
| `dashboard:snapshot-refresh` | `DashboardSnapshotRefreshJob` | `* * * * *` | `default` | rebuild and cache the dashboard snapshot |

Registration mechanics live in `RecurringJobManagerExtensions.ScheduleRecurringJob<TJob>(id, settings, expression)`:

- enabled → `AddOrUpdate` on the named queue and cron, always UTC
- disabled → `RemoveIfExists`, so a turned-off job stops firing instead of lingering on its old schedule

Job ids are constants in service-owned classes (`SalesRecurringJobIds`, `InventoryRecurringJobIds`, `DashboardRecurringJobIds`) — deliberately **not** configurable, so a config change cannot accidentally create a second recurring job. Schedules are configuration and validated on start.

`MaintenanceCleanupJob` acquires a Redis lock (`lock:jobs:sales-cleanup`, 5-minute TTL, Lua compare-and-delete release) so only one instance cleans up per run. `CancelExpiredPendingOrdersJob` dispatches `CancelExpiredPendingOrders` through MediatR, records the batch metrics, and logs a summary.

The messaging reliability jobs use Postgres advisory transaction locks, mutate only messaging state, and do not publish to Kafka or process business events directly. Replay/retry jobs reset terminal rows so the existing `InboxRedriveService` and `OutboxPublisherService` own the actual processing path.

`DashboardSnapshotRefreshJob` is an aggregation adapter only: it calls `IDashboardSnapshotBuilder`, then stores the result in `IDashboardSnapshotCache`.

## 2. Hosted services

| Service | Host | Loop |
|---|---|---|
| `SalesOutboxPublisher` / `InventoryOutboxPublisher` | API | signal-driven with polling fallback |
| `SalesInboxRedriveService` / `InventoryInboxRedriveService` | API | fixed poll interval |
| `InventoryMaintenanceWorker` | Inventory.Api | `PeriodicTimer`, once at startup then daily; processed-outbox cleanup only |
| `MongoStartupService` | AuditLog.Worker | one-shot readiness + index creation |
| `KafkaBusService` | AuditLog.Worker | owns the KafkaFlow bus lifecycle |

All create a fresh async scope per cycle, catch and log exceptions without exiting the loop, and honour the stopping token.

`InventoryMaintenanceWorker` remains for the legacy processed-outbox retention cleanup. Inventory inbox cleanup and messaging recovery/monitoring run through Hangfire so they share the same recurring-job settings and queue conventions as Sales.

## 3. Kafka consumers

Long-running KafkaFlow consumers started with the bus. See [messaging-conventions.md](messaging-conventions.md).

## 4. Startup tasks

Run before the host serves traffic (`RunStartupTasksAsync`):

- Sales: start the Kafka bus (with a stop hook on `ApplicationStopping`), apply migrations + seed identity roles and the `admin` user, register recurring jobs.
- Inventory: apply migrations, start the Kafka bus, register recurring jobs.
- AuditLog: `MongoStartupService` pings Mongo (20 attempts, 2 s apart) and ensures indexes, then `KafkaBusService` starts consuming.
- Dashboard.Bff: register the snapshot refresh recurring job when Hangfire is configured.

## Choosing a mechanism

| Need | Use |
|---|---|
| Cron schedule, visible in a dashboard, survives restarts | Hangfire recurring job |
| Continuous polling loop tied to the host lifetime | `BackgroundService` |
| React to an event from another service | Kafka consumer |
| One-time preparation before serving traffic | startup task |
| Operator-triggered recovery | `*MaintenanceService` method, not a job |

## Concurrency safety

Every scheduled or looping operation must be safe to run twice:

- outbox rows are leased (`LockId`, `LockedUntil` 30 s)
- Sales cleanup takes a Redis lock; Sales/Inventory messaging reliability jobs and Inventory processed-outbox cleanup take Postgres advisory locks
- expired-order cancellation re-checks `CancelDueToExpiration` per order inside its own scope
- locks are treated as optimisations; the underlying operation is idempotent

## Observability

Jobs log structured summaries with counts/thresholds. Expired-order cancellation emits `sales.orders.expiration.*`; messaging reliability jobs update outbox/inbox/Kafka gauges and counters documented in [logging-and-observability-strategy.md](logging-and-observability-strategy.md). Hangfire dashboard polling is logged at `Debug` so it does not drown the signal.

## Related

- [configuration-and-environment.md](configuration-and-environment.md)
- [retry-and-dead-letter.md](retry-and-dead-letter.md)
- [outbox-inbox-schema.md](outbox-inbox-schema.md)

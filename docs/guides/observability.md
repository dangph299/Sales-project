# Observability dashboard contract

Local Docker Compose sends OpenTelemetry traces, metrics, and logs to Elastic APM through `otel-collector`. See [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) for how the SDK is wired into each service's code, and [Elastic-usage-guide.md](Elastic-usage-guide.md) for the collector/APM Server/Elasticsearch/Kibana pipeline itself.

## Required Kibana dashboard panels

Create a Kibana dashboard named `Sales Management Reliability` with these Lens panels:

| Panel | Metric name | Service | Visualization |
|---|---|---|---|
| Sales outbox backlog | `sales.outbox.backlog` | `sales-api` | Gauge / time series |
| Sales outbox dead letters | `sales.outbox.deadletters` | `sales-api` | Gauge / time series |
| Sales outbox published rate | `sales.outbox.published` | `sales-api` | Counter rate |
| Sales outbox failure rate | `sales.outbox.failed` | `sales-api` | Counter rate |
| Sales inbox duplicates | `sales.inbox.duplicate` | `sales-api` | Counter rate |
| Sales inbox retried rate | `sales.inbox.retried` | `sales-api` | Counter rate |
| Sales inbox dead-letter rate | `sales.inbox.dead_lettered` | `sales-api` | Counter rate |
| Inventory outbox backlog | `inventory.outbox.backlog` | `inventory-api` | Gauge / time series |
| Inventory outbox dead letters | `inventory.outbox.deadletters` | `inventory-api` | Gauge / time series |
| Inventory inbox retried rate | `inventory.inbox.retried` | `inventory-api` | Counter rate |
| Inventory inbox dead-letter rate | `inventory.inbox.dead_lettered` | `inventory-api` | Counter rate |
| Inventory reservation rejected rate | `inventory.reservation.rejected` | `inventory-api` | Counter rate |
| Inventory reservation reserved rate | `inventory.reservation.reserved` | `inventory-api` | Counter rate |
| HTTP latency | `http.server.request.duration` | `sales-api`, `inventory-api` | p95/p99 time series |
| HTTP errors | `http.server.request.duration` filtered by 5xx status | `sales-api`, `inventory-api` | Counter rate |

## Trace fields to inspect

- `service.name`
- `trace.id` (Kibana) / `TraceId` (Seq) — same value, see [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 7 for how the two correlate
- `http.route`
- `messaging.destination.name`
- `messaging.kafka.consumer.group`
- `db.system`

## Operational thresholds for the MVP

- `*.outbox.backlog > 0` for more than 5 minutes: warning.
- `*.outbox.deadletters > 0`: critical; inspect `outbox_messages.LastError`, fix consumer/broker issue, then replay.
- `*.inbox.duplicate` spikes: expected during replay, suspicious during normal traffic.
- `*.inbox.dead_lettered > 0`: critical; inspect `inbox_messages.LastError`, fix handler/data issue, then reset dead-lettered inbox rows for replay.
- `inventory.reservation.rejected` spikes: stock/customer workflow issue, not a platform issue by itself.

## Replay operations

Sales exposes on-demand replay operations on `SalesMaintenanceService`. These are not recurring jobs and currently have no production caller:

- `ReplayOutboxMessageAsync(Guid outboxMessageId)` resets a specific outbox message.
- `ReplayDeadLetterOutboxMessagesAsync(int maximumMessageCount)` resets up to 100 dead-lettered outbox messages.
- `ResetInboxDeadLetterAsync(Guid eventId)` resets a specific dead-lettered inbox message to `Failed`.
- `ResetInboxDeadLettersAsync(int maximumMessageCount)` resets up to 100 dead-lettered inbox messages.

After replay, the outbox publisher will pick rows where `ProcessedAt is null`, `DeadLetteredAt is null`, and `NextAttemptAt <= now`.
After inbox reset, `SalesInboxRedriveService` will pick rows where `Status = Failed`, `Payload is not null`, and `NextAttemptAt is null or <= now`.

Inventory exposes the same inbox reset operations on `InventoryMaintenanceService`; it does not use Hangfire in the current host.

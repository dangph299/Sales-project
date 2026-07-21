# Retry, Dead Letter & Recovery

## Backoff

One schedule for both reliable-messaging pipelines, `BuildingBlocks.Infrastructure/Resilience/RetryBackoff.cs`:

```
delay(attempts) = min(300s, 2 ^ min(attempts, 8))
```

Attempts 1…8 give 2, 4, 8, 16, 32, 64, 128, 256 seconds; anything beyond is capped at 300 seconds. There is no jitter.

## Where retries happen

| Pipeline | Trigger | Max attempts | On exhaustion |
|---|---|---|---|
| Outbox publish | `OutboxPublisherService` cycle | `OutboxMessage.MaxAttempts = 10` | `DeadLetteredAt` stamped; automatic retry stops |
| Inbox re-drive | `InboxRedriveService` cycle | `InboxConsumerOptions.MaxAttempts = 5` | `Status = DeadLettered`, `NextAttemptAt = null` |
| Mongo startup | `MongoStartupService` | 20 attempts, fixed 2 s | last attempt rethrows and the worker fails to start |
| Hangfire jobs | Hangfire defaults | Hangfire default retry policy | job moves to Failed in the dashboard |

There is **no** circuit breaker and no Polly pipeline anywhere in the solution. Resilience is the outbox/inbox state machine plus Kafka's own client retries. See [discrepancies.md](discrepancies.md).

## Why Kafka redelivery is not the retry mechanism

KafkaFlow commits the consumer offset whether or not the handler succeeded. A handler that throws therefore **loses** the message. That is why `IntegrationEventHandler` swallows the exception after recording the failure in the inbox — the inbox row, complete with the stored envelope, is the durable retry queue. `InboxRedriveService` replays from it.

The single case where the handler rethrows is when no `IInboxFailureRecorder` is registered: there is nowhere to persist the event, so the loss is made loud instead of silent.

## Dead-letter semantics

There is no Kafka DLQ topic. "Dead letter" means a row parked in the service's own table:

- Outbox: `ProcessedAt IS NULL AND DeadLetteredAt IS NOT NULL`
- Inbox: `Status = DeadLettered`

Dead-lettered rows are never auto-deleted by the cleanup jobs, which only remove *processed* rows older than 14 days.

## Manual recovery

`Sales.Infrastructure/Maintenance/SalesMaintenanceService.cs` (registered scoped; invoked by an operator, not scheduled):

| Operation | Effect |
|---|---|
| `ReplayOutboxMessageAsync(id)` | resets `Attempts`, error, lease, `DeadLetteredAt`; sets `NextAttemptAt = now` |
| `ReplayDeadLetterOutboxMessagesAsync(max)` | same, for up to 100 dead-lettered rows, oldest first |
| `ResetInboxDeadLetterAsync(eventId)` | sets `Status = Failed`, clears attempts/error/dead-letter so re-drive picks it up |
| `ResetInboxDeadLettersAsync(max)` | same, batched to 100 |

`Inventory.Infrastructure/Maintenance/InventoryMaintenanceService.cs` offers the inbox equivalents plus `CleanupAsync`. Inventory has no outbox replay method — see [discrepancies.md](discrepancies.md).

There is no HTTP endpoint or dashboard for these operations today; they are invoked from code or a scripted host.

## Observability

| Signal | Meaning |
|---|---|
| `<service>.outbox.backlog` | rows neither published nor dead-lettered — should trend to 0 |
| `<service>.outbox.deadletters` | needs an operator |
| `<service>.outbox.failed` | publish attempts that failed |
| `<service>.inbox.retried` | re-drive successes |
| `<service>.inbox.dead_lettered` | needs an operator |
| Log `Inbound message dead-lettered …` | Error level, carries topic/partition/offset/EventId |
| Log `Inbox re-drive failed …` | Warning level, carries attempts and dead-letter flag |

The Kibana dashboard in `docker/kibana/exports/sales-management-reliability.ndjson` visualises these.

## Reliability tests

`Category=Reliability` tests exercise these state machines against real Postgres and Mongo, using the `internal` single-cycle hooks (`RunPublishCycleAsync`, `RunRedriveCycleAsync`) so the loops stay deterministic. See [reliability-tests.md](reliability-tests.md).

## Related

- [outbox-inbox-schema.md](outbox-inbox-schema.md)
- [messaging-conventions.md](messaging-conventions.md)
- [background-jobs.md](background-jobs.md)

# Outbox & Inbox Schema

Both entities live in `src/Shared/BuildingBlocks.Infrastructure/{Outbox,Inbox}/` and are mapped independently by each service into its own database, so Sales and Inventory share the shape but not the data.

## `outbox_messages`

| Column | Type | Meaning |
|---|---|---|
| `Id` | `uuid` PK | equals the envelope's `EventId` |
| `Topic` | `text` | destination Kafka topic |
| `Payload` | `jsonb` | serialized `EventEnvelope` |
| `OccurredAt` | `timestamptz` | when the underlying event occurred; publish order |
| `ProcessedAt` | `timestamptz?` | set only after the broker acknowledges |
| `NextAttemptAt` | `timestamptz?` | earliest next attempt after a failure |
| `DeadLetteredAt` | `timestamptz?` | set once `Attempts >= MaxAttempts` (10) |
| `LockedUntil` | `timestamptz?` | lease expiry (30 s) |
| `LockId` | `uuid?` | id of the publisher cycle holding the lease |
| `Attempts` | `int` | publish attempts so far |
| `LastError` | `text?` | truncated to 2000 chars |

Indexes: `(ProcessedAt, OccurredAt)`, `(DeadLetteredAt, NextAttemptAt, OccurredAt)`, `(LockId)`.

### Publish cycle — `OutboxPublisherService<TDbContext>`

1. Wake on `IOutboxSignal` or after the poll interval (`Outbox:PollIntervalMilliseconds`, default 2000, clamped 100–60000).
2. Select up to 100 ready ids — not processed, not dead-lettered, `NextAttemptAt` due, lease free — ordered by `OccurredAt`.
3. Claim them with one `ExecuteUpdateAsync` setting `LockId` and `LockedUntil = now + 30s`, so multiple app instances never publish the same row.
4. Load only the rows carrying this cycle's `LockId` and publish each.
5. Success → `ProcessedAt = now`, `Attempts++`, clear error and lease.
6. Failure → `Attempts++`, store the error, clear the lease, set `NextAttemptAt = now + RetryBackoff.ForAttempt(Attempts)`; at `MaxAttempts` set `DeadLetteredAt` instead.
7. Update the backlog and dead-letter gauges.

The lease is the only concurrency control. A crashed instance's rows become claimable again after 30 seconds.

## `inbox_messages`

| Column | Type | Meaning |
|---|---|---|
| `EventId` | `uuid` PK | the deduplication key |
| `ProcessedAt` | `timestamptz` | when processing completed |
| `Status` | `int` | `Processed=0`, `Failed=1`, `DeadLettered=2` |
| `Attempts` | `int` | failed attempts; unchanged by successful duplicates |
| `LastFailedAt` | `timestamptz?` | |
| `DeadLetteredAt` | `timestamptz?` | |
| `NextAttemptAt` | `timestamptz?` | earliest re-drive time |
| `Payload` | `text?` | serialized envelope, retained only for failed events so they can be replayed |
| `LastExceptionType` | `varchar(512)?` | |
| `LastError` | `varchar(2000)?` | |
| `OriginalTopic` | `varchar(256)?` | |
| `OriginalPartition` | `int?` | |
| `OriginalOffset` | `bigint?` | |
| `OriginalConsumerGroup` | `varchar(256)?` | |
| `Consumer` | `text` | `sales-v1` / `inventory-v1`. Required in Sales, nullable in Inventory. |

Index: `(Status, DeadLetteredAt)`.

The primary key **is** the idempotency mechanism: a duplicate insert raises a Postgres unique violation, which `PostgresExceptions.IsUniqueViolation` recognises and turns into a "Duplicate" outcome.

### Sales consume path

`SalesInventoryEventProcessor` opens an explicit transaction so the inbox row and the order transition commit together:

1. Look up the inbox row. Absent → insert and save. Present and `Processed`/`DeadLettered` → roll back, count `sales.inbox.duplicate`, return `Duplicate`. Present and `Failed` → mark `Processed`.
2. Load the order. Missing → still save the inbox row and commit, return `order_not_found`, so the orphan event is not reprocessed forever.
3. Apply the transition for `StockReserved` / `StockRejected`; `StockReleased` and unknown types change nothing.
4. Save and commit unconditionally — an event with no handler is still successfully processed and its row must be `Processed`, otherwise the re-drive service selects it forever.
5. Notify SignalR after commit, best-effort.

### Inventory consume path

`InventoryTransactionBehavior` wraps every command:

1. Non-transactional `HasBeenProcessedAsync` pre-check → early return for duplicates without opening a transaction.
2. Open a `Serializable` transaction.
3. `TryRecordAsync(eventId)` — the authoritative barrier. Duplicate → roll back and return the command's `DuplicateResponse`.
4. Run the handler, `SaveChangesAsync`, commit.

### Re-drive — `InboxRedriveService<TDbContext>`

Kafka commits the offset even when a handler fails, so Kafka will never redeliver. This background service is the retry mechanism:

- Every `InboxConsumer:RedrivePollSeconds` (default 15, clamped 1–3600), select up to `RedriveBatchSize` (default 50) rows with `Status = Failed`, a non-null `Payload`, and `NextAttemptAt` due, oldest first.
- Deserialize the stored envelope and replay it through `IIntegrationEventProcessor` **in its own scope**, so a failed attempt's tracked changes cannot leak into the failure-recording scope.
- Success → `sales|inventory.inbox.retried`.
- Failure → `IInboxFailureRecorder.RecordFailureAsync` increments `Attempts`, sets `NextAttemptAt` from `RetryBackoff`, and dead-letters at `MaxAttempts` (5).

## Retention

Processed rows older than 14 days are deleted: Sales by the `sales-cleanup` Hangfire job (daily, Redis-locked), Inventory by `InventoryMaintenanceWorker` (daily, Postgres advisory lock). Dead-lettered rows are never auto-deleted.

## Metrics

`<service>.outbox.{published,failed,deadlettered}` counters, `<service>.outbox.{backlog,deadletters}` gauges, `<service>.inbox.{duplicate,processed,retried,dead_lettered}` counters.

## Related

- [messaging-conventions.md](messaging-conventions.md)
- [retry-and-dead-letter.md](retry-and-dead-letter.md)
- [reliability-tests.md](reliability-tests.md)

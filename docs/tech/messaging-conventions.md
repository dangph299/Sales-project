# Messaging Conventions

## What exists

Asynchronous, at-least-once messaging over Kafka (KafkaFlow 4.2), with a transactional outbox on the producing side and an inbox on the consuming side. No synchronous service-to-service HTTP calls exist between bounded contexts.

## Why it exists

A Sales order confirmation must not depend on Inventory being reachable, and a Kafka publish cannot participate in a database transaction. Writing the event to the same database transaction as the state change, then publishing it from a background poller, makes the pair atomic. The consumer's inbox makes redelivery harmless.

## Producing

```
aggregate raises domain event
  -> SalesDbContext.SaveChangesAsync maps it via DomainEventMapper
     (or Inventory handler calls IInventoryEventOutbox)
  -> OutboxMessage row written in the SAME transaction as the state change
  -> IOutboxSignal.Notify()
  -> OutboxPublisherService claims a batch and publishes
  -> KafkaOutboxPublisher.ProduceAsync
  -> row marked ProcessedAt only after the broker acknowledges
```

Rules:

- No component other than `KafkaOutboxPublisher` produces to Kafka.
- The message key is `envelope.AggregateId.ToString()`, so all events for one aggregate land on one partition and stay ordered.
- The publish span is `kafka.publish <topic>` (`ActivityKind.Producer`) and writes `traceparent`/`tracestate` headers for distributed tracing.
- Publishing is idempotent from the broker's perspective only in the sense that a redelivered `EventId` is deduplicated by the consumer's inbox.

## Consuming

```
KafkaFlow typed handler (IntegrationEventHandler<T>)
  -> starts kafka.consume span from traceparent
  -> pushes EventId/EventType/CorrelationId/TraceId onto the log context
  -> resolves IIntegrationEventProcessor in a fresh async scope
  -> processor deduplicates on EventId, applies state, returns an outcome string
  -> logs "Consumed …" with topic/partition/offset/result/elapsed
```

The critical rule: **a consumer handler never rethrows.** KafkaFlow commits the offset regardless of the handler's outcome, so throwing would drop the message permanently. Instead the failure is recorded in the inbox (`IInboxFailureRecorder`) and `InboxRedriveService` owns retry. The one exception is when no failure recorder is registered — then it rethrows so the loss is loud rather than silent.

## Delivery guarantees

| Property | Guarantee |
|---|---|
| Delivery | at least once |
| Ordering | per aggregate (partition key = `AggregateId`) |
| Duplicate handling | inbox keyed on `EventId` |
| Out-of-order across topics | aggregate version guard (`Reservation.IsStale`) |
| Loss on consumer failure | prevented by inbox failure recording + re-drive |
| Loss on producer failure | prevented by the outbox row surviving the crash |

## Outcome strings

Processors return a short outcome that appears in consume logs and metrics. Current vocabulary:

Sales: `Reserved`, `Rejected`, `Released`, `Ignored`, `Duplicate`, `order_not_found`.
Inventory: `Reserved`, `ReservedAcknowledged`, `AlreadyReserved`, `Rejected`, `Released`, `AlreadyReleased`, `StaleRelease`, `ReleasedBeforeReserve`, `Duplicate`, `Ignored`, `stale_reservation`.

These are asserted by tests. Renaming one is a breaking change.

## Retry and dead-letter

Both outbox publishing and inbox re-drive use the same `RetryBackoff.ForAttempt(n)`: `min(300, 2^min(n,8))` seconds — 2, 4, 8 … capped at 5 minutes.

- Outbox: `OutboxMessage.MaxAttempts = 10`, then `DeadLetteredAt` is stamped and automatic retry stops.
- Inbox: `InboxConsumerOptions.MaxAttempts = 5`, then `Status = DeadLettered`.
- Recovery is manual, through `SalesMaintenanceService` / `InventoryMaintenanceService`.

See [retry-and-dead-letter.md](retry-and-dead-letter.md).

## Naming

- Topic: `<bounded-context>.<business-event>.v<version>`
- Consumer group: `<consumer>-<purpose>-v<n>`
- Producer: `<service>-outbox`
- Header: `kebab-case`

All declared as constants in `BuildingBlocks.Contracts`. Never inline.

## Related

- [kafka-topics-and-schemas.md](kafka-topics-and-schemas.md)
- [outbox-inbox-schema.md](outbox-inbox-schema.md)
- [retry-and-dead-letter.md](retry-and-dead-letter.md)
- Rules: [../project/backend/kafka-rule.md](../project/backend/kafka-rule.md)

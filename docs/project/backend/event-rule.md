# Domain & Integration Event Rules

## Domain events

- `sealed record <Aggregate><Fact>DomainEvent(...) : DomainEvent` in `<Service>.Domain/Events/<Aggregate>s/`.
- Immutable, past tense, business data only. No topic name, no transport field, no `EventId`.
- Raised from inside the aggregate with `Raise(...)` / `RaiseDomainEvent(...)`, always after the state change and `Touch()`.
- Buffered on the aggregate and cleared by `SalesDbContext.SaveChangesAsync` after the save.
- Not dispatched in-process through MediatR. The only consumer today is `DomainEventMapper`.

## Domain event → integration event

- Mapping lives in `Sales.Infrastructure/Kafka/DomainEventMapper.cs`.
- A domain event with no mapping is simply not published — that is intentional, not a bug.
- To publish a new domain event: add a `case` to `MapToPayload`, return `(topic, integrationEventPayload)`.
- Never publish directly from a handler or an aggregate. Always domain event → mapper → outbox row → publisher.

## Integration events

- `sealed record` deriving from `IntegrationEventBase`, in `BuildingBlocks.Contracts/IntegrationEvents/<Context>/`.
- Primitive/serializable fields only — no domain types, no enums whose numeric value matters.
- One event per business fact. Keep payloads minimal; consumers re-read details from their own store or a query.
- Every field gets an XML doc.

## Envelope

Everything on the wire is an `EventEnvelope`:

```
EventId, EventType, AggregateId, Version, CorrelationId, CausationId, OccurredAt, Actor, Data
```

- Build it with `EventEnvelopeFactory.Create(aggregateId, version, payload, actor, correlationId, causationId)`.
- `EventType` is the payload's runtime type name; consumers switch on it.
- `AggregateId` is the Kafka partition key. It must be the entity whose ordering matters (the order id for order events).
- `Version` carries the source aggregate version and is used for staleness checks. Never send 0 for a business event (audit events deliberately send 0).
- `CorrelationId` propagates the workflow; `CausationId` is the id of the event that caused this one.

## Consuming

- Switch on `envelope.EventType`, deserialize `envelope.Data` into the contract type.
- An unknown `EventType` must still be recorded in the inbox and return an "ignored" outcome — never throw and never silently drop.
- Handle out-of-order delivery with the aggregate's own version guard (`Reservation.IsStale`), not with timestamps.

## Versioning

- Breaking change ⇒ new topic with a `.v2` suffix and a new constant in `KafkaTopics`.
- Additive optional field on an existing payload is allowed within `.v1`.
- `ContractVersions.V1` is the current and only version. The `contract-version` header carries it.
- Never rename an integration event type or a field within a published version.

## Related

- [kafka-rule.md](kafka-rule.md)
- Reference: [../../tech/kafka-topics-and-schemas.md](../../tech/kafka-topics-and-schemas.md)
- Learning: [../../guides/07-domain-events-and-outbox.md](../../guides/07-domain-events-and-outbox.md)

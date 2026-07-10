# Shared Infrastructure Refactor Specs — Sales + Inventory

## 1. Goal

Refactor only the duplicated technical infrastructure between `Sales.Infrastructure` and `Inventory.Infrastructure` into shared BuildingBlocks projects.

The refactor must:

- remove real duplication only;
- preserve all business behavior;
- preserve database schema unless explicitly proven necessary;
- keep Domain/Application untouched;
- keep AuditLog out of scope;
- follow `docs/CODING_RULES.md`;
- implement one phase at a time and validate before moving to the next phase.

## 2. Scope

### In scope

Only duplicated technical code in:

- `Sales.Infrastructure`
- `Inventory.Infrastructure`

Targeted shared concerns:

- Outbox entity model
- Outbox publisher abstraction
- Kafka outbox publisher
- Event envelope creation
- ActivitySource injection
- shared metrics primitives
- PostgreSQL duplicate-key helper
- Kafka consumer activity helper

### Out of scope

Do not change:

- Domain layer
- Application layer
- API layer behavior
- business rules
- AuditLog
- Inbox schema
- Outbox BackgroundService polling logic

## 3. Non-negotiable decisions

These decisions are fixed for this refactor:

1. `AuditLog` is out of scope.
2. `InboxMessage` and `InboxRow` must remain separate because schema differs.
3. `SalesOutboxPublisher` and `InventoryOutboxPublisher` background services remain separate.
4. Do not merge business consumer handlers.
5. Only dedupe technical helper code from consumers.
6. Every phase must build and test before continuing.
7. No database migration should be generated except the temporary probe migration used to verify no schema change.

## 4. BuildingBlocks project layout

Use this structure:

```text
src/
└── Shared/
    ├── BuildingBlocks.Contracts/
    │   └── Messaging/
    │       ├── EventEnvelope.cs
    │       ├── MessageHeaders.cs
    │       └── TraceContextParser.cs
    │
    ├── BuildingBlocks.Infrastructure/
    │   ├── Messaging/
    │   │   ├── EventEnvelopeFactory.cs
    │   │   └── KafkaOutboxPublisher.cs
    │   │
    │   ├── Outbox/
    │   │   ├── OutboxMessage.cs
    │   │   └── IOutboxPublisher.cs
    │   │
    │   ├── Exceptions/
    │   │   └── PostgresExceptions.cs
    │   │
    │   └── Tracing/
    │       └── KafkaConsumerActivity.cs
    │
    └── BuildingBlocks.Observability/
        ├── ObservabilityNames.cs
        ├── Metrics/
        │   ├── OutboxMetrics.cs
        │   └── InboxMetrics.cs
        └── Tracing/
```

## 5. Project dependency rules

Allowed dependencies:

| Project | May reference |
|---|---|
| `BuildingBlocks.Contracts` | none or framework-neutral BCL only |
| `BuildingBlocks.Observability` | BCL only, optionally `BuildingBlocks.Contracts` if needed |
| `BuildingBlocks.Infrastructure` | `BuildingBlocks.Contracts`, `BuildingBlocks.Observability` |
| `Sales.Infrastructure` | `BuildingBlocks.Contracts`, `BuildingBlocks.Infrastructure`, `BuildingBlocks.Observability` |
| `Inventory.Infrastructure` | `BuildingBlocks.Contracts`, `BuildingBlocks.Infrastructure`, `BuildingBlocks.Observability` |

Forbidden:

- `BuildingBlocks.*` must never reference `Sales.*` or `Inventory.*`.
- `BuildingBlocks.Contracts` must not reference Infrastructure.
- Domain/Application must not reference Infrastructure.
- Do not introduce circular dependencies.

## 6. BuildingBlocks boundary rules

Before moving code into BuildingBlocks, verify all conditions:

- It is duplicated in at least two places.
- It is technical infrastructure, not business logic.
- It contains no Sales-specific business knowledge.
- It contains no Inventory-specific business knowledge.
- It is likely reusable by another service.
- It has a clear owner project.
- It does not create a dumping-ground utility class.

If any condition fails, keep the code local.

Do not move code into BuildingBlocks only because two files look similar.

## 7. Phase 1 — Move `EventEnvelopeFactory`

### Current problem

`EventEnvelopeFactory` exists in both Sales and Inventory and is effectively identical.

### Decision

Move it to:

```text
BuildingBlocks.Infrastructure/Messaging/EventEnvelopeFactory.cs
```

Do not place it in `BuildingBlocks.Contracts` because it is executable creation logic, not a pure contract.

### Required changes

- Create `BuildingBlocks.Infrastructure/Messaging/EventEnvelopeFactory.cs`.
- Keep the existing public API as close as possible.
- Change access modifier to `public` only because it is used across assemblies.
- Delete the duplicated Sales and Inventory copies.
- Update namespaces/usings.

### Must not change

- Envelope structure
- Serialization behavior
- CorrelationId behavior
- Trace context behavior
- Business behavior

### Validation

- `dotnet build`
- full test suite
- no duplicate `EventEnvelopeFactory` remains in Sales/Inventory

## 8. Phase 2 — Shared Outbox entity and publisher abstraction

### Current problem

`Sales.Infrastructure` has `OutboxMessage` and `Inventory.Infrastructure` has `OutboxRow`, but both represent the same outbox row structure.

### Decision

Create one shared outbox model:

```text
BuildingBlocks.Infrastructure/Outbox/OutboxMessage.cs
```

Create shared abstraction:

```text
BuildingBlocks.Infrastructure/Outbox/IOutboxPublisher.cs
```

### Required changes

- Create shared `OutboxMessage` with the same 11 properties and `MaxAttempts` constant.
- Add `OutboxMessage.From(EventEnvelope envelope, string topic)` only if it exactly deduplicates repeated object initialization.
- Delete local `Sales.Infrastructure` and `Inventory.Infrastructure` outbox row/entity classes.
- Update EF configurations in each service to map the shared type.
- Keep `ToTable("outbox_messages")` unchanged.
- Keep each service’s `IEntityTypeConfiguration` local because each DbContext/database owns its mapping.
- Keep Inbox untouched.

### Must not change

- table name
- column names
- indexes
- migration history
- outbox behavior
- inbox behavior

### Validation

- `dotnet build`
- full test suite
- migration probe for both DbContexts:
  - `dotnet ef migrations add ZZ_Probe ...`
  - verify `Up` and `Down` are empty
  - delete the probe migration

## 9. Phase 3 — Shared `KafkaOutboxPublisher`

### Current problem

Sales and Inventory publish outbox messages with the same Kafka logic. Only `ActivitySource` and producer name differ.

### Decision

Move publisher to:

```text
BuildingBlocks.Infrastructure/Messaging/KafkaOutboxPublisher.cs
```

Constructor:

```csharp
public sealed class KafkaOutboxPublisher(
    IProducerAccessor producers,
    ILogger<KafkaOutboxPublisher> logger,
    ActivitySource activitySource,
    string producerName) : IOutboxPublisher
```

### Required changes

- Move shared publish logic into `KafkaOutboxPublisher`.
- Inject `ActivitySource`.
- Inject `producerName` using service-specific DI factory lambda.
- Do not create Options class for a single string.
- Delete duplicated local publisher classes.

### Must not change

- Kafka topic
- Kafka key
- Kafka headers
- traceparent/tracestate propagation
- logging fields
- retry/dead-letter behavior

### Validation

- `dotnet build`
- full test suite
- verify Sales uses `sales-outbox`
- verify Inventory uses the current Inventory producer name

## 10. Phase 4 — ActivitySource via DI

### Current problem

`SalesActivitySource` and `InventoryActivitySource` are static wrappers around a name string and an `ActivitySource` instance.

### Decision

Remove static wrappers and register `ActivitySource` through DI.

Use shared constants to avoid string drift:

```text
BuildingBlocks.Observability/ObservabilityNames.cs
```

Example:

```csharp
public static class ObservabilityNames
{
    public const string SalesKafka = "Sales.Infrastructure.Kafka";
    public const string InventoryKafka = "Inventory.Infrastructure.Kafka";
}
```

### Required changes

- Delete `SalesActivitySource.cs`.
- Delete `InventoryActivitySource.cs`.
- Register service-specific `ActivitySource` singleton in DI.
- Inject `ActivitySource` into:
  - `KafkaOutboxPublisher`
  - `SalesIntegrationEventHandler`
  - `InventoryEventHandler`
- Use `ObservabilityNames` wherever the same source name is required.

### Must not change

- ActivitySource name values
- OpenTelemetry source registration behavior
- span names
- tags
- trace propagation

### Validation

- `dotnet build`
- full test suite
- verify tracing source names remain identical to before

## 11. Phase 5 — Shared metrics primitives

### Current problem

Sales and Inventory metrics repeat the same outbox/inbox counters and gauges, while Inventory also has reservation-specific metrics.

### Decision

Create reusable metrics primitives in:

```text
BuildingBlocks.Observability/Metrics/OutboxMetrics.cs
BuildingBlocks.Observability/Metrics/InboxMetrics.cs
```

Keep `SalesMetrics` and `InventoryMetrics` as local static facades for now to avoid call-site churn.

### Required changes

- Add `OutboxMetrics` with published, failed, dead-lettered counters and snapshot gauge support.
- Add `InboxMetrics` with duplicate and processed counters.
- Update `SalesMetrics` to delegate internally.
- Update `InventoryMetrics` to delegate internally.
- Keep Inventory reservation counters local.
- Keep all metric names unchanged.

### Must not change

- metric names
- meter names
- dashboard compatibility
- alert compatibility
- existing call sites

### Validation

- `dotnet build`
- full test suite
- verify metric names are unchanged

## 12. Phase 6 — Outbox BackgroundService

Phase 6 is intentionally skipped.

Do not merge:

- `SalesOutboxPublisher`
- `InventoryOutboxPublisher`

Reason:

- the code is reliability-sensitive;
- polling, locking, retry and dead-letter behavior are critical;
- deduplication benefit does not justify the risk in this refactor.

This may be revisited in a separate production-hardening effort after phases 1–5 and 7 are stable.

## 13. Phase 7 — Consumer technical helper dedupe only

### Current problem

Sales and Inventory consumers have different business dispatch logic but repeat two technical blocks:

- PostgreSQL unique violation detection
- Kafka consumer tracing activity setup

### Decision

Extract only those technical blocks.

Create:

```text
BuildingBlocks.Infrastructure/Exceptions/PostgresExceptions.cs
BuildingBlocks.Infrastructure/Tracing/KafkaConsumerActivity.cs
```

### Required changes

- Move duplicate-key detection to `PostgresExceptions.IsUniqueViolation(DbUpdateException ex)`.
- Move consumer activity start logic to `KafkaConsumerActivity.Start(ActivitySource source, IMessageContext context)`.
- Update Sales and Inventory handlers to use these helpers.

### Must not change

- consumer business dispatch
- order status behavior
- inventory reservation behavior
- idempotency behavior
- logging behavior
- trace tags

### Explicitly forbidden

Do not introduce:

- base consumer handler
- template method
- generic consumer handler
- strategy wrapper
- shared business dispatch abstraction

### Validation

- `dotnet build`
- full test suite
- verify consumer behavior remains unchanged

## 14. Public API rules

Every new shared type must be `internal` by default.

Use `public` only when cross-assembly usage requires it.

Public types must be intentional shared APIs, not accidental implementation details.

Review every public type after each phase.

## 15. Naming rules

Use consistent names when the concept is identical:

- `OutboxMessage`, not `OutboxRow`
- `IOutboxPublisher`
- `KafkaOutboxPublisher`
- `OutboxMetrics`
- `InboxMetrics`
- `PostgresExceptions`
- `KafkaConsumerActivity`

Do not rename service-specific business concepts just for symmetry.

## 16. Verification after every phase

After each phase, run:

```bash
dotnet build
dotnet test
```

Also verify:

- architecture tests pass;
- no circular project reference exists;
- no Domain/Application dependency violation exists;
- no obsolete duplicated class remains;
- no unused using remains;
- no dead code remains;
- no unnecessary public API was introduced;
- namespace structure is clean;
- tests that reference moved types are updated without changing assertions.

For Phase 2 only, also run EF migration probe for both DbContexts and verify the generated migration is empty.

## 17. Phase completion checklist

A phase is complete only when all items are true:

- [ ] Build passes.
- [ ] Unit tests pass.
- [ ] Architecture tests pass.
- [ ] No migration change exists unless explicitly intended.
- [ ] No duplicate implementation remains for the extracted concern.
- [ ] No dead code remains.
- [ ] No unnecessary public API was introduced.
- [ ] Namespaces are clean.
- [ ] Existing behavior is unchanged.
- [ ] Phase notes are updated if implementation differs from this spec.

## 18. Implementation order

Implement in this order:

```text
Phase 1 -> Phase 2 -> Phase 3 -> Phase 4 -> Phase 5 -> Phase 7
```

Do not start the next phase until the previous phase passes validation.

## 19. Final acceptance criteria

The refactor is complete only when:

- Sales builds successfully.
- Inventory builds successfully.
- All tests pass.
- Architecture tests pass.
- No database schema change is generated.
- Outbox behavior is unchanged.
- Inbox behavior is unchanged.
- Kafka publish behavior is unchanged.
- Consumer behavior is unchanged.
- Metric names remain unchanged.
- Trace source names remain unchanged.
- Duplicated technical code listed in this spec has been removed.
- No extra abstraction was introduced beyond this spec.


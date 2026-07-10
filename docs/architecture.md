# Architecture and folder responsibilities

## Dependency rule

Dependencies always point inward:

`Api/Worker -> Infrastructure -> Application -> Domain`

`Domain` has no dependency on EF Core, Kafka, Redis, HTTP, MediatR, or another bounded context. `Application` may depend on Domain and defines ports; Infrastructure implements those ports. Cross-service communication uses contracts from `BuildingBlocks`, never direct project references between bounded contexts.

## Sales bounded context

### Sales.Domain

- `Aggregates`: consistency boundaries and state-changing behavior. `Order`, `Product`, and `Customer` are aggregate roots; `OrderLine` is controlled by `Order`.
- `Entities`: entities that have identity but are not aggregate roots. Keep this empty until such a concept exists; do not create folders merely to classify DTOs.
- `ValueObjects`: immutable concepts compared by value, such as `Money`.
- `Events`: facts raised by aggregates. They contain business data only and know nothing about Kafka topics.
- `Exceptions`: violations of domain invariants.
- `Repositories`: command-side aggregate persistence contracts only. They must not expose `DbSet`, `IQueryable`, or DTOs.
- `Services`: domain services only when behavior does not naturally belong to one aggregate.

### Sales.Application

- `Commands`: use cases that load aggregates, call domain behavior, and commit one unit of work.
- `Queries`: CQRS query handlers that call read-service ports; they never query EF directly.
- `DTOs`: API/read models and pagination contracts.
- `Interfaces`: ports implemented by Infrastructure, including Unit of Work, read services, cache, and execution context.
- `Services`: application-level orchestration and errors, not domain rules.
- `Validators`: request validation before a command reaches the aggregate. Domain invariants must still be enforced in Domain.

### Sales.Infrastructure

- `Persistence`: EF Core DbContext, entity configurations, migrations, and CQRS read-service implementations.
- `Repositories`: command-side repository implementations that rehydrate complete aggregates.
- `Kafka`: Domain Event to Integration Event mapping (`DomainEventMapper`) and the Inbox consumer (`SalesIntegrationEventHandler`). The Outbox entity, `IOutboxPublisher`/`KafkaOutboxPublisher`, and `EventEnvelopeFactory` now live in `BuildingBlocks.Infrastructure` (shared with Inventory); only the outbox-polling `SalesOutboxPublisher` `BackgroundService` stays local, kept separate from Inventory's equivalent on purpose (see `BuildingBlocks.Infrastructure` below).
- `Hangfire`: scheduled/recovery jobs. A Redis lock may coordinate jobs but cannot replace database correctness.
- `ExternalServices`: Redis cache and adapters for HTTP/runtime context.
- `UnitOfWork`: transaction-specific implementations when they are not implemented directly by the DbContext.

### Sales.Api

- `Controllers`: HTTP transport, authorization requirements, request parsing, ETag handling, and MediatR dispatch.
- `Middleware`: cross-cutting HTTP error handling and Problem Details.
- `Filters`: host-specific access filters such as the Hangfire dashboard rule.
- `Extensions`: composition-root bootstrap such as development identity seed data.
- `Program.cs`: dependency registration and pipeline composition only; no business rules.

Coding rules for API/controller code:

- Use ASP.NET Core MVC Controllers for HTTP APIs in `Sales.Api`; do not add new Minimal API endpoint groups for business APIs.
- Use traditional constructor injection with explicit private readonly fields, for example `private readonly ISender _sender;`.
- Do not use C# primary constructors in controllers or API-facing classes. Keep constructors explicit so dependency roles remain obvious in code review.

## Inventory bounded context

- `Inventory.Domain`: stock and reservation invariants. It has no dependency on Sales contracts.
- `Inventory.Application`: inventory use-case/read ports and DTOs.
- `Inventory.Infrastructure/Persistence`: Inventory database, Inbox, and migrations. The Outbox entity itself lives in `BuildingBlocks.Infrastructure` (shared with Sales — see below).
- `Inventory.Infrastructure/Kafka`: translates shared integration events into Inventory domain operations (`InventoryEventHandler`); the outbox-polling `InventoryOutboxPublisher` `BackgroundService` stays local and separate from Sales' equivalent.
- `Inventory.Infrastructure/Services`: implementations of Application ports.
- `Inventory.Api`: authentication, HTTP endpoints, health checks, and host lifecycle.

## AuditLog bounded context

- `AuditLog.Infrastructure/Mongo`: MongoDB document model, repository behavior, and unique `eventId` handling.
- `AuditLog.Worker`: Generic Host and Kafka consumer lifecycle only.

## Shared and tests

- `BuildingBlocks.Contracts`: versioned integration-event envelopes/contracts shared across processes. It must not contain domain models.
- `BuildingBlocks.Infrastructure`: shared Kafka/outbox infrastructure referenced only by `Sales.Infrastructure` and `Inventory.Infrastructure` (never by Domain/Application) — `Outbox/OutboxMessage` (the unified outbox row, mapped independently by each service's own `outbox_messages` table configuration) + `IOutboxPublisher`/`KafkaOutboxPublisher` (constructor-parameterized by producer name and `ActivitySource`), `Messaging/EventEnvelopeFactory`, and two purely-technical Kafka-consumer helpers, `Exceptions/PostgresExceptions.IsUniqueViolation` and `Tracing/KafkaConsumerActivity.Start`. Deliberately does **not** include the outbox-polling `BackgroundService` (`SalesOutboxPublisher`/`InventoryOutboxPublisher` stay separate, near-duplicate classes — considered too reliability-sensitive to merge) or a shared base class for the two Kafka consumer handlers (their dispatch logic is genuinely different business logic). See `docs/superpowers/specs/2026-07-09-shared-infrastructure-refactor-design.md` for the full rationale.
- `BuildingBlocks.Observability`: cross-cutting Serilog bootstrap (`SerilogBootstrap.ConfigureSharedSinks`) shared by Sales.Api, Inventory.Api, and AuditLog.Worker — Console + Seq + OTLP sinks, one policy instead of duplicated per-service setup. Also holds `Metrics/OutboxMetrics` + `Metrics/InboxMetrics` (reusable outbox/inbox counter-and-gauge groups that `SalesMetrics`/`InventoryMetrics` delegate to internally, keeping their own call sites unchanged) and `ObservabilityNames` (shared tracing-source-name constants used by both the DI-registered `ActivitySource` and each Api's `AddSource(...)` call, so the two can't silently drift apart). See [Seqlog-usage-guide.md](Seqlog-usage-guide.md) and [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md).
- `BuildingBlocks.Web`: cross-cutting ASP.NET Core middleware shared by Sales.Api and Inventory.Api (`RequestObservabilityMiddleware`, `RequestLoggingDefaults`) — HTTP request/response logging and correlation, not referenced by AuditLog.Worker since it has no HTTP surface.
- `Sales.Domain.Tests`: aggregate invariants and state transitions.
- `Sales.Application.Tests`: command/query orchestration with mocked ports.
- `Inventory.Tests`: stock and reservation invariants plus idempotency scenarios.
- `AuditLog.Tests`: Mongo idempotency and event mapping.
- `Sales.Architecture.Tests`: executable dependency rules preventing EF/Kafka leakage into Domain/Application and cross-context references.

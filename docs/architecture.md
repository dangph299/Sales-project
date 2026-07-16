# Architecture and folder responsibilities

## Dependency rule

Dependencies always point inward:

`Api/Worker -> Infrastructure -> Application -> Domain`

`Domain` has no dependency on EF Core, Kafka, Redis, HTTP, MediatR, or another bounded context. `Application` may depend on Domain and defines ports; Infrastructure implements those ports. Cross-service communication uses contracts from `BuildingBlocks`, never direct project references between bounded contexts. `Sales.Domain` and `Inventory.Domain` both depend on `BuildingBlocks.Domain` for the framework-independent base types (`AggregateRoot<TId>`, `Entity<TId>`, `IDomainEvent`/`DomainEvent`, `DomainException`); `Sales.Application` and `Inventory.Application` both depend on `BuildingBlocks.Application` for CQRS markers, shared MediatR pipeline behaviors, `IUnitOfWork`, and pagination/time helpers where needed — see `BuildingBlocks.Domain`/`BuildingBlocks.Application` under "Shared and tests" below.

## Sales bounded context

### Sales.Domain

- `Aggregates`: consistency boundaries and state-changing behavior. `Order`, `Product`, and `Customer` are aggregate roots (derive from `BuildingBlocks.Domain.AggregateRoot<TId>`); `OrderLine` is controlled by `Order`.
- `Entities`: entities that have identity but are not aggregate roots. Keep this empty until such a concept exists; do not create folders merely to classify DTOs.
- `ValueObjects`: immutable concepts compared by value, such as `Money`.
- `Events`: facts raised by aggregates, implementing `BuildingBlocks.Domain.IDomainEvent`. They contain business data only and know nothing about Kafka topics.
- `Exceptions`: violations of domain invariants raised as `BuildingBlocks.Domain.DomainException` (or a subtype of it).
- `Repositories`: command-side aggregate persistence contracts only. They must not expose `DbSet`, `IQueryable`, or DTOs.
- `Services`: domain services only when behavior does not naturally belong to one aggregate.

Note: `AggregateRoot<TId>`, `Entity<TId>`, `IDomainEvent`/`DomainEvent`, and `DomainException` themselves live in `BuildingBlocks.Domain` (shared with Inventory), not in `Sales.Domain` — see "Shared and tests" below.

### Sales.Application

- `Commands`: use cases that load aggregates, call domain behavior, and commit one unit of work.
- `Queries`: CQRS query handlers that call read-service ports; they never query EF directly.
- `DTOs`: API/read models.
- `Interfaces`: ports implemented by Infrastructure, including read services, cache, and execution context. `IUnitOfWork` and pagination (`PagedResult<T>`/`Paging`) live in `BuildingBlocks.Application` instead (shared shape, no Sales-specific behavior).
- `Services`: application-level orchestration and errors, not domain rules. `SalesApplicationExceptionClassifier` extends the shared `DefaultApplicationExceptionClassifier` (from `BuildingBlocks.Application`) with Sales-specific expected exceptions (`NotFoundException`, `ConflictException`).
- `Validators`: request validation before a command reaches the aggregate. Domain invariants must still be enforced in Domain.

Note: the MediatR pipeline behaviors (`ErrorLoggingBehavior`, `LoggingBehavior`, `ValidationBehavior`) live in `BuildingBlocks.Application/Behaviors` and are registered via `AddApplicationBuildingBlocks()`, called from `Sales.Application`'s own `AddSalesApplication()` — see `BuildingBlocks.Application` below.

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

- `Inventory.Domain`: stock and reservation invariants, built on `BuildingBlocks.Domain` (`AggregateRoot<TId>`/`IEntity<TId>`) for `Reservation`/`InventoryItem`. It has no dependency on Sales contracts.
- `Inventory.Application`: inventory commands, queries, handlers, validators, use-case/read ports, DTOs, and the Inventory-specific transaction behavior for idempotent commands.
- `Inventory.Infrastructure/Persistence`: Inventory database, Inbox, and migrations. The Outbox entity itself lives in `BuildingBlocks.Infrastructure` (shared with Sales — see below).
- `Inventory.Infrastructure/Kafka`: technical adapters that consume shared integration events and dispatch Inventory commands through MediatR; the outbox-polling `InventoryOutboxPublisher` `BackgroundService` stays local and separate from Sales' equivalent.
- `Inventory.Infrastructure/Persistence`: implementations of Application ports such as repositories, read services, Inbox/Outbox persistence, and transaction management.
- `Inventory.Api`: authentication, controllers, MediatR dispatch, health checks, and host lifecycle.

## AuditLog bounded context

- `AuditLog.Infrastructure/Mongo`: MongoDB document model, repository behavior, and unique `eventId` handling.
- `AuditLog.Worker`: Generic Host and Kafka consumer lifecycle only.

## Shared and tests

- `BuildingBlocks.Domain`: framework-independent domain base types shared by `Sales.Domain` and `Inventory.Domain` — `Abstractions/AggregateRoot<TId>` (buffers `IDomainEvent`s, exposes `Version`/`UpdatedAt`, `Touch()`), `Abstractions/Entity<TId>`/`IEntity<TId>`/`IAggregateRoot`, `Abstractions/IDomainEvent`/`DomainEvent`, and `Exceptions/DomainException`. It has zero package dependencies beyond the BCL — no EF Core, MediatR, Kafka, or ASP.NET Core — enforced by `Sales.Architecture.Tests.DependencyRulesTests.BuildingBlocks_domain_is_framework_independent`.
- `BuildingBlocks.Application`: shared Application-layer building blocks referenced by Sales and Inventory Application layers — `Behaviors/ErrorLoggingBehavior`, `Behaviors/LoggingBehavior`, `Behaviors/PerformanceBehavior`, `Behaviors/ValidationBehavior`, `Persistence/IUnitOfWork`, `Pagination/PagedResult`/`Pagination/Paging`, `Abstractions/Time/IClock` + `SystemClock`, CQRS markers (`ICommand`, `ICommand<T>`, `IQuery<T>`), `Exceptions/IApplicationExceptionClassifier`/`DefaultApplicationExceptionClassifier`, and the `AddApplicationBuildingBlocks()` DI extension. Must not depend on `BuildingBlocks.Infrastructure`/`BuildingBlocks.Web`, EF Core, or another bounded context — enforced by `DependencyRulesTests.BuildingBlocks_application_does_not_depend_on_infrastructure_or_web`.
- `BuildingBlocks.Contracts`: versioned integration-event envelopes/contracts shared across processes. It must not contain domain models.
- `BuildingBlocks.Infrastructure`: shared Kafka/outbox infrastructure referenced only by `Sales.Infrastructure` and `Inventory.Infrastructure` (never by Domain/Application) — `Outbox/OutboxMessage` (the unified outbox row, mapped independently by each service's own `outbox_messages` table configuration) + `IOutboxPublisher`/`KafkaOutboxPublisher` (constructor-parameterized by producer name and `ActivitySource`), `Messaging/EventEnvelopeFactory`, `Messaging/AuditChangeDetector` (shared by both services' Kafka handlers to detect field-level changes for the audit-log integration event), and two purely-technical Kafka-consumer helpers, `Exceptions/PostgresExceptions.IsUniqueViolation` and `Tracing/KafkaConsumerActivity.Start`. Deliberately does **not** include the outbox-polling `BackgroundService` (`SalesOutboxPublisher`/`InventoryOutboxPublisher` stay separate, near-duplicate classes — considered too reliability-sensitive to merge) or a shared base class for the two Kafka consumer handlers (their dispatch logic is genuinely different business logic). See `docs/superpowers/specs/2026-07-09-shared-infrastructure-refactor-design.md` for the full rationale.
- `BuildingBlocks.Observability`: cross-cutting observability capability project owning the shared Serilog sink policy (`SerilogBootstrap.ConfigureSharedSinks` — Console + Seq + OTLP) and the base OpenTelemetry pipeline (OTLP export + runtime instrumentation). Exposed through `ObservabilityRegistration`: `AddBuildingBlocksLogging(serviceName)` and `AddBuildingBlocksObservability(...)`, shared by Sales.Api, Inventory.Api, and AuditLog.Worker — one policy instead of duplicated per-service setup. (The reusable `Observability/Metrics/OutboxMetrics` + `InboxMetrics` counter-and-gauge groups that `SalesMetrics`/`InventoryMetrics` delegate to internally live in `BuildingBlocks.Infrastructure`, not here.) Service-specific tracing source names are owned by each service Infrastructure project (`SalesObservability`, `InventoryObservability`) so Shared does not contain service names. See [Seqlog-usage-guide.md](Seqlog-usage-guide.md) and [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md).
- `BuildingBlocks.Web`: cross-cutting ASP.NET Core host wiring shared by Sales.Api and Inventory.Api — the `AddBuildingBlocksWeb(configuration, options)` / `UseBuildingBlocksRequestPipeline()` facade (`WebHostRegistration` + `BuildingBlocksWebOptions`) covering problem-details/exception handling, Swagger docs, JWT auth, and web-layer OpenTelemetry instrumentation (`AddBuildingBlocksWebObservability`, layered on the `BuildingBlocks.Observability` base), plus the `RequestObservabilityMiddleware` / `RequestLoggingDefaults` middleware for HTTP request/response logging and correlation. Not referenced by AuditLog.Worker since it has no HTTP surface.
- `Sales.Domain.Tests`: aggregate invariants and state transitions.
- `Sales.Application.Tests`: command/query orchestration with mocked ports.
- `Inventory.Tests`: stock and reservation invariants plus idempotency scenarios.
- `AuditLog.Tests`: Mongo idempotency and event mapping.
- `Sales.Architecture.Tests`: executable dependency rules preventing EF/Kafka leakage into Domain/Application and cross-context references, and framework-independence of `BuildingBlocks.Domain`/`BuildingBlocks.Application`.

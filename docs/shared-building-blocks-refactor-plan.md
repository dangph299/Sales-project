# Shared BuildingBlocks Refactor Plan

## A. Current-State Analysis

### Shared projects

The solution currently contains these real Shared projects:

| Project | Current role | Main dependencies |
| --- | --- | --- |
| `BuildingBlocks.Domain` | Entity, aggregate root, domain event, domain exception | none beyond BCL |
| `BuildingBlocks.Application` | MediatR pipeline behaviors, validation, pagination, `IUnitOfWork` | `BuildingBlocks.Domain`, MediatR, FluentValidation, logging abstractions |
| `BuildingBlocks.Contracts` | Integration event payloads, event envelope, Kafka names/headers, trace parser | BCL only |
| `BuildingBlocks.Infrastructure` | Outbox row, Kafka publisher adapter, Kafka consumer tracing helper, audit diff, Postgres exception helper, Serilog bootstrap, common metric instruments/names | `BuildingBlocks.Contracts`, KafkaFlow, EF Core, Npgsql, Serilog, logging abstractions |
| `BuildingBlocks.Web` | ASP.NET Core JWT, Swagger/OpenAPI, request observability, OpenTelemetry | ASP.NET Core framework reference, Serilog.AspNetCore, OpenTelemetry, Swashbuckle |

The stale `obj/`-only folders that previously existed under `src/Shared` (`BuildingBlocks.Auditing`, `BuildingBlocks.Messaging`, `BuildingBlocks.Outbox`, `BuildingBlocks.Persistence`) have been deleted during cleanup.

### Dependency graph

Current intended graph:

```text
BuildingBlocks.Domain
  <- BuildingBlocks.Application

BuildingBlocks.Contracts
  <- BuildingBlocks.Infrastructure

BuildingBlocks.Web
```

Service usage:

```text
Sales.Domain       -> BuildingBlocks.Domain
Sales.Application  -> Sales.Domain, BuildingBlocks.Application
Sales.Infrastructure -> Sales.Application, Sales.Domain, BuildingBlocks.Contracts,
                        BuildingBlocks.Infrastructure
Sales.Api          -> Sales.Application, Sales.Infrastructure,
                      BuildingBlocks.Infrastructure, BuildingBlocks.Web

Inventory.Domain   -> BuildingBlocks.Domain
Inventory.Application -> Inventory.Domain
Inventory.Infrastructure -> Inventory.Domain, Inventory.Application,
                            BuildingBlocks.Contracts, BuildingBlocks.Domain,
                            BuildingBlocks.Infrastructure
Inventory.Api      -> Inventory.Application, Inventory.Infrastructure,
                      BuildingBlocks.Infrastructure, BuildingBlocks.Web

AuditLog.Infrastructure -> BuildingBlocks.Contracts
AuditLog.Worker    -> AuditLog.Infrastructure, BuildingBlocks.Infrastructure
```

No Shared project references Sales, Inventory, or AuditLog.

### Duplicate code

- `SalesOutboxPublisher` and `InventoryOutboxPublisher` are near-duplicates: claim rows, lock rows, publish, retry/backoff, dead-letter, update metrics.
- `SalesIntegrationEventHandler`, `InventoryEventHandler`, and `AuditEventHandler` repeat Kafka consume logging scope setup and tracing/log shape.
- Sales and Inventory each configure the shared `OutboxMessage` EF mapping separately; this is acceptable for service-owned tables, but common configuration conventions could be extracted later if duplication grows.
- Inbox idempotency is implemented separately as `Sales.Infrastructure.InboxMessage` and `Inventory.Infrastructure.Persistence.Inbox.InboxRow`; a shared abstraction is missing.

### Code in the wrong layer

- `BuildingBlocks.Contracts.KafkaTopics` and `KafkaConsumerGroups` contain business/service-specific topic and group names. These are stable cross-service contracts today, but the names are Kafka-specific and business-specific; they should move to service contract constants or infrastructure configuration during the contracts phase.
- `TraceContextParser` has been moved from `BuildingBlocks.Contracts` to `BuildingBlocks.Infrastructure`, because it is transport/observability behavior rather than a pure message contract.
- `BuildingBlocks.Domain.AggregateRoot<TId>` owns `Version`, `UpdatedAt`, and calls `DateTimeOffset.UtcNow` in `Touch()`. Versioning may be useful across aggregates, but direct clock access in shared domain primitives is a rule violation when time affects business behavior.
- `Sales.Api.Controllers.AuthController` uses `SalesDbContext` directly. This is outside Shared, but it is a local Clean Architecture exception worth tracking separately.

### Too many responsibilities

- `SalesOutboxPublisher` and `InventoryOutboxPublisher` combine background loop, outbox querying, locking, publishing, retry policy, state transitions, logging, and metrics.
- Kafka consumer handlers combine transport concerns, logging context, tracing, idempotency, transaction boundary, deserialization, and application state transitions.
- `KafkaOutboxPublisher` combines envelope deserialization, activity creation, header propagation, transport publish, and success logging. Logging/metrics are still decorator candidates.

### Direct framework dependencies in abstractions

- Domain has no EF/Kafka/Hangfire/Serilog/ASP.NET references.
- Application has MediatR and FluentValidation by design; no EF/Kafka/Hangfire/ASP.NET references. `DefaultApplicationExceptionClassifier` detects EF concurrency by string type name, avoiding a hard EF reference.
- Contracts has no EF/Kafka/ASP.NET references, but has Kafka-specific constant classes by name and semantics.
- Infrastructure correctly contains EF Core, KafkaFlow, Npgsql, and logging abstractions.
- Web correctly contains ASP.NET Core, Serilog.AspNetCore, OpenTelemetry, and Swagger.

### Shared code used by only one service

- `BuildingBlocks.Web.Authentication.JwtAuthenticationExtensions` is currently used by API hosts; keep in Web.
- `BuildingBlocks.Infrastructure.PostgresExceptions` is used by Sales and Inventory consumers; keep.
- `BuildingBlocks.Infrastructure.AuditChangeDetector` is currently used by Sales and Inventory audit event production and tested by AuditLog tests; keep in Infrastructure for now.
- `BuildingBlocks.Infrastructure.OutboxMessage` is used by Sales and Inventory; keep, but separate store/processor abstractions before moving more logic into Shared.

### Over-abstraction

- No excessive generic repository was added to Shared. Sales has a local generic `IRepository<T>` and implementation; this should stay service-local until Inventory has the same need.
- Observability has been folded into `BuildingBlocks.Infrastructure/Observability` because it only contained infrastructure-level Serilog and metric helpers.

### Under-abstraction

- Added and migrated `IClock`, CQRS marker interfaces, and message log context abstractions. Removed `ValueObject`, `Result/Error`, `ICurrentUser`, transaction marker/behavior, and Outbox/Inbox store abstractions because no service had been migrated to use them yet.
- Missing decorator chain around `IOutboxPublisher`/future `IEventPublisher` for logging and metrics.
- Missing architecture tests for Contracts, Infrastructure, Web, and Observability dependency isolation.

### Keep as-is for now

- `Entity<TId>`, `IEntity<TId>`, `IDomainEvent`, `DomainException`.
- `IUnitOfWork` abstraction.
- `ValidationBehavior`, `LoggingBehavior`, `ErrorLoggingBehavior` registration order.
- `OutboxMessage` as a shared persistence model, because Sales and Inventory both use it.
- `BuildingBlocks.Web` OpenAPI, JWT, request observability, and OpenTelemetry helpers.

### Move later

- `KafkaTopics` and `KafkaConsumerGroups`: move out of pure Contracts into service-specific contract/config modules, or rename to explicit transport contract constants if the team chooses to keep Kafka topic names as public integration contract.
- `TraceContextParser`: moved from Contracts to Infrastructure diagnostics.
- Kafka consume log-context code: moved to a shared Infrastructure logging context abstraction.
- Outbox publishing loops: extract service-local stores plus shared processor/decorators.

### Split later

- `KafkaOutboxPublisher`: split transport publish, tracing/header propagation, logging, and metrics.
- Consumer handlers: split transport handler, inbox idempotency, use-case handling, and logging/tracing decorators.
- `AggregateRoot<TId>`: separate generic aggregate event buffering from optional timestamp/version behavior if not every bounded context needs optimistic version semantics.

### Delete or merge later

- Stale `obj/`-only Shared folders have been deleted.
- Do not create `BuildingBlocks.Auditing`, `BuildingBlocks.Messaging`, `BuildingBlocks.Outbox`, or `BuildingBlocks.Persistence` until package/dependency boundaries justify the extra projects.

## B. Target Architecture

### Project structure

Keep the current six-project shape for now:

```text
src/Shared/
  BuildingBlocks.Domain/
  BuildingBlocks.Application/
  BuildingBlocks.Contracts/
  BuildingBlocks.Infrastructure/
  BuildingBlocks.Web/
```

Do not add more Shared projects during the first migration unless package isolation is proven useful.

### Target dependency graph

```text
BuildingBlocks.Domain
  <- BuildingBlocks.Application
       <- service Application projects

BuildingBlocks.Contracts
  <- service Infrastructure projects
  <- BuildingBlocks.Infrastructure

BuildingBlocks.Infrastructure
  -> BuildingBlocks.Contracts
  -> optional BuildingBlocks.Application abstractions only when needed

BuildingBlocks.Web
  -> ASP.NET Core packages only
```

Strict rules:

- Domain has no framework or service dependencies.
- Application has no Infrastructure/Web/service implementation dependencies.
- Contracts has no Infrastructure/Web/Application handler/domain entity dependencies.
- Infrastructure and Web have no Sales/Inventory/AuditLog dependencies.
- Service Domain projects do not reference Infrastructure/API.

### Public abstractions

Target public surface:

- Domain: `IEntity<TId>`, `Entity<TId>`, `IAggregateRoot`, `AggregateRoot<TId>`, `IDomainEvent`, `DomainEvent`, `DomainException`.
- Application: `IUnitOfWork`, `IClock`, `PagedResult<T>`, `ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>`.
- Contracts: `IIntegrationEvent`, `IntegrationEvent`, `EventEnvelope<TMessage>` or an evolved non-generic envelope, `EventMetadata`, `MessageHeaders`, audit payload records.
- Infrastructure: `IMessageLogContext`; future Outbox/Inbox abstractions should be added only in the same phase as service stores/processors are migrated.

### Internal implementations

- Kafka adapters, envelope serializers, Kafka tracing/header propagation.
- Serilog message log context implementation.
- EF/Postgres outbox and inbox stores when they can be made generic without leaking service `DbContext`.
- Outbox processor implementation.
- Logging/metrics decorators.
- Web middleware implementations that are not part of host composition API.

### Decorators to create

- `LoggingOutboxProcessorDecorator`
- `MetricsOutboxProcessorDecorator`
- `LoggingOutboxPublisherDecorator`
- `MetricsOutboxPublisherDecorator`
- `MessageLogContextConsumerDecorator` or equivalent composition for Kafka consumers
- Optional `LoggingAuditWriterDecorator` around `IAuditWriter`

### Pipeline behaviors

Keep current:

```text
ErrorLoggingBehavior
  -> LoggingBehavior
    -> ValidationBehavior
      -> Handler
```

Add later:

- `PerformanceBehavior`
- Transaction behavior is deferred until handlers stop owning `SaveChangesAsync` or a transaction-only behavior is introduced.
- Optional idempotency behavior for commands only after request identity is modeled.

## C. Migration Plan

| Task | Current file | Target file | Dependency/namespace change | Risk | Verify |
| --- | --- | --- | --- | --- | --- |
| Add missing architecture rules | `tests/Sales.Architecture.Tests/DependencyRulesTests.cs` | same | Add direct test references to Shared projects | Low | `dotnet test tests/Sales.Architecture.Tests/Sales.Architecture.Tests.csproj --no-restore` |
| Add `ValueObject` | none | deferred | Removed until a real Sales/Inventory value object migrates to it | Low | Domain unit tests when reintroduced |
| Add `Result/Error` | none | deferred | Removed until handlers/controllers migrate to result-based outcomes | Medium | Application/API tests when reintroduced |
| Remove direct clock from aggregate primitive | `AggregateRoot.cs` | same or split timestamp base | Keep Domain BCL-only | Medium; migrations/tests may assume `UpdatedAt` behavior | Domain tests, Sales/Inventory tests |
| Add `IClock` | none | `BuildingBlocks.Application/Abstractions/Time` | Application only | Low | build, DI tests |
| Add command/query markers | none | `BuildingBlocks.Application/Abstractions/Messaging/*` | Application references MediatR as today | Medium; service migration may be broad | Application tests |
| Add transaction behavior | none | deferred | Removed until SaveChanges ownership is changed | Medium; ordering and command selection matter | handler tests, integration tests |
| Move trace parser | `BuildingBlocks.Contracts/Messaging/TraceContextParser.cs` | `BuildingBlocks.Infrastructure/Messaging/TraceContextParser.cs` | Contracts sheds diagnostics concern | Low; updated `KafkaConsumerActivity` and AuditLog consumer | architecture tests |
| Move or reclassify topic/group constants | `BuildingBlocks.Contracts/Messaging/KafkaTopics.cs`, `KafkaConsumerGroups.cs` | service Infrastructure config or explicit integration contract constants | Contracts becomes less Kafka-specific | High; touches every service Kafka registration | integration tests, Playwright Kafka specs |
| Extract message log context | repeated `LogContext.PushProperty` in consumer handlers | `BuildingBlocks.Infrastructure/Messaging/Logging/*` | Serilog implementation in Infrastructure/Observability only | Medium; log shape must stay compatible | consumer tests, log assertions if any |
| Extract outbox store/processor | `SalesOutboxPublisher.cs`, `InventoryOutboxPublisher.cs` | deferred | Add shared abstractions only with service stores/processors in the same phase | High; reliability behavior must not regress | outbox reliability tests |
| Add publisher decorators | `KafkaOutboxPublisher.cs` | decorators in `BuildingBlocks.Infrastructure/Messaging/Publishing` | DI registration order becomes explicit | Medium | unit tests for decorator order/behavior |
| Add inbox abstraction | `Sales.Infrastructure/Persistence/Inbox/InboxMessage.cs`, `Inventory.Infrastructure/Persistence/Inbox/InboxRow.cs` | deferred | Add only when consumers stop touching DbContext inbox sets directly | Medium | consumer idempotency tests |
| Cleanup stale folders | `src/Shared/BuildingBlocks.{Auditing,Messaging,Outbox,Persistence}/obj` | delete folders | no project dependency change | Low | `dotnet sln Sales.sln list`, build |

Recommended phase commands:

```bash
dotnet build Sales.sln
dotnet test Sales.sln
dotnet test tests/Sales.Architecture.Tests/Sales.Architecture.Tests.csproj --no-restore
```

Phase order:

1. Lock architecture tests and document baseline.
2. Add Domain primitives with tests.
3. Add Application abstractions/behaviors without migrating all handlers.
4. Clean Contracts transport leakage in small commits.
5. Extract Outbox/Inbox processors behind service-local stores.
6. Refactor Web only where duplication appears in API hosts.
7. Migrate Sales, then Inventory, then AuditLog.
8. Delete stale folders and unused references/packages.

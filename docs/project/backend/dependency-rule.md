# Dependency Rules

## Allowed project references

| Project | May reference |
|---|---|
| `<Service>.Domain` | `BuildingBlocks.Domain` |
| `<Service>.Application` | own Domain, `BuildingBlocks.Application`, `BuildingBlocks.Contracts` (Inventory only) |
| `<Service>.Infrastructure` | own Application + Domain, `BuildingBlocks.Application`, `BuildingBlocks.Contracts`, `BuildingBlocks.Domain`, `BuildingBlocks.Infrastructure` |
| `<Service>.Api` | own Application + Infrastructure, `BuildingBlocks.Web`, `BuildingBlocks.Observability`, `BuildingBlocks.Contracts`, `BuildingBlocks.Infrastructure` |
| `AuditLog.Worker` | `AuditLog.Infrastructure`, `BuildingBlocks.Infrastructure`, `BuildingBlocks.Observability` |

Anything not listed is forbidden and will fail `Sales.Architecture.Tests`.

## Banned dependencies

- Domain: no `Microsoft.EntityFrameworkCore`, `Npgsql`, `KafkaFlow`, `Confluent.Kafka`, `Hangfire`, `StackExchange.Redis`, `MongoDB.Driver`, `MediatR`, `Serilog`, `Microsoft.AspNetCore`, no other bounded context.
- Application: no `Microsoft.EntityFrameworkCore`, `Npgsql`, `KafkaFlow`, `Confluent.Kafka`, `Hangfire`, `StackExchange.Redis`, `MongoDB.Driver`, `BuildingBlocks.Infrastructure`, `BuildingBlocks.Web`, no other bounded context.

## External library boundaries

- Domain and Application may only depend on: BCL/`System.*`, the owning service's own code, `BuildingBlocks.Domain`, `BuildingBlocks.Application` (Application only), and the application-level libraries this solution already standardizes on — `MediatR`, `FluentValidation`, `Mapster`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options` (for `IOptions<T>`). Adding a *new* application-level package to that list is an architecture decision, not a per-feature one — raise it, don't add it silently.
- Every other infrastructure library (EF Core providers, Npgsql, KafkaFlow/Confluent.Kafka, Hangfire, StackExchange.Redis, MongoDB.Driver, OpenTelemetry SDK packages, Serilog sinks) is confined to `BuildingBlocks.Infrastructure`, `<Service>.Infrastructure`, or an API/Worker composition root (`Program.cs` via its `Add<Layer>` extension). Enforced by `Sales.Architecture.Tests` (`DependencyRulesTests`); a new banned-library gap gets a new assertion in the relevant test, not a comment.
- When Application needs an infrastructure capability, add a port (see [Ports and adapters](#ports-and-adapters)) rather than reaching for the vendor type. Don't create a wrapper that only renames or re-exposes the underlying type (e.g. an `IRedisClient` that hands back `IDatabase`) — a port earns its place by hiding the vendor type, standardizing a cross-cutting policy (retry, idempotency, transaction, serialization), or having more than one implementation. `IDistributedCache`-backed caches, `ICacheService<T>`/`IProductCache`, and the Hangfire `*JobBase` classes in `BuildingBlocks.Infrastructure/Hangfire` are the existing examples to follow.
- Don't hide a banned dependency behind a `GlobalUsings.cs` entry, a type alias, or a fully-qualified name instead of a `using` — the architecture tests check assembly-level type dependencies, not source text, so this doesn't work anyway and just makes the violation harder to find in review. `GlobalUsings.cs` in a Domain/Application project may only list that project's own namespaces plus `BuildingBlocks.Domain`/`BuildingBlocks.Application`/`BuildingBlocks.Contracts`.
- A capability shared by two or more services (Hangfire recurring-job wiring, outbox/inbox, Kafka registration, distributed cache) belongs in `BuildingBlocks.Infrastructure`. A capability owned by one service (its specific recurring jobs, its read services, its repositories) stays in that service's own `Infrastructure` project even if the pattern looks similar to another service's.
- `BuildingBlocks.Domain`: BCL only.
- `BuildingBlocks.Contracts`: no other BuildingBlocks project, no transport library, no trace-parsing behavior.
- `BuildingBlocks.Infrastructure`: no service assembly, no `BuildingBlocks.Web`, no `Microsoft.AspNetCore`.
- `BuildingBlocks.Web`: no service assembly, no `BuildingBlocks.Domain`/`Infrastructure`, no `KafkaFlow`, no `Hangfire`.
- `BuildingBlocks.Web.ExceptionHandling`: no `Microsoft.EntityFrameworkCore`, no `Npgsql`. Provider-specific persistence failures reach it only through `IPersistenceExceptionClassifier`.

## Ports and adapters

- Application declares an interface; Infrastructure implements it and registers it.
- Application never references an Infrastructure type, not even in a generic argument.
- When Application needs an infrastructure capability, add a port under `Features/<Aggregate>/Interfaces/` (feature-scoped) or `Common/Interfaces/` (cross-feature).

Existing ports: `IUnitOfWork`, `IClock`, `IExecutionContext`, `ICacheService<T>`, `IProductCache`, `I*ReadService`, `I*CodeGenerator`, `IOrderRealtimeNotifier`, `IInventoryInbox`, `IInventoryEventOutbox`, `IInventoryTransactionManager`, `IInventoryMetrics`, `IOutboxPublisher`, `IOutboxSignal`, `IIntegrationEventProcessor`, `IInboxFailureRecorder`, `IAuditWriter`, `IErrorCatalog`, `IErrorMessageProvider`, `IPersistenceExceptionClassifier`.

## Cross-context rules

- No project reference between Sales, Inventory, and AuditLog.
- Cross-context data travels only as `BuildingBlocks.Contracts` integration events inside an `EventEnvelope`.
- A contract change that breaks consumers ships as a new `.v2` topic, never as a mutated `.v1` payload.

## Service lifetimes

| Lifetime | Use for |
|---|---|
| Singleton | `IClock`, `ActivitySource`, `IOutboxSignal`, `IConnectionMultiplexer`, `IErrorCatalog`, `IErrorMessageProvider`, `IPersistenceExceptionClassifier`, `IInventoryMetrics`, `TypeAdapterConfig` |
| Scoped | `DbContext`, repositories, read services, `IUnitOfWork`, `IMapper`, `IExecutionContext`, audit services, MediatR behaviors, Hangfire job classes |
| Hosted | outbox publishers, inbox re-drive services, maintenance workers, Kafka bus lifecycle |

- Never inject a scoped service into a singleton or a `BackgroundService` constructor. Resolve it per cycle through `IServiceScopeFactory.CreateAsyncScope()`.

## Related

- [architecture.md](architecture.md)

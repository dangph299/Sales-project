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

- Domain: no `Microsoft.EntityFrameworkCore`, `KafkaFlow`, `MediatR`, `Serilog`, `Microsoft.AspNetCore`, no other bounded context.
- Application: no `Microsoft.EntityFrameworkCore`, `KafkaFlow`, `BuildingBlocks.Infrastructure`, `BuildingBlocks.Web`, no other bounded context.
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

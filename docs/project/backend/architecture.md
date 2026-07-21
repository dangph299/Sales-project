# Backend Architecture Rules

Rules only. Concepts are explained in [../../guides/02-solution-structure.md](../../guides/02-solution-structure.md).

## Layering

Dependencies point inward only:

```
Api / Worker  ->  Infrastructure  ->  Application  ->  Domain
```

- Domain depends on `BuildingBlocks.Domain` and nothing else.
- Application depends on Domain + `BuildingBlocks.Application` (+ `BuildingBlocks.Contracts` in Inventory only).
- Infrastructure depends on Application, Domain, `BuildingBlocks.Infrastructure`, `BuildingBlocks.Contracts`.
- Api depends on Application, Infrastructure, `BuildingBlocks.Web`, `BuildingBlocks.Observability`.

## Bounded contexts

- Three contexts: `Sales`, `Inventory`, `AuditLog`. Each owns its own database.
- Never add a project reference between two bounded contexts.
- Cross-context communication happens only through `BuildingBlocks.Contracts` integration events over Kafka.
- Never share an aggregate, entity, enum, or DbContext across contexts.

## Where code goes

| Concern | Project |
|---|---|
| Invariants, state transitions, domain events | `<Service>.Domain` |
| Use cases, commands, queries, validators, ports, DTOs | `<Service>.Application` |
| EF Core, Kafka, Redis, Hangfire, Mongo, port implementations | `<Service>.Infrastructure` |
| Controllers, middleware, filters, SignalR hubs, composition root | `<Service>.Api` |
| Reusable, business-free infrastructure | `src/Shared/BuildingBlocks.*` |

## BuildingBlocks placement

| Project | Put here | Never put here |
|---|---|---|
| `BuildingBlocks.Domain` | `AggregateRoot<TId>`, `Entity<TId>`, `IDomainEvent`, `DomainException` | Any package reference beyond the BCL |
| `BuildingBlocks.Application` | CQRS markers, MediatR behaviors, `IUnitOfWork`, `IClock`, `PagedResult<T>`, Mapster registration | EF Core, Kafka, ASP.NET Core |
| `BuildingBlocks.Contracts` | Integration events, `EventEnvelope`, `AuditLogEvent`, `KafkaTopics`, `ErrorCodes`, `ErrorCatalog` | Domain models, transport behavior, trace parsing |
| `BuildingBlocks.Infrastructure` | Outbox/Inbox rows and services, `KafkaOutboxPublisher`, EF audit interceptor, `RetryBackoff`, Hangfire scheduling helpers | Service names, ASP.NET Core, business rules |
| `BuildingBlocks.Observability` | Serilog sink policy, base OpenTelemetry pipeline | Service-specific meter/activity-source names |
| `BuildingBlocks.Web` | `AddBuildingBlocksWeb`, exception handling, API response models, Swagger, JWT, request middleware | `BuildingBlocks.Domain`/`Infrastructure`, Kafka, Hangfire |

## Composition root

- One `AddApplicationServices(this WebApplicationBuilder)` per API host, in `<Service>.Api/Extensions/ServiceCollectionExtensions.cs`.
- One `ConfigureApplication(this WebApplication)` per API host, in `<Service>.Api/Extensions/ApplicationBuilderExtensions.cs`.
- `Program.cs` contains only builder → configure → startup tasks → run.
- Each layer exposes exactly one `Add<Layer>` extension: `AddSalesApplication`, `AddSalesInfrastructure`, `AddInventoryApplication`, `AddInventoryInfrastructure`, `AddAuditLogInfrastructure`, `AddAuditLogWorker`.
- Split large registration bodies into `private static IServiceCollection Add<Capability>(...)` helpers.

## Enforcement

`tests/Sales.Architecture.Tests/DependencyRulesTests.cs` fails the build when these rules are broken. Add a test there when adding a new layering rule.

## Related

- [dependency-rule.md](dependency-rule.md)
- [folder-structure.md](folder-structure.md)
- [checklist.md](checklist.md)

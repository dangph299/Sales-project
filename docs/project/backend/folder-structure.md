# Folder Structure Rules

## Solution

```
src/
  Services/
    Sales/       Sales.Domain  Sales.Application  Sales.Infrastructure  Sales.Api
    Inventory/   Inventory.Domain  Inventory.Application  Inventory.Infrastructure  Inventory.Api
    AuditLog/    AuditLog.Infrastructure  AuditLog.Worker
  Shared/
    BuildingBlocks.Domain  BuildingBlocks.Application  BuildingBlocks.Contracts
    BuildingBlocks.Infrastructure  BuildingBlocks.Observability  BuildingBlocks.Web
  Web/Sales.Web        Angular client
tests/                 one xUnit project per production project + tests/Playwright
docker/                compose stack, otel/apm config, Kafka topic init, seed SQL
docs/                  guides | tech | project | superpowers
```

## `<Service>.Domain`

```
Aggregates/     Entities/     ValueObjects/     Enums/
Events/<Aggregate>s/          Repositories/     Services/
Services/Specifications/      Exceptions/       GlobalUsings.cs
```

- Aggregate roots go in `Aggregates/`; non-root entities in `Entities/`.
- Do not create an empty folder to "classify" a concept that does not exist yet.

## `<Service>.Application` — feature-first

```
Common/Interfaces/       cross-feature ports (IExecutionContext, ICacheService<T>)
Common/Exceptions/       NotFoundException, ConflictException
Common/Extensions/       shared FluentValidation rule extensions
Common/Behaviors/        service-specific MediatR behaviors (Inventory only)
Features/<Aggregate>/
  Commands/    one file per command + one file per handler
  Queries/     one file per query + one file per handler
  DTOs/
  Interfaces/  feature-scoped ports (read service, cache, code generator)
  Mapping/     Mapster IRegister
  Validators/
  Enums/       feature-scoped enums (e.g. PhoneMatch)
  Realtime/    realtime notification ports + payloads (Orders only)
DependencyInjection.cs
GlobalUsings.cs
```

- A new use case never adds a new top-level folder; it adds files inside the owning `Features/<Aggregate>/` folder.
- Command and its handler live in **separate files** (`CreateOrder.cs`, `CreateOrderHandler.cs`).
- Shared helper logic for one feature's commands goes in `Features/<Aggregate>/Commands/<Aggregate>CommandSupport.cs` as an `internal static` class.

## `<Service>.Infrastructure`

```
Persistence/DbContexts/          Persistence/Configurations/
Persistence/Migrations/          Persistence/ReadServices/
Persistence/Specifications/      Persistence/SeedData/ReferenceData/
Persistence/CodeGeneration/
Repositories/    UnitOfWork/     Kafka/    Auditing/
Observability/   Maintenance/    ExternalServices/
Hangfire/        Hangfire/Jobs/  Hangfire/Options/
DependencyInjection.cs
```

## `<Service>.Api`

```
Controllers/  Models/Requests/  Models/Responses/
Extensions/   Middleware/       Filters/   Realtime/
Properties/   Program.cs        Dockerfile
appsettings.json  appsettings.Development.json
```

## Tests

- One test project per production project, named `<Project>.Tests`, placed in `tests/`.
- Cross-cutting dependency rules live in `tests/Sales.Architecture.Tests`.
- Browser/E2E specs live in `tests/Playwright/specs`.

## Related

- [architecture.md](architecture.md)
- [naming.md](naming.md)

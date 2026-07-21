# CQRS + MediatR Rules

## Markers

```csharp
public interface ICommand : IRequest;                  // no response
public interface ICommand<TResponse> : IRequest<TResponse>;
public interface IQuery<TResponse> : IRequest<TResponse>;
```

- Every request implements a marker from `BuildingBlocks.Application`. Never implement `IRequest<T>` directly.
- Handlers implement `IRequestHandler<TRequest, TResponse>` from MediatR.

## Commands

- `sealed record`, one per file, named after the use case.
- Return a DTO or `void`. Never return a domain entity, `IQueryable`, or an EF type.
- A command handler:
  1. loads aggregates through repositories,
  2. checks the expected version when the API exposes an ETag,
  3. calls aggregate behavior,
  4. calls `IUnitOfWork.SaveChangesAsync`,
  5. invalidates cache / notifies realtime **after** the save,
  6. maps to a DTO and returns.
- Never open a transaction manually in a Sales handler — `SaveChangesAsync` is the boundary. Inventory command transactions are owned by `InventoryTransactionBehavior`.
- Never dispatch another command from a command handler. Extract shared logic into an `internal static` support class instead (`OrderCommandSupport`, `CategoryCommandSupport`).

## Queries

- `sealed record` implementing `IQuery<TResponse>`.
- Query handlers **only** call a read-service port. They never touch a repository, `DbContext`, or an aggregate.
- Queries never mutate state, never write to cache from the handler, never publish events.
- A missing resource throws `NotFoundException` in the handler when the endpoint contract is 404; a nullable read (`GetInventoryByProductQuery`) returns `null` and the controller decides.

## Read services

- Live in `<Service>.Infrastructure/Persistence/ReadServices/`.
- Always `AsNoTracking()`.
- Project to DTOs in the query, or map with Mapster; never return tracked entities.
- Paginated reads call `Paging.Normalize(page, pageSize)` and return `PagedResult<T>`.

## Pipeline order

Registered in `AddApplicationBuildingBlocks()` and then extended per service. Outer to inner:

```
LoggingBehavior -> PerformanceBehavior -> ValidationBehavior -> [InventoryTransactionBehavior] -> Handler
```

- Add a new shared behavior in `BuildingBlocks.Application/Behaviors` and register it in `AddApplicationBuildingBlocks` at the correct position.
- Add a service-specific behavior in `<Service>.Application/Common/Behaviors` and register it **after** `AddApplicationBuildingBlocks()` so validation still runs before it.
- `InventoryTransactionBehavior` is constrained to `TRequest : ICommand<TResponse>` so queries never open a transaction. Keep that constraint on any transactional behavior.

## Registration

```csharp
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
services.AddValidatorsFromAssembly(assembly);
services.AddApplicationMapping(assembly);
services.AddApplicationBuildingBlocks();
```

Assembly scanning only. Never register a handler by hand.

## Related

- [validation-rule.md](validation-rule.md)
- [dto-rule.md](dto-rule.md)
- Learning: [../../guides/05-cqrs-and-mediatr.md](../../guides/05-cqrs-and-mediatr.md)

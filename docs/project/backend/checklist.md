# Backend Feature Checklist

Every generated backend feature must satisfy every applicable item. Skip a section only when the feature genuinely does not touch it.

## Architecture

- [ ] Code is in the correct project for its layer (Domain / Application / Infrastructure / Api).
- [ ] No new project reference crosses a bounded context.
- [ ] No EF Core, Kafka, Redis, Hangfire, or ASP.NET Core type appears in Domain or Application.
- [ ] Every new infrastructure capability used by Application is behind a port declared in Application.
- [ ] An existing port/abstraction was reused instead of adding a new one, unless no existing one fits.
- [ ] No wrapper was added that only renames or re-exposes a vendor type without hiding it or adding policy.
- [ ] No dependency is hidden behind a `GlobalUsings.cs` entry, a type alias, or a fully-qualified name.
- [ ] New registrations go through the owning layer's single `Add<Layer>` extension.
- [ ] `Sales.Architecture.Tests` still passes; a new layering rule got a new test.

## DDD

- [ ] Invariants live in the aggregate, not in a handler, validator, or controller.
- [ ] Aggregate root is `sealed`, has a private EF constructor and a `static Create` factory.
- [ ] Child entities are created and mutated only through their root, with `internal` members.
- [ ] Other aggregates are referenced by id, or captured as a snapshot value object.
- [ ] Every mutation calls `Touch()` exactly once and raises its domain event last.
- [ ] Idempotent transitions return early instead of throwing.
- [ ] Soft-deletable aggregates call `EnsureNotDeleted()` in every mutation.

## CQRS

- [ ] Command/query implements `ICommand`, `ICommand<T>`, or `IQuery<T>` — not `IRequest<T>`; the handler implements the matching `ICommandHandler`/`IQueryHandler` — not `IRequestHandler<T, R>` directly.
- [ ] Command and handler are in separate files under `Features/<Aggregate>/`.
- [ ] Query handler calls only a read service; it never touches a repository or `DbContext`.
- [ ] Command handler order is: load → version check → domain call → save → side effects → map.
- [ ] No command handler dispatches another command.
- [ ] Handler returns a DTO, never a domain type.

## Validation

- [ ] A `<Command>Validator` exists for every new command.
- [ ] Reusable rules are extension methods, not copy-paste.
- [ ] Validator max lengths match the EF `HasMaxLength` values.
- [ ] Collections that must be unique have an explicit rule with a business message.
- [ ] Domain invariants are still enforced in the aggregate, not only in the validator.

## Exception handling

- [ ] `DomainException` for invariants, `NotFoundException` for missing resources, `ConflictException` for version mismatch.
- [ ] No new service-specific HTTP exception type; a mapping was registered instead.
- [ ] Any new error code is added to `ErrorCodes`, `ErrorCatalog`, and the `Definitions` array.
- [ ] Every new exception mapping sets an explicit `LogLevel`.
- [ ] No `try`/`catch` in a controller.

## Logging

- [ ] Failures are logged exactly once, at the boundary that owns the path.
- [ ] Message templates use named PascalCase placeholders, no interpolation.
- [ ] Property names reuse the standard set (`TraceId`, `CorrelationId`, `OrderId`, `EventId`, `ElapsedMs`, …).
- [ ] Nothing sensitive is logged above `Debug`.
- [ ] No new Serilog sink added inside a service.

## API

- [ ] Route follows `api/<plural-resource>`; ids are `{id:guid}`-constrained.
- [ ] Correct verb and status code (`201` create, `200` read/update/transition, `204` delete).
- [ ] Response goes through `ToOkResponse` / `ToCreatedResponse` / `ToNoContentResponse`.
- [ ] Mutating a versioned aggregate requires `If-Match` via `Request.RequireVersion()`.
- [ ] Every response carrying a versioned DTO sets `Response.SetEtag(dto)`.
- [ ] Action takes and forwards a `CancellationToken`.
- [ ] Controller has no business logic and no direct data access.

## Swagger

- [ ] XML docs on the controller action: summary, every parameter, every returned status code.
- [ ] XML docs on request/response models.
- [ ] Infrastructure endpoints are hidden with `[ApiExplorerSettings(IgnoreApi = true)]`.
- [ ] Authorized endpoints show the bearer requirement (automatic via `AuthorizeOperationFilter`).

## Security

- [ ] Endpoint is authorized by default; `[AllowAnonymous]` is justified.
- [ ] Role gate is on the action, matching the existing `Admin` / `Sales` / `Warehouse` model.
- [ ] No secret is committed, logged, audited, or returned.
- [ ] No user input is concatenated into SQL.
- [ ] New sensitive fields are added to the audit ignore/mask lists and `HttpLogging:SensitiveJsonFields`.

## Performance

- [ ] Reads use `AsNoTracking()`.
- [ ] No N+1 — related data is loaded in one bulk query.
- [ ] Paged endpoints call `Paging.Normalize` and return `PagedResult<T>`.
- [ ] Every new filter/sort column has an index.
- [ ] Bulk maintenance uses `ExecuteDeleteAsync`/`ExecuteUpdateAsync`.
- [ ] Long work is moved out of the request path.

## Events

- [ ] Domain event is an immutable past-tense record with business data only.
- [ ] Publishing goes aggregate → domain event → `DomainEventMapper` → outbox (never direct).
- [ ] Integration event lives in `BuildingBlocks.Contracts` with primitive fields and XML docs.
- [ ] Envelope carries a meaningful `AggregateId`, `Version`, and `CorrelationId`.
- [ ] A breaking payload change ships as a new `.v2` topic.

## Kafka

- [ ] Topic/group/header names come from the shared constant classes.
- [ ] New topic constant added to `KafkaTopics` so `kafka-init` provisions it.
- [ ] Consumer sets `AutoOffsetReset.Earliest`, group id, buffer size, worker count.
- [ ] Consumer handler does not rethrow; failures are recorded in the inbox.
- [ ] Unknown event types are recorded and return an "ignored" outcome.

## Outbox

- [ ] Nothing is published to Kafka inside a business transaction.
- [ ] Outbox row is written in the same `SaveChangesAsync` as the state change.
- [ ] Row is marked processed only after the broker acknowledges.
- [ ] Failures increment `Attempts`, set `NextAttemptAt` from `RetryBackoff`, and dead-letter at `MaxAttempts`.

## Inbox

- [ ] Every consumed event is deduplicated by `EventId` before any state change.
- [ ] The inbox row and the state change commit together.
- [ ] A duplicate returns the duplicate outcome and mutates nothing.
- [ ] A failed event stores its envelope so `InboxRedriveService` can replay it.

## Redis

- [ ] Only DTOs are cached, through an `ICacheService<T>` port.
- [ ] Cache invalidation happens after `SaveChangesAsync`, on every write path that affects the cached shape.
- [ ] Key uses the `<prefix>:<guid:N>` format.
- [ ] A distributed lock is treated as an optimisation, not a correctness guarantee.

## Database

- [ ] Entity has an `IEntityTypeConfiguration<T>` with table name, key, max lengths, enum conversions.
- [ ] Versioned aggregates map `Version` as the concurrency token.
- [ ] Soft-deletable entities have a query filter and `NOT "IsDelete"`-filtered unique indexes.
- [ ] Computed domain properties are `Ignore`d.
- [ ] Money columns use the `Money` value converter and `numeric(18,0)`.

## Migrations

- [ ] Migration is scaffolded, PascalCase-named, and committed with its designer and snapshot.
- [ ] No existing migration was edited.
- [ ] `Down` works, or its absence is justified in a comment.
- [ ] Provider-specific model constructs are guarded with `Database.IsNpgsql()`.
- [ ] Additive-first for any change that could break a running instance.

## Transactions

- [ ] Sales: one `SaveChangesAsync` per command; no manual transaction unless the inbox is involved.
- [ ] Inventory: the handler neither commits nor rolls back — `InventoryTransactionBehavior` owns it.
- [ ] No transaction spans an HTTP call, a Kafka publish, or a cache write.

## Concurrency

- [ ] Version mismatch throws `ConflictException` carrying the current version.
- [ ] Out-of-order events are rejected with the aggregate's version guard, not a timestamp.
- [ ] Concurrent background instances coordinate with a lease, advisory lock, or Redis lock.
- [ ] Retryable vs. non-retryable conflicts are distinguished in the API response.

## Naming

- [ ] Types, files, tables, topics, metrics, error codes all follow [naming.md](naming.md).
- [ ] Async methods end in `Async`.
- [ ] The feature matches the naming style of the files around it.

## Folder structure

- [ ] New files sit inside the existing `Features/<Aggregate>/` structure; no new top-level folder.
- [ ] Infrastructure code is under the right subfolder (`Persistence`, `Kafka`, `Hangfire`, `Auditing`, …).
- [ ] No empty placeholder folders were created.

## Testing

- [ ] Domain test per new invariant and rejected transition.
- [ ] Handler test for the happy path and each thrown exception.
- [ ] Validator test per distinct message.
- [ ] Mapping test when an `IRegister` changed.
- [ ] Authorization test when a role gate changed.
- [ ] Reliability test when outbox/inbox/retry/concurrency behavior changed.
- [ ] `dotnet test Sales.sln` passes.

## Documentation

- [ ] XML docs on new public shared types, ports, contracts, and controller actions.
- [ ] `docs/tech/` updated when a business rule, endpoint, topic, schema, or convention changed.
- [ ] `docs/project/backend/` updated when a rule changed.
- [ ] `docs/tech/discrepancies.md` updated if the change resolves or introduces a known gap.
- [ ] README/AGENTS updated if the run/verify commands changed.

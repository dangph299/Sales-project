# Testing Rules

## Framework and layout

- xUnit for every .NET test. One test project per production project: `tests/<Project>.Tests`.
- Jasmine/Karma for the Angular client (`ng test`).
- Playwright (TypeScript) for cross-service browser/API flows in `tests/Playwright/specs`.
- Test class is named after the unit under test; method names state the behavior in `snake_case_or_Sentence_case` matching the surrounding file.

## What to test where

| Project | Tests |
|---|---|
| `Sales.Domain.Tests` / `Inventory.Tests` | invariants, state transitions, soft delete, specifications |
| `Sales.Application.Tests` | handler orchestration with fakes, pipeline behaviors, mapping registers |
| `Sales.Infrastructure.Tests` / `Inventory.Infrastructure.Tests` | EF mapping, read services, outbox/inbox state machines, maintenance |
| `Sales.Api.Tests` / `Inventory.Api.Tests` | exception mapping, authorization attributes, Swagger composition, health |
| `BuildingBlocks.*.Tests` | shared registration, error catalog, correlation contract, recurring-job helpers |
| `Sales.Architecture.Tests` | layering and dependency rules |
| `AuditLog.Tests` | audit diffing, document mapping, Mongo idempotency |

## Required tests for a new feature

1. Domain test for every new invariant and every rejected transition.
2. Handler test for the happy path and each thrown exception type.
3. Validator test for each rule that produces a distinct message.
4. Mapping test when adding an `IRegister`.
5. Architecture test when adding a layering rule.
6. Authorization test when adding or changing a role gate.
7. Reliability test when touching outbox, inbox, retry, dead-letter, or concurrency behavior.

## Isolation

- Unit tests use in-memory fakes for ports. No real Postgres, Kafka, Redis, or Mongo.
- EF-level tests use the SQLite fixture (`SqliteSalesFixture`), which builds the schema from the model.
- Tests that need real infrastructure are marked `[Trait("Category", "Reliability")]`, gated on `RUN_RELIABILITY_TESTS=true`, and skip cleanly when it is not set.
- Never write a test that depends on wall-clock timing. Inject a fake `IClock` or the `utcNow` delegate the service already accepts.

## Determinism

- Background services expose an `internal` single-cycle method (`RunPublishCycleAsync`, `RunRedriveCycleAsync`) for tests; use it instead of starting the loop. `InternalsVisibleTo` is already configured.
- Log-outcome strings (`"Reserved"`, `"Duplicate"`, `"ReleasedBeforeReserve"`) are asserted by tests. Renaming one is a breaking change.

## Commands

```bash
dotnet test Sales.sln                                     # everything except gated reliability tests
dotnet test Sales.sln --filter "Category!=Reliability"     # what CI runs on every push
RUN_RELIABILITY_TESTS=true dotnet test Sales.sln           # includes real Postgres/Mongo
cd src/Web/Sales.Web && npm test
cd tests/Playwright && npx playwright test
```

## Related

- Reference: [../../tech/reliability-tests.md](../../tech/reliability-tests.md)
- Learning: [../../guides/17-testing-strategy.md](../../guides/17-testing-strategy.md)

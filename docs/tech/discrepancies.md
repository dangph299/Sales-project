# Known Discrepancies & Gaps

Findings from reading the implementation against the documentation, as of 2026-07-21. Each entry says what the code actually does, why it matters, and whether it needs action. Nothing here is a bug report — it is the list of things a future change is likely to trip over.

## Naming and contract mismatches

### `OrderUndoConfirmationRequested` topic carries an `OrderCancellationRequested` payload

`KafkaTopics.OrderUndoConfirmationRequested = "sales.order-undo-confirmation-requested.v1"`, but `DomainEventMapper.MapOrderUndoConfirmed` publishes an `OrderCancellationRequested` record, and `InventoryIntegrationEventProcessor` dispatches on `nameof(OrderCancellationRequested)`.

Harmless — consumers switch on `EventType`, not on the topic — but the topic name and the payload name tell different stories. Do not "fix" the payload name without shipping a `.v2` topic.

### `OrderUndoComfirmedDomainEvent` is misspelled

`Sales.Domain/Events/Orders/OrderCancelledDomainEvent.cs` declares `OrderUndoComfirmedDomainEvent` ("Comfirmed"), and the file name does not match the type. Renaming is safe — it is an internal domain type with no wire representation — but it touches `Order`, `DomainEventMapper`, and tests.

### `OrderLineIntegration.ProductId` carries a product **variant** id

Documented on the contract itself: the field kept its v1 name after the catalog gained variants. Inventory therefore keys `InventoryItem` by variant id while calling it `ProductId`. Renaming requires a `.v2` topic.

## Endpoint and API gaps

### `If-Match` is only enforced on orders

`ProductDto`, `CustomerDto`, and `CategoryDto` all carry `Version`, and `ProductsController.Get` / `CustomersController.Get` set an `ETag`, but their `PUT`/`DELETE` endpoints do not call `RequireVersion()`. Concurrent edits to a product or customer are last-writer-wins at the API level (the EF concurrency token still catches a genuinely concurrent transaction, surfacing a 409).

### Inventory 404s bypass the shared error envelope

`InventoryController.Get` and `GetReservation` return a bare `NotFound()` rather than the `ApiErrorResponse` shape every other 404 uses. The frontend's `getOptional` tolerates it, but a client parsing the standard envelope will find an empty body.

### No HTTP API versioning

All routes are unversioned `api/<resource>`. Only Kafka contracts are versioned. Introducing HTTP versioning would be a new convention, not an extension of an existing one.

### `AuthController` bypasses CQRS

Deliberate and documented in the class: it uses `UserManager` and `SalesDbContext` directly because authentication is not a Sales business use case. Do not copy the pattern for business endpoints.

## Domain behaviour worth knowing

### `Customer.Create(name, phone)` uses a process-local sequence

The two-argument overload builds `CUS{Interlocked.Increment(ref customerCodeSequence):D6}` from a static field. It is only used by tests; production goes through `ICustomerCodeGenerator` and the Postgres sequence. Two API instances using the overload would collide.

### Creating a product variant does not create an `InventoryItem`

Inventory items appear the first time stock is adjusted for a variant id. Confirming an order for a variant that has never been stocked is rejected as insufficient stock. Expected, but a common surprise when seeding demo data.

### `Product.DeactivateVariant` is an alias for `DiscontinueVariant`

`DeactivateProductVariantCommand` and the `/deactivate` route both discontinue. The two names for one behaviour are worth collapsing eventually.

### Empty domain folders

`Sales.Domain/Exceptions/` and `Sales.Domain/Services/Orders/` exist but contain no files. `DomainException` lives in `BuildingBlocks.Domain`.

## Infrastructure gaps

### Message headers are declared but mostly unused

`MessageHeaders` declares `contract-version`, `correlation-id`, `causation-id`, `event-id`, `event-type`, `occurred-at`, but `KafkaOutboxPublisher` writes only `traceparent` and `tracestate`. Correlation travels inside the envelope instead. A tool that inspects headers to route or filter will find them missing.

### No circuit breaker, no Polly

Resilience is entirely the outbox/inbox state machine plus Kafka client retries. `RetryBackoff` has no jitter, so a mass failure retries in lockstep.

### No Kafka dead-letter topic

"Dead letter" means a row parked in the service's own table. Recovery is a manual `*MaintenanceService` call; there is no endpoint, dashboard, or CLI for it.

### No operator endpoint for outbox replay

Sales and Inventory recurring jobs can reset terminal failed outbox rows for publisher retry, but there is still no HTTP endpoint, dashboard action, or CLI for an operator to trigger a specific outbox replay by id.

### Sales cleanup ignores its cancellation token

`MaintenanceCleanupJob.CleanupAsync()` takes no token and its `ExecuteDeleteAsync` calls pass none, so a shutdown mid-cleanup cannot cancel the deletes.

### No HTTPS enforcement, no rate limiting

Neither API calls `UseHttpsRedirection`, HSTS, or a rate limiter. TLS termination is assumed at the edge. Fine for the local stack; not production-ready.

### No health checks beyond liveness

`/health` returns a static `"healthy"`. It does not probe Postgres, Redis, Kafka, or Mongo, so a container can report healthy while its dependencies are down. `AddHealthChecks()` is not registered.

### No feature-flag mechanism

`ErrorCodes.FeatureDisabled` exists in the catalog but nothing produces it. The nearest runtime toggle is `RecurringJobSettings.Enabled`.

## Not implemented

### Promotions, coupons, and order history

`docs/superpowers/specs/2026-07-16-sales-product-order-coupon-history-design.md` designs them. No promotion, coupon, tax, shipping, or price-history code exists. Pricing is unit price × quantity × per-line discount, VND only.

### AuditLog has no query API

`AuditLog.Worker` writes to MongoDB. Nothing reads it back over HTTP; inspection is via `mongosh` or the `tests/Playwright/AuditProbe` console app.

### AuditLog has no inbox

The audit worker relies on MongoDB's unique-`AuditId` upsert for idempotency instead of an inbox table, and it **does** rethrow on failure (unlike the Sales/Inventory consumers), so a failing audit write is retried by Kafka redelivery within the consumer's session but is not durably queued.

## Documentation status

These docs are the source of truth as of 2026-07-21. `docs/superpowers/` is read-only design history and may describe intentions that were never implemented, or were implemented differently — always trust the code and these documents over a superpowers spec.

Files removed in this refactor because they were superseded: `docs/service-migration-matrix.md`, `docs/shared-building-blocks-refactor-plan.md`, `docs/tech/inventory-cqrs-refactor-audit.md`, `docs/guides/Sumaries-guide.md`, `docs/guides/observability.md`, `docs/guides/project-presentation.md`, `docs/guides/project-style-guide.md`. Their content lives on in `docs/tech/`, `docs/project/`, and `docs/guides/` respectively.

## Related

- [../README.md](../README.md)
- [review-notes.md](review-notes.md)

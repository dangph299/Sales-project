# Exception Hierarchy & Error Catalog

## What exists

A single solution-wide list of public error codes in `BuildingBlocks.Contracts`, and a single HTTP translator in `BuildingBlocks.Web`.

- `ErrorCodes` — 57 `const string` codes, `snake_case`.
- `ErrorCatalog` — one `ErrorDefinition(Code, Description)` per code, plus a lookup dictionary. Unknown codes resolve to `internal_server_error`.
- `IErrorMessageProvider` — lets a service override the *description* of a code without redefining the code.
- `ErrorCatalogResolver` — the registered `IErrorCatalog`; resolves the default and applies the provider's override.

`BuildingBlocks.Contracts.Tests.ErrorCatalogTests` fails if a code is declared without being registered.

## Code families

| Family | Examples |
|---|---|
| Validation | `validation`, `invalid_request`, `invalid_operation`, `invalid_input`, `missing_required_field`, `unsupported_operation` |
| Auth | `unauthorized`, `forbidden`, `authentication_failed`, `token_expired`, `invalid_token`, `permission_denied` |
| Resource | `not_found`, `already_exists`, `conflict`, `duplicate`, `duplicate_request`, `duplicate_message` |
| Concurrency | `concurrency_conflict`, `resource_locked`, `resource_deleted`, `version_mismatch` |
| Business | `business_rule_violation`, `invalid_state`, `state_transition_not_allowed` |
| Inventory | `insufficient_stock`, `reservation_not_found`, `reservation_expired`, `stale_reservation`, `product_out_of_stock` |
| Order/Sales | `order_not_found`, `customer_not_found`, `product_not_found`, `invalid_order_state`, `payment_required` |
| Database | `database_error`, `unique_violation`, `foreign_key_violation`, `serialization_failure`, `transaction_failed` |
| External | `external_service_error`, `external_service_unavailable`, `external_request_failed`, `external_timeout` |
| Messaging | `message_publish_failed`, `message_consume_failed`, `message_processing_failed`, `invalid_message` |
| Cache | `cache_error`, `cache_unavailable` |
| Config | `configuration_error`, `feature_disabled` |
| Availability | `timeout`, `operation_cancelled`, `service_unavailable` |
| Internal | `internal_server_error`, `unexpected_error` |

Many are declared for future use. The codes actually produced today are: `validation`, `invalid_request`, `invalid_operation`, `not_found`, `unauthorized`, `concurrency_conflict`, `unique_violation`, `operation_cancelled`, `internal_server_error`, plus `stale_reservation` and `order_not_found` as internal outcome strings.

## Exception hierarchy

| Exception | Declared in | HTTP | Code | Log level |
|---|---|---|---|---|
| `DomainException` | `BuildingBlocks.Domain` | 400 | `invalid_operation` (message passed through) | Information |
| `NotFoundException` | `Sales.Application` | 404 | `not_found` | Information |
| `ConflictException(currentVersion)` | `Sales.Application` | 409 | `concurrency_conflict` + `errors[current_version]` | Warning |
| `ValidationException` | FluentValidation | 400 | `validation` + `validationErrors[]` | Information |
| `UnauthorizedAccessException` | BCL | 401 | `unauthorized` | Warning |
| `BadHttpRequestException` | ASP.NET Core | its own status (428 for a missing `If-Match`) | `invalid_request` | Information |
| `OperationCanceledException` | BCL | 499 | `operation_cancelled` | Information |
| `KeyNotFoundException` | BCL | 404 | `not_found` | Information |
| anything else | — | 500 | `internal_server_error` | Error |

Persistence exceptions are classified **first**, by `PostgresPersistenceExceptionClassifier`:

| Exception | HTTP | Code | `retryable` |
|---|---|---|---|
| `DbUpdateConcurrencyException` | 409 | `concurrency_conflict` | `False` |
| `DbUpdateException` with Postgres 23505 | 409 | `unique_violation` | `False` |
| Postgres 40001 / 40P01 (serialization failure / deadlock) | 409 | `concurrency_conflict` | `True` |

The classifier lives in `BuildingBlocks.Infrastructure` and reaches the web layer only through the `IPersistenceExceptionClassifier` port — an architecture test forbids `Microsoft.EntityFrameworkCore` and `Npgsql` inside `BuildingBlocks.Web.ExceptionHandling`.

## Mapping order

`ApiExceptionHandler.MapException`:

1. Service-registered mappings (`ApiExceptionHandlingOptions.TryMap`).
2. Persistence classification.
3. `ValidationException` → 400.
4. `UnauthorizedAccessException` → 401.
5. `BadHttpRequestException` → its own status.
6. `OperationCanceledException` → 499.
7. `KeyNotFoundException` → 404.
8. Fallback → 500.

Service mappings are registered in the host:

- Sales (`ConfigureSalesExceptions`): `DomainException`, `NotFoundException`, `ConflictException`.
- Inventory: none — it relies on the shared behaviour plus its own `IErrorMessageProvider` wording.

## Side effects of handling

Besides writing the response, `ApiExceptionHandler`:

- logs the failure exactly once at the mapping's `LogLevel`, with `ErrorCode`, `StatusCode`, method, path, `TraceId`, `CorrelationId`;
- attaches the exception to `Activity.Current` and tags `error.code`;
- sets `ActivityStatusCode.Error` **only** for 5xx, so validation failures do not inflate the service's trace error rate.

## Service-specific wording

| Code | Sales | Inventory |
|---|---|---|
| `not_found` | "The requested sales resource was not found." | "The requested inventory resource was not found." |
| `concurrency_conflict` | "The sales resource was changed by another request." | "Inventory was changed by another operation. Please retry." |
| `stale_reservation` | — | "The reservation event is older than the current inventory state." |

## Related

- [api-conventions.md](api-conventions.md)
- Rules: [../project/backend/exception-rule.md](../project/backend/exception-rule.md)

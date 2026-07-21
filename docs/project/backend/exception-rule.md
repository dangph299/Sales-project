# Exception & Error Rules

## Hierarchy

```
Exception
├─ DomainException                    (BuildingBlocks.Domain)  invariant violated        -> 400 invalid_operation
├─ NotFoundException                  (Sales.Application)      resource missing          -> 404 not_found
├─ ConflictException(currentVersion)  (Sales.Application)      version mismatch          -> 409 concurrency_conflict
├─ FluentValidation.ValidationException                        request invalid           -> 400 validation
├─ UnauthorizedAccessException                                 credential rejected       -> 401 unauthorized
├─ BadHttpRequestException                                     malformed request/header  -> its own status, invalid_request
├─ OperationCanceledException                                  client disconnected       -> 499 operation_cancelled
├─ KeyNotFoundException                                        lookup miss               -> 404 not_found
└─ everything else                                             bug                       -> 500 internal_server_error
```

Persistence failures are classified before the list above by `IPersistenceExceptionClassifier`:

- `DbUpdateConcurrencyException` → `409 concurrency_conflict`, `retryable=False`
- Postgres unique violation → `409 unique_violation`, `retryable=False`
- Postgres serialization failure / deadlock → `409 concurrency_conflict`, `retryable=True`

## Rules

- Throw `DomainException` from aggregates only. Do not subclass it per rule.
- Throw `NotFoundException(nameof(Entity), id)` from handlers when a lookup returns null.
- Throw `ConflictException(order.Version)` when an expected version does not match; the current version is returned to the client as an `errors[]` entry.
- Never create a service-specific HTTP exception type. Register a mapping instead.
- Never catch an exception just to rethrow it unchanged.
- Never catch `Exception` broadly except in a background loop, and there you must log it and continue.

## Error codes

- Every public error code is declared once in `BuildingBlocks.Contracts.ErrorCodes` (snake_case) with a default description in `ErrorCatalog`.
- Adding a code means: add the `const`, add the `ErrorDefinition`, add it to the `Definitions` array. `ErrorCatalogTests` fails otherwise.
- Service-specific wording overrides the description via an `IErrorMessageProvider` subclass (`SalesErrorMessageProvider`, `InventoryErrorMessageProvider`). Never redefine the code.

## Mapping to HTTP

Register service-specific mappings in the host, not in Application:

```csharp
options.Map<ConflictException>((exception, catalog) =>
{
    var error = catalog.Get(ErrorCodes.ConcurrencyConflict);
    var errors = new[] { new ApiError("current_version", exception.CurrentVersion.ToString()) };
    return new ApiExceptionMapping(409, error.Code, error.Description, errors, LogLevel: LogLevel.Warning);
});
```

Every mapping must set an explicit `LogLevel`:

- `Information` — caused by client input (validation, not found, business rule rejection, cancelled).
- `Warning` — conflicts, retryable contention, rejected credentials.
- `Error` — needs an engineer (500).

The default is `Error`, so a mapping that forgets is loud rather than silent.

## Response shape

`ApiExceptionHandler` is the single place the HTTP boundary logs and formats a failure. It returns:

```json
{ "status": 409, "errorCode": "concurrency_conflict", "message": "...",
  "traceId": "...", "correlationId": "...", "errors": [], "validationErrors": [] }
```

It also attaches the exception to the current `Activity` and sets `ActivityStatusCode.Error` for 5xx only, so 4xx does not inflate the trace error rate.

## Related

- [logging-rule.md](logging-rule.md)
- Reference: [../../tech/exception-and-error-catalog.md](../../tech/exception-and-error-catalog.md)

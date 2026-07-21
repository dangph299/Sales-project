# API Design Rules

## Routing

- Route template: `api/<plural-resource>` on the controller, relative segments on actions.
- Sub-resources nest: `api/products/{id:guid}/variants/{variantId:guid}`.
- Non-CRUD transitions are `POST` verbs on a sub-path: `api/orders/{id:guid}/confirm`, `/cancel`, `/undo-confirm`, `api/products/{id}/variants/{variantId}/deactivate`.
- Always constrain route ids: `{id:guid}`.

## Verbs and status codes

| Operation | Verb | Success |
|---|---|---|
| Create | `POST` | `201 Created` via `ToCreatedResponse` |
| Read one | `GET /{id:guid}` | `200 OK` (+ `ETag` for versioned resources) |
| Search | `GET` with query string | `200 OK` with `PagedResult<T>` |
| Full update | `PUT /{id:guid}` | `200 OK` with the updated resource |
| Status change / transition | `POST /{id:guid}/<verb>` | `200 OK` |
| Soft delete | `DELETE /{id:guid}` | `204 No Content` |

## Response envelope

- Success responses always use `ApiResponse<T>` through `this.ToOkResponse(...)` / `this.ToCreatedResponse(uri, ...)` / `this.ToNoContentResponse()`.
- Never return a bare payload, `Ok(dto)`, or `StatusCode(...)` with an ad-hoc body.
- Errors always use `ApiErrorResponse`, produced centrally by `ApiExceptionHandler`. Controllers never build an error body.

## Optimistic concurrency

- Endpoints that mutate an existing versioned aggregate require `If-Match`.
- Read `Request.RequireVersion()` — missing or non-numeric returns `428`.
- Set `Response.SetEtag(dto)` on every response that carries a versioned DTO.
- Currently required on: `PUT /api/orders/{id}/lines`, `POST /api/orders/{id}/confirm|cancel|undo-confirm`.

## Query parameters

- Paging: `page` (default 1), `pageSize` (default 20, clamped to 100 by `Paging.Normalize`).
- Filters are optional and nullable; empty string means "no filter".
- Enum filters bind by name (`status=Confirmed`) so an unknown value produces `400`, not a silent no-op.
- Date ranges: `from` inclusive, `to` exclusive, always UTC `DateTimeOffset`.

## Controllers

- `[ApiController]` + `[Authorize]` (or a role-scoped `[Authorize(Roles = "...")]`) on the class.
- Depend only on `ISender` (plus `UserManager`/`SalesDbContext` in `AuthController`, which is deliberately outside CQRS).
- Explicit constructor with `private readonly` fields when there is more than one dependency.
- Body models live in `Api/Models/Requests`; never bind a domain type. Binding an Application command directly (`CreateOrder`, `CreateProductCommand`) is allowed and used where the command shape is already the wire shape.
- No business logic, no `DbContext` queries, no try/catch.
- Every action takes and forwards a `CancellationToken`.
- Every action carries XML docs describing parameters and returned status codes; Swagger reads them.

## Auth

- Default is authenticated. `[AllowAnonymous]` only on `AuthController` and `HealthController`.
- Role gates in place today: `Admin` for catalog/category writes and customer status changes; `Admin,Sales` for orders and customers; `Admin,Warehouse` for stock adjustment.

## Documentation

- `/health` and other infrastructure endpoints are hidden with `[ApiExplorerSettings(IgnoreApi = true)]`.
- Swagger UI is Development-only, served at `/swagger`. Sales aggregates the Inventory document via `Swagger:InventoryApiUrl`.

## Related

- [controller-rule.md](controller-rule.md)
- [dto-rule.md](dto-rule.md)
- [exception-rule.md](exception-rule.md)
- Reference: [../../tech/api-conventions.md](../../tech/api-conventions.md), [../../tech/api-endpoints.md](../../tech/api-endpoints.md)

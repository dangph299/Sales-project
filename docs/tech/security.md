# Security

## Authentication

JWT bearer, registered once by `BuildingBlocks.Web.Authentication.JwtAuthenticationExtensions.AddJwtAuthentication`.

Validation is fully enabled on both APIs: issuer, audience, lifetime, signing key. Sales sets a 30-second clock skew; Inventory uses the ASP.NET Core default. Both validate against the same `Jwt:Issuer` / `Jwt:Audience` / `Jwt:Key`, so a token issued by Sales is accepted by Inventory.

### Token issuance

Only `Sales.Api/Controllers/AuthController.cs` issues tokens.

| Aspect | Value |
|---|---|
| Algorithm | HMAC-SHA256 (`SymmetricSecurityKey`) |
| Access token lifetime | 30 minutes (`expiresIn: 1800`) |
| Claims | `sub` = user id, `unique_name` = username, one `role` claim per role |
| Refresh token | 48 random bytes, base64 |
| Refresh lifetime | 7 days |
| Refresh storage | SHA-256 hex of the token in `refresh_tokens.TokenHash` (unique index) — the raw token is never persisted |
| Refresh rotation | single-use: the presented token is revoked (`RevokedAt`) and a new pair is issued |

`POST /api/auth/refresh` only accepts a token that is unrevoked and unexpired; anything else is `401`.

### SignalR

The browser cannot set an `Authorization` header on a WebSocket handshake, so `RealtimeServiceCollectionExtensions.ConfigureJwtBearerForSignalR` reads `access_token` from the query string — but **only** for paths starting `/hubs/orders`, and only when no bearer token was already supplied. Do not widen that path check.

## Authorization

Role-based, no policies or claims-based rules today. Roles `Admin`, `Sales`, `Warehouse` are seeded at startup together with a development `admin` user.

| Surface | Requirement |
|---|---|
| `POST /api/auth/*`, `GET /health` | anonymous |
| `GET /api/categories`, `GET /api/common/*`, `GET /api/products*` | any authenticated user |
| `POST|PUT|DELETE /api/categories`, all product/variant writes | `Admin` |
| `/api/customers` (all) | `Admin,Sales` |
| `PUT /api/customers/{id}/status` | `Admin` |
| `/api/orders` (all) | `Admin,Sales` |
| `/hubs/orders` | `Admin,Sales` |
| `GET /api/inventory/*` | any authenticated user |
| `POST /api/inventory/{id}/adjust` | `Admin,Warehouse` |
| `/hangfire` | loopback only (`LocalDashboardAuthorizationFilter`) |
| `/swagger` | Development environment only |

`Sales.Api.Tests.CategoriesControllerAuthorizationTests` asserts the category gates.

## Secrets

The repository contains development-only credentials: `Jwt:Key = "local-development-key-change-before-production"` and the seeded `admin` / `Admin123!` user. **Both must be overridden outside local development** through environment variables, user secrets, or a compose override.

Password policy on `IdentityCore`: minimum length 8, unique email required. Other ASP.NET Identity defaults apply.

## Data protection in logs and audit

| Mechanism | Protects |
|---|---|
| `HttpLogging:SensitiveHeaders` | `Authorization`, `Cookie`, `Set-Cookie` → `***` |
| `HttpLogging:SensitiveJsonFields` | `password`, `token`, `accessToken`, `refreshToken`, `secret`, `currentPassword`, `newPassword` → `***` |
| body capture gated on `Debug` | request/response bodies never reach production Information logs |
| `AuditOptions.IsPropertyIgnored` | any property containing `Password`, `Token`, `Secret`, `ApiKey`, `ConnectionString`, `Payload` |
| `AuditOptions.IsPropertyMasked` | any property containing `Phone` or `Email` → `***` |
| `AuditOptions.IgnoreEntity<T>` | `ApplicationUser`, `RefreshToken`, `OutboxMessage`, `InboxMessage` in Sales |
| `EfCoreAuditEntryFactory` | skips shadow properties, concurrency tokens, primary keys, and any type whose name contains "Audit" |
| binary values | replaced with `[binary]`; strings truncated at 2000 chars |

## Injection and input safety

- All persistence goes through EF Core LINQ. The only raw SQL is parameterized (`SELECT nextval({name}::regclass)`) or a constant advisory-lock id.
- Every command is validated by FluentValidation; invariants are enforced in aggregates.
- Enum query parameters bind by name, so an unknown value is rejected at the boundary.
- `ApiExceptionHandler` returns catalog descriptions only — internal exception messages never reach a client, except `DomainException`'s message, which is written to be client-safe.

## Transport

- CORS: Sales exposes the `SalesWeb` policy for the Angular origin with `AllowCredentials` (required by SignalR) and explicit origins — never `AllowAnyOrigin`. Inventory's `AggregatedSwaggerUi` policy exists in Development only.
- HTTPS is not enforced by the application; there is no `UseHttpsRedirection` or HSTS. TLS termination is expected at the edge — see [discrepancies.md](discrepancies.md).
- There is no rate limiting.

## Related

- [api-endpoints.md](api-endpoints.md)
- [audit-logging.md](audit-logging.md)
- Rules: [../project/backend/security-rule.md](../project/backend/security-rule.md)

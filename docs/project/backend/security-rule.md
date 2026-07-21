# Security Rules

## Authentication

- JWT bearer only, registered once via `AddJwtAuthentication(configuration, clockSkew)` in `BuildingBlocks.Web`.
- Both APIs validate issuer, audience, lifetime, and signing key. Never disable a validation flag.
- Sales sets a 30-second clock skew; Inventory uses the ASP.NET Core default.
- Tokens are issued only by `Sales.Api`'s `AuthController`: 30-minute access token, 7-day refresh token.
- Refresh tokens are stored **hashed** (SHA-256 hex) and single-use — the old one is revoked when exchanged. Never persist a raw refresh token.
- SignalR reads the token from the `access_token` query string for `/hubs/orders` only. Do not widen that path check.

## Authorization

- Controllers are `[Authorize]` by default. `[AllowAnonymous]` is limited to `AuthController` and `HealthController`.
- Roles: `Admin`, `Sales`, `Warehouse`. Seeded at startup with a development `admin` user.
- Current gates:
  - `Admin` — category writes, product writes, variant writes, customer status changes
  - `Admin,Sales` — orders, customers, order hub
  - `Admin,Warehouse` — stock adjustment
  - any authenticated user — reference-data and category reads, product reads, inventory reads
- Put the role check on the action, not in a handler.
- The Hangfire dashboard is restricted to loopback by `LocalDashboardAuthorizationFilter`. Do not expose it publicly.

## Secrets

- Never commit a secret. The committed `Jwt:Key` and `admin`/`Admin123!` credentials are development-only and must be overridden outside local development.
- Supply real values through environment variables, user secrets, or compose overrides.
- Never log or audit a secret — `AuditOptions` ignores properties containing `Password`, `Token`, `Secret`, `ApiKey`, `ConnectionString`, `Payload`, and masks `Phone`/`Email`.
- `RequestObservabilityMiddleware` masks the `Authorization`/`Cookie` headers and the configured sensitive JSON fields; request bodies are only captured at `Debug`.

## Input handling

- Validate every command with FluentValidation; enforce invariants in the aggregate.
- Never interpolate user input into SQL. The only raw SQL is parameterized (`nextval({name}::regclass)`) or a constant advisory-lock id.
- Bind enums by name so unknown values are rejected at the boundary.
- Never expose an EF entity or internal exception message to a client — `ApiExceptionHandler` returns catalog descriptions only.

## Transport and CORS

- Sales exposes a named CORS policy for the Angular client, origins from `SalesWeb:AllowedOrigins` (defaults to `localhost:4200`). `AllowCredentials` is required by SignalR — never combine it with `AllowAnyOrigin`.
- Inventory's CORS policy exists only in Development, for the aggregated Swagger UI.
- Swagger UI is Development-only.

## Related

- [controller-rule.md](controller-rule.md)
- Learning: [../../guides/04-authentication-and-authorization.md](../../guides/04-authentication-and-authorization.md)
- Reference: [../../tech/security.md](../../tech/security.md)

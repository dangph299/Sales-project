# Configuration & Environment

## Sources

Standard ASP.NET Core precedence: `appsettings.json` → `appsettings.{Environment}.json` → environment variables → command line. Environment variables use `__` for nesting (`Outbox__PollIntervalMilliseconds`).

## Sales.Api

| Key | Default | Meaning |
|---|---|---|
| `ConnectionStrings:Sales` | `Host=postgres;Database=sales;Username=postgres;Password=postgres` | business database |
| `ConnectionStrings:Redis` | `redis:6379` | cache + lock |
| `ConnectionStrings:Hangfire` | `Host=postgres;Database=hangfire;…` | job storage |
| `Jwt:Issuer` / `Jwt:Audience` / `Jwt:Key` | `sales-api` / `sales-clients` / development key | token signing and validation |
| `Kafka:Brokers` | `["kafka:9092"]` | falls back to `kafka:9092` when empty |
| `Seq:Url` | `http://seq:5341` | Serilog Seq sink |
| `Outbox:PollIntervalMilliseconds` | `2000` | clamped 100–60000 |
| `InboxConsumer:MaxAttempts` | `5` | before dead-lettering |
| `InboxConsumer:RedrivePollSeconds` | `15` | clamped 1–3600 |
| `InboxConsumer:RedriveBatchSize` | `50` | rows per re-drive cycle |
| `SalesRecurringJobs:MaintenanceCleanup:{Enabled,Queue,Cron}` | `true` / `maintenance` / `0 0 * * *` | inbox/outbox cleanup |
| `SalesRecurringJobs:CancelExpiredPendingOrders:Schedule:{Enabled,Queue,Cron}` | `true` / `critical` / `*/5 * * * *` | expiry scan |
| `SalesRecurringJobs:CancelExpiredPendingOrders:ExpirationMinutes` | `30` | idle window before cancellation |
| `SalesRecurringJobs:CancelExpiredPendingOrders:BatchSize` | `100` | clamped to 1000 by the handler |
| `SalesWeb:AllowedOrigins` | `["http://localhost:4200","http://127.0.0.1:4200"]` | CORS for the Angular client |
| `Swagger:InventoryApiUrl` | Development only | external document listed in the aggregated Swagger UI |
| `HttpLogging:LogRequestBody` | `true` | Debug-level only |
| `HttpLogging:LogResponseBody` | `false` | Debug-level only |
| `HttpLogging:MaxBodyBytes` | `8192` | body capture cap |
| `HttpLogging:SensitiveHeaders` | `Authorization, Cookie, Set-Cookie` | masked to `***` |
| `HttpLogging:SensitiveJsonFields` | `password, token, accessToken, refreshToken, secret, currentPassword, newPassword` | masked to `***` |
| `Serilog:MinimumLevel:*` | Information, Microsoft Warning, EF SQL Warning | log levels |

`SalesRecurringJobsOptions` is validated on start by `SalesRecurringJobsOptionsValidator`: an enabled job must name a queue and a parsable cron expression (5 or 6 fields, Cronos), and `ExpirationMinutes`/`BatchSize` must be positive. A misconfigured job fails startup with a message naming the job.

## Inventory.Api

| Key | Default |
|---|---|
| `ConnectionStrings:Inventory` | `Host=postgres;Database=inventory;Username=postgres;Password=postgres` |
| `Kafka:Brokers` | `["kafka:9092"]` |
| `Jwt:*` | same issuer/audience/key as Sales — both validate tokens Sales issues |
| `Seq:Url`, `HttpLogging:*`, `Serilog:*`, `Outbox:*`, `InboxConsumer:*` | as above |

## AuditLog.Worker

| Key | Default |
|---|---|
| `ConnectionStrings:Mongo` | `mongodb://mongo:27017` |
| `Mongo:Database` | `audit` |
| `Kafka:Brokers` | `["kafka:9092"]` |
| `Kafka:AuditGroupId` | `audit-mongodb-v3` |
| `Seq:Url`, `Serilog:*` | as above |

## Environment variables

| Variable | Set where | Purpose |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT` | compose, launch settings | `Development` enables Swagger and the Inventory CORS policy |
| `ASPNETCORE_URLS` | compose (`http://+:8080`) | listen address |
| `OTEL_SERVICE_NAME` | compose | OpenTelemetry resource name and the Serilog `Service` property |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | compose (`http://otel-collector:4317`) | trace/metric/log export |
| `Outbox__PollIntervalMilliseconds` | compose (`1000`) | faster local feedback |
| `RUN_RELIABILITY_TESTS` | CI / shell | gates `Category=Reliability` tests |
| `SALES_TEST_POSTGRES`, `INVENTORY_TEST_POSTGRES` | CI / shell | reliability-test databases (default `localhost:5432`) |
| `MONGO_TEST_CONNECTION`, `MONGO_TEST_DATABASE` | CI / shell | reliability-test Mongo (default `localhost:27017`) |

## Local ports

| Service | Host port |
|---|---|
| Sales API | 5000 (`/swagger`, `/hangfire`, `/hubs/orders`) |
| Inventory API | 5001 |
| Angular client | 4200 |
| PostgreSQL | 5432 |
| Redis | 6379 |
| MongoDB | 27017 |
| Kafka (external listener) | 9094 |
| Seq | 8081 (ingest 5341) |
| Elasticsearch | 9200 |
| Kibana | 5601 |
| APM Server | 8200 |
| OTel Collector | 4317 / 4318 |

The Angular dev server proxies `/sales-api` → `localhost:5000` and `/inventory-api` → `localhost:5001` (`proxy.conf.json`), including WebSocket upgrade for SignalR.

## Secrets

The committed `Jwt:Key` and the seeded `admin` / `Admin123!` credentials are development-only. Override them with environment variables, user secrets, or a compose override outside local development. No secret is ever committed, logged, or audited.

## Feature flags

There is no feature-flag system. `ErrorCodes.FeatureDisabled` exists in the catalog but nothing produces it. The closest thing to a runtime toggle is `RecurringJobSettings.Enabled`, which removes a job from Hangfire storage when set to false. See [discrepancies.md](discrepancies.md).

## Related

- [dependency-injection-map.md](dependency-injection-map.md)
- [background-jobs.md](background-jobs.md)
- [../guides/18-running-and-deployment.md](../guides/18-running-and-deployment.md)

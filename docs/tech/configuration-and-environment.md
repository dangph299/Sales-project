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
| `SalesRecurringJobs:ReplayDeadLetter:Schedule:{Enabled,Queue,Cron}` | `true` / `maintenance` / `*/15 * * * *` | inbound dead-letter replay reset |
| `SalesRecurringJobs:ReplayDeadLetter:{BatchSize,RetryDelaySeconds}` | `100` / `0` | replay reset batch and next-attempt delay |
| `SalesRecurringJobs:KafkaLagMonitor:Schedule:{Enabled,Queue,Cron}` | `true` / `maintenance` / `*/5 * * * *` | Kafka lag snapshot |
| `SalesRecurringJobs:KafkaLagMonitor:{GroupId,Topics,WarningThreshold,RequestTimeoutSeconds}` | Sales inventory group/topics / `100` / `10` | lag monitor inputs and warning threshold |
| `SalesRecurringJobs:InboxCleanup:Schedule:{Enabled,Queue,Cron}` | `true` / `maintenance` / `0 1 * * *` | processed inbox cleanup |
| `SalesRecurringJobs:InboxCleanup:{BatchSize,RetentionDays}` | `500` / `14` | cleanup batch and retention |
| `SalesRecurringJobs:FailedOutboxRetry:Schedule:{Enabled,Queue,Cron}` | `true` / `maintenance` / `*/15 * * * *` | terminal failed outbox retry reset |
| `SalesRecurringJobs:FailedOutboxRetry:{BatchSize,RetryDelaySeconds}` | `100` / `0` | retry reset batch and next-attempt delay |
| `SalesRecurringJobs:OutboxPendingMonitor:Schedule:{Enabled,Queue,Cron}` | `true` / `maintenance` / `*/5 * * * *` | outbox backlog snapshot |
| `SalesRecurringJobs:OutboxPendingMonitor:{BacklogWarningThreshold,OldestPendingWarningSeconds}` | `100` / `300` | warning thresholds |
| `SalesWeb:AllowedOrigins` | `["http://localhost:4200","http://127.0.0.1:4200"]` | CORS for the Angular client |
| `Swagger:InventoryApiUrl` | Development only | external document listed in the aggregated Swagger UI |
| `HttpLogging:LogRequestBody` | `true` | Debug-level only |
| `HttpLogging:LogResponseBody` | `false` | Debug-level only |
| `HttpLogging:MaxBodyBytes` | `8192` | body capture cap |
| `HttpLogging:SensitiveHeaders` | `Authorization, Cookie, Set-Cookie` | masked to `***` |
| `HttpLogging:SensitiveJsonFields` | `password, token, accessToken, refreshToken, secret, currentPassword, newPassword` | masked to `***` |
| `Serilog:MinimumLevel:*` | Information, Microsoft Warning, EF SQL Warning | log levels |

`SalesRecurringJobsOptions` is validated on start by `SalesRecurringJobsOptionsValidator`: an enabled job must name a queue and a parsable cron expression (5 or 6 fields, Cronos), and each job's own batch/retention/threshold settings must be valid. A misconfigured job fails startup with a message naming the job.

## Inventory.Api

| Key | Default | Meaning |
|---|---|---|
| `ConnectionStrings:Inventory` | `Host=postgres;Database=inventory;Username=postgres;Password=postgres` | business database |
| `ConnectionStrings:InventoryHangfire` | `Host=postgres;Database=inventory_hangfire;…` | Inventory job storage |
| `Inventory:Summary:LowStockThreshold` | `5` | dashboard/read-model low-stock threshold |
| `Kafka:Brokers` | `["kafka:9092"]` | falls back to `kafka:9092` when empty |
| `InventoryRecurringJobs:ReplayDeadLetter:Schedule:{Enabled,Queue,Cron}` | `true` / `maintenance` / `*/15 * * * *` | inbound dead-letter replay reset |
| `InventoryRecurringJobs:ReplayDeadLetter:{BatchSize,RetryDelaySeconds}` | `100` / `0` | replay reset batch and next-attempt delay |
| `InventoryRecurringJobs:KafkaLagMonitor:Schedule:{Enabled,Queue,Cron}` | `true` / `maintenance` / `*/5 * * * *` | Kafka lag snapshot |
| `InventoryRecurringJobs:KafkaLagMonitor:{GroupId,Topics,WarningThreshold,RequestTimeoutSeconds}` | Inventory orders group/topics / `100` / `10` | lag monitor inputs and warning threshold |
| `InventoryRecurringJobs:InboxCleanup:Schedule:{Enabled,Queue,Cron}` | `true` / `maintenance` / `0 1 * * *` | processed inbox cleanup |
| `InventoryRecurringJobs:InboxCleanup:{BatchSize,RetentionDays}` | `500` / `14` | cleanup batch and retention |
| `InventoryRecurringJobs:FailedOutboxRetry:Schedule:{Enabled,Queue,Cron}` | `true` / `maintenance` / `*/15 * * * *` | terminal failed outbox retry reset |
| `InventoryRecurringJobs:FailedOutboxRetry:{BatchSize,RetryDelaySeconds}` | `100` / `0` | retry reset batch and next-attempt delay |
| `InventoryRecurringJobs:OutboxPendingMonitor:Schedule:{Enabled,Queue,Cron}` | `true` / `maintenance` / `*/5 * * * *` | outbox backlog snapshot |
| `InventoryRecurringJobs:OutboxPendingMonitor:{BacklogWarningThreshold,OldestPendingWarningSeconds}` | `100` / `300` | warning thresholds |
| `Jwt:*` | same issuer/audience/key as Sales | both APIs validate tokens Sales issues |
| `Seq:Url`, `HttpLogging:*`, `Serilog:*`, `Outbox:*`, `InboxConsumer:*` | as above | shared host/messaging options |

`InventoryRecurringJobsOptions` is validated on start by `InventoryRecurringJobsOptionsValidator` with the same `RecurringJobSettings` rules as Sales, plus each job's own batch/retention/threshold settings.

## Dashboard.Bff

| Key | Default | Meaning |
|---|---|---|
| `ConnectionStrings:Redis` | `redis:6379` | dashboard snapshot cache |
| `ConnectionStrings:Hangfire` | `Host=postgres;Database=dashboard;…` | refresh job storage |
| `Downstream:SalesBaseUrl` | compose sets `http://sales-api:8080` | Sales API base URL |
| `Downstream:InventoryBaseUrl` | compose sets `http://inventory-api:8080` | Inventory API base URL |
| `ServiceAccount:UserName` / `ServiceAccount:Password` | blank in Development | credentials for background refresh calls |
| `ServiceAccount:AllowAdminDevFallback` | `false` (`true` in Development/compose) | allows the dev-only seeded `admin` fallback when credentials are blank |
| `Dashboard:Cache:Key` | `dashboard:snapshot` | cache key |
| `Dashboard:Cache:TtlSeconds` | `300` | cache TTL |
| `Dashboard:Cache:UseRedis` | `true` | uses in-memory cache if Redis is unavailable/not configured |
| `Dashboard:RefreshJob:{Enabled,Cron,Queue}` | `true` / `* * * * *` / `default` | snapshot refresh schedule |
| `Dashboard:Inventory:LowStockThreshold` | `5` | threshold passed to Inventory summary |
| `Jwt:*` | same issuer/audience/key as Sales | validates caller bearer tokens |

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
| Dashboard BFF | 5002 |
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

The Angular dev server proxies `/sales-api` → `localhost:5000`, `/inventory-api` → `localhost:5001`, and `/dashboard-api` → `localhost:5002` (`proxy.conf.json`). The Sales proxy includes WebSocket upgrade for SignalR.

## Secrets

The committed `Jwt:Key` and the seeded `admin` / `Admin123!` credentials are development-only. Dashboard.Bff may use that admin fallback only when `ServiceAccount:AllowAdminDevFallback=true` in Development; production must provide service-account credentials through environment variables, user secrets, or a compose override. No secret is ever committed, logged, or audited.

## Feature flags

There is no feature-flag system. `ErrorCodes.FeatureDisabled` exists in the catalog but nothing produces it. The closest thing to a runtime toggle is `RecurringJobSettings.Enabled`, which removes a job from Hangfire storage when set to false. See [discrepancies.md](discrepancies.md).

## Related

- [dependency-injection-map.md](dependency-injection-map.md)
- [background-jobs.md](background-jobs.md)
- [../guides/18-running-and-deployment.md](../guides/18-running-and-deployment.md)

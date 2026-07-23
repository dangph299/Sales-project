# Dependency Injection Map

Every registration in the solution, by host.

## Composition entry points

| Extension | Project |
|---|---|
| `AddBuildingBlocksLogging(serviceName)` | `BuildingBlocks.Observability` |
| `AddBuildingBlocksObservability(...)` | `BuildingBlocks.Observability` |
| `AddBuildingBlocksWeb(configuration, options)` | `BuildingBlocks.Web` |
| `AddBuildingBlocksWebObservability(...)` | `BuildingBlocks.Web` |
| `AddApplicationBuildingBlocks()` | `BuildingBlocks.Application` |
| `AddApplicationMapping(assemblies)` | `BuildingBlocks.Application` |
| `AddBuildingBlocksInfrastructure(configuration)` | `BuildingBlocks.Infrastructure` |
| `AddKafkaOutboxPublisher(producerName)` | `BuildingBlocks.Infrastructure` |
| `AddAuditing(configure)` | `BuildingBlocks.Infrastructure` |
| `AddSalesApplication()` / `AddSalesInfrastructure(configuration)` | Sales |
| `AddInventoryApplication()` / `AddInventoryInfrastructure(configuration)` | Inventory |
| `AddAuditLogInfrastructure(configuration)` / `AddAuditLogWorker(configuration)` | AuditLog |
| `AddDashboardBff(builder)` | Dashboard.Bff |

## Shared

### `AddApplicationBuildingBlocks`
| Service | Lifetime |
|---|---|
| `IClock` → `SystemClock` | singleton (`TryAdd`) |
| `IPipelineBehavior<,>` → `LoggingBehavior<,>` | scoped |
| `IPipelineBehavior<,>` → `PerformanceBehavior<,>` | scoped |
| `IPipelineBehavior<,>` → `ValidationBehavior<,>` | scoped |

### `AddApplicationMapping`
`TypeAdapterConfig` (singleton, scanned from the given assemblies) and `IMapper` → `Mapper` (scoped).

### `AddBuildingBlocksInfrastructure`
| Service | Lifetime |
|---|---|
| `InboxConsumerOptions` bound from `InboxConsumer` | options |
| `IMessageLogContext` → `SerilogMessageLogContext` | singleton |
| `IOutboxSignal` → `OutboxSignal` | singleton |
| `IPersistenceExceptionClassifier` → `PostgresPersistenceExceptionClassifier` | singleton |

### `AddAuditing`
`AuditOptions` (validated on start — `TopicName` must be set), `IAuditContextAccessor` → `SystemAuditContextAccessor`, `IAuditAggregateResolver` → `DefaultAuditAggregateResolver`, `IAuditEntryFactory` → `EfCoreAuditEntryFactory`, `AuditSaveChangesInterceptor` — all scoped via `TryAdd`, so a service can override the accessor and resolver.

### `AddBuildingBlocksWeb`
Problem details · `ApiExceptionHandler` (+ optional service mappings) · `IErrorCatalog` → `ErrorCatalogResolver` (singleton) · controllers (+ optional configuration hook) · `AddSharedApiModelResponses` · Swagger with JWT security and XML comments · JWT bearer authentication · authorization · web observability.

## Sales.Api

```
AddBuildingBlocksLogging("sales-api")
AddBuildingBlocksWeb(...)  // ActivitySource Sales.Infrastructure.Kafka, Meter Sales.Infrastructure,
                           // JWT skew 30s, ConfigureSalesExceptions, JsonStringEnumConverter
IErrorMessageProvider -> SalesErrorMessageProvider          singleton
AddSalesApplication()
AddSalesInfrastructure(configuration)
AddSalesBackgroundJobs(configuration)   // Hangfire + PostgreSQL storage, queues critical/default/maintenance
AddSalesIdentity()                      // IdentityCore<ApplicationUser>, roles, EF stores
AddSalesRealtime(configuration)         // SignalR + CORS + JWT-from-query-string
```

### `AddSalesInfrastructure`
| Service | Impl | Lifetime |
|---|---|---|
| `SalesDbContext` | Npgsql + audit interceptor | scoped |
| `IRepository<>` | `Repository<>` | scoped |
| `IProductRepository` / `IOrderRepository` | `ProductRepository` / `OrderRepository` | scoped |
| `IUnitOfWork` | `UnitOfWork` | scoped |
| `ProductReadService` | concrete | scoped |
| `IProductReadService` | `CachedProductReadService` wrapping it | scoped |
| `IReferenceDataReadService` / `ICategoryReadService` / `ICustomerReadService` / `IOrderReadService` | EF read services | scoped |
| `SequentialCodeGenerator` + the three `I*CodeGenerator` | Postgres sequences | scoped |
| `IExecutionContext` | `HttpExecutionContext` (+ `AddHttpContextAccessor`) | scoped |
| `IAuditContextAccessor` / `IAuditAggregateResolver` / `IAuditEnricher` | `SalesAuditContextAccessor` / `SalesAuditAggregateResolver` / `OrderAuditEnricher` | scoped |
| `IProductCache` | `ProductCache` over `IDistributedCache` | scoped |
| `IConnectionMultiplexer` | Redis | singleton |
| `ActivitySource` | `Sales.Infrastructure.Kafka` | singleton |
| `IIntegrationEventProcessor` | `SalesInventoryEventProcessor` | scoped |
| `IInboxFailureRecorder` | `SalesInboxFailureRecorder` | scoped |
| `IOutboxPublisher` | `KafkaOutboxPublisher("sales-outbox")` | singleton |
| KafkaFlow cluster | producer `sales-outbox`; consumer group `sales-inventory-results-v1` | — |
| `SalesOutboxPublisher`, `SalesInboxRedriveService` | hosted services | — |
| `SalesMaintenanceService` | operator recovery | scoped |
| `MaintenanceCleanupJob`, `CancelExpiredPendingOrdersJob` | Hangfire jobs | scoped |
| `SalesRecurringJobsOptions` + validator | bound from `SalesRecurringJobs`, validated on start | options |

Startup tasks: start the Kafka bus, seed identity (which also migrates), register recurring jobs.

## Inventory.Api

```
AddBuildingBlocksLogging("inventory-api")
AddBuildingBlocksWeb(...)   // ActivitySource Inventory.Infrastructure.Kafka, Meter Inventory.Infrastructure
IErrorMessageProvider -> InventoryErrorMessageProvider      singleton
AddSwaggerCors(environment)  // Development only
AddInventoryApplication()    // + InventoryTransactionBehavior AFTER the shared behaviors
AddInventoryInfrastructure(configuration)
```

### `AddInventoryInfrastructure`
`InventoryDbContext` (Npgsql + audit interceptor) · `IClock` → `SystemClock` (singleton) · `IInventoryRepository`, `IReservationRepository` · `IInventoryItemReadService` and `IReservationReadService` both → `InventoryReadService` · `IUnitOfWork` → `InventoryUnitOfWork` · `IInventoryTransactionManager` → `InventoryTransactionManager` · `IInventoryInbox` → `InventoryInbox` · `IInventoryEventOutbox` → `InventoryEventOutbox` · `InventoryMaintenanceService` (scoped) + `InventoryMaintenanceWorker` (hosted) · `IInventoryMetrics` → `InventoryMetricsAdapter` (singleton) · `IAuditAggregateResolver` / `IAuditEnricher` → Inventory versions · `IIntegrationEventProcessor` → `InventoryIntegrationEventProcessor` · `IInboxFailureRecorder` → `InventoryInboxFailureRecorder` · `ActivitySource` (singleton) · `IOutboxPublisher` → `KafkaOutboxPublisher("inventory-outbox")` · `InventoryOutboxPublisher`, `InventoryInboxRedriveService` (hosted) · KafkaFlow cluster with consumer group `inventory-orders-v1`.

Startup tasks: migrate, start the Kafka bus.

## AuditLog.Worker

```
AddBuildingBlocksLogging("audit-worker")
AddBuildingBlocksObservability("audit-worker", tracingSourceNames: ["AuditLog.Infrastructure.Kafka"])
AddAuditLogWorker(configuration)
  -> AddAuditLogInfrastructure(configuration)
       AddBuildingBlocksInfrastructure
       MongoOptions from ConnectionStrings:Mongo + Mongo:Database
       IMongoClient, IMongoDatabase, IAuditWriter -> MongoAuditWriter   (all singleton)
  -> KafkaFlow consumer group audit-mongodb-v3 on sales.audit.v1 + inventory.audit.v1
  -> MongoStartupService, KafkaBusService                               (hosted)
```

## Dashboard.Bff

```
AddBuildingBlocksLogging("dashboard-bff")
AddBuildingBlocksWeb(...)   // JWT, ProblemDetails, Swagger, request logging, observability
IHttpContextAccessor
DownstreamOptions, ServiceAccountOptions, DashboardCacheOptions,
DashboardRefreshJobOptions, DashboardInventoryOptions                  options, ValidateOnStart
IClock -> SystemClock                                                   singleton (TryAdd)
ICallerTokenAccessor -> CallerTokenAccessor                             scoped
IServiceTokenProvider -> ServiceAccountTokenProvider                    singleton
DownstreamAuthDelegatingHandler                                         transient
ISalesClient -> SalesClient                                             typed HttpClient + resilience
IInventoryClient -> InventoryClient                                     typed HttpClient + resilience
IDashboardSnapshotBuilder -> DashboardSnapshotBuilder                   scoped
IDashboardSnapshotCache -> RedisDashboardSnapshotCache                  scoped when Redis configured
IDashboardSnapshotCache -> MemoryDashboardSnapshotCache                 scoped fallback
DashboardSnapshotRefreshJob                                             scoped
Hangfire server + recurring job registration                            when ConnectionStrings:Hangfire exists
```

Dashboard.Bff intentionally references only shared BuildingBlocks projects. It does not register MediatR or service-layer dependencies because its only business behavior is HTTP aggregation for the web dashboard.

## Lifetime rules

- Singleton: clocks, activity sources, outbox signal, Redis multiplexer, error catalog/provider, persistence classifier, Inventory metrics adapter, Mongo client/database/writer, Mapster config.
- Scoped: DbContexts, repositories, read services, units of work, mappers, execution context, audit services, MediatR behaviors, Hangfire job classes.
- Hosted: outbox publishers, inbox re-drive services, maintenance workers, Kafka bus (worker), Mongo startup.

Background services never take a scoped dependency in their constructor; they resolve one per cycle through `IServiceScopeFactory.CreateAsyncScope()`.

## Related

- [configuration-and-environment.md](configuration-and-environment.md)
- Rules: [../project/backend/dependency-rule.md](../project/backend/dependency-rule.md)

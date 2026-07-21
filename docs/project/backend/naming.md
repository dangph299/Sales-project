# Naming Rules

## C# members

| Element | Convention | Example |
|---|---|---|
| Type, public member, constant | PascalCase | `OrderRepository`, `MaxAttempts` |
| Local, parameter | camelCase | `orderUpdatedBefore` |
| Private field | `_camelCase` (explicit fields) or `camelCase` (primary-ctor captures) | `_sender`, `db` |
| Async method | `Async` suffix | `GetWithLinesAsync` |
| Interface | `I` prefix | `IOrderRepository` |
| Generic type parameter | `T` prefix | `TDbContext`, `TResponse` |

## Application layer

| Element | Pattern | Example |
|---|---|---|
| Command | Imperative verb, no suffix in Sales / `Command` suffix when the name is ambiguous | `CreateOrder`, `UpdateProductCommand`, `ReserveStockCommand` |
| Command handler | `<Command>Handler` | `CreateOrderHandler`, `ReserveStockCommandHandler` |
| Query | Verb + noun, `Query` suffix where it clashes with a DTO | `GetOrder`, `SearchProductsQuery` |
| Query handler | `<Query>Handler` | `SearchProductsHandler` |
| Validator | `<Command>Validator` | `CreateOrderValidator` |
| Mapster register | `<Aggregate>MappingRegister` | `ProductMappingRegister` |
| Read port | `I<Aggregate>ReadService` | `IProductReadService` |
| Cache port | `I<Aggregate>Cache` | `IProductCache` |
| DTO | `<Name>Dto` (Sales) / `<Name>Snapshot` (Inventory read models) | `OrderDto`, `InventorySnapshot` |
| Lookup DTO | `<Name>LookupDto` | `CategoryLookupDto` |
| Request input DTO | `<Name>Input` | `OrderLineInput` |

Do not rename an existing family to "fix" the inconsistency. Match the surrounding feature.

## Domain layer

| Element | Pattern | Example |
|---|---|---|
| Aggregate root | Singular noun | `Order`, `Product`, `Reservation` |
| Child entity | Singular noun | `OrderLine`, `ProductVariant` |
| Value object | Singular noun, no suffix | `Money`, `ProductSnapshot` |
| Domain event | `<Aggregate><PastTenseFact>DomainEvent` | `OrderConfirmedDomainEvent` |
| Enum | `E` prefix for catalog/status enums added since the catalog refactor; no prefix for `OrderStatus`/`ReservationStatus` | `EProductStatus`, `OrderStatus` |
| Specification | `<Subject><Rule>Specification` | `OrderCreatedFromSpecification` |

## Infrastructure layer

| Element | Pattern | Example |
|---|---|---|
| EF configuration | `<Entity>Configuration` | `ProductVariantConfiguration` |
| Repository | `<Aggregate>Repository` | `OrderRepository` |
| Read service | `<Aggregate>ReadService` | `CustomerReadService` |
| Hangfire job | `<Action>Job` | `CancelExpiredPendingOrdersJob` |
| Job options | `<Job>JobOptions` | `CancelExpiredPendingOrdersJobOptions` |
| Metrics holder | `<Service>Metrics` | `SalesMetrics` |

## API layer

| Element | Pattern | Example |
|---|---|---|
| Controller | Plural resource + `Controller` | `CustomersController` |
| Request body model | `<Action><Resource>RequestDto` or `<Action>Request` | `CreateCategoryRequestDto`, `LoginRequest` |
| Response body model | `<Name>Response` | `TokenResponse` |

## Persistence

- Tables: `snake_case`, plural — `order_lines`, `outbox_messages`, `product_variants`.
- Columns: PascalCase (EF default; the project does not apply a snake-case naming convention).
- Sequences: `<entity>_code_seq` — `customer_code_seq`.
- Indexes are named by EF unless a filter requires an explicit name.

## Messaging

- Topic: `<bounded-context>.<business-event>.v<version>` — `sales.order-confirmation-requested.v1`.
- Consumer group: `<consumer>-<purpose>-v<n>` — `inventory-orders-v1`.
- KafkaFlow producer name: `<service>-outbox` — `sales-outbox`.
- Kafka header: `kebab-case` — `correlation-id`.

## Observability

- Metric: `<service>.<subject>.<measure>` — `sales.outbox.published`, `inventory.reservation.rejected`.
- Meter name: `<Service>.Infrastructure`.
- ActivitySource: `<Service>.Infrastructure.Kafka`.
- OTEL service name: `kebab-case` — `sales-api`, `audit-worker`.

## Configuration

- Section names are PascalCase (`SalesRecurringJobs`, `HttpLogging`, `InboxConsumer`).
- Environment overrides use `__` (`Outbox__PollIntervalMilliseconds`).

## Error codes

`snake_case`, declared once in `BuildingBlocks.Contracts.ErrorCodes`. See [exception-rule.md](exception-rule.md).

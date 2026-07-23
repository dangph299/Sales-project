# API Endpoint Reference

Every endpoint that exists today. Roles are the `[Authorize]` requirement. "Auth" means any authenticated user.

## Sales API (`http://localhost:5000`)

### `AuthController` — `api/auth` (anonymous)

| Verb | Path | Body | Returns |
|---|---|---|---|
| POST | `/api/auth/login` | `LoginRequest { userName, password }` | `200` `TokenResponse { accessToken, expiresIn: 1800, refreshToken }`, `401` on bad credentials |
| POST | `/api/auth/refresh` | `RefreshRequest { refreshToken }` | `200` new token pair (old refresh token revoked), `401` if missing/expired/revoked |

`Sales.Api/Controllers/AuthController.cs`. Access token lifetime 30 min, refresh 7 days, refresh stored as SHA-256 hex.

### `CustomersController` — `api/customers` (Admin, Sales)

| Verb | Path | Notes |
|---|---|---|
| POST | `/api/customers` | `CreateCustomerRequestDto { name, phone, email?, address? }` → `201` `CustomerDto`. Code allocated from `customer_code_seq`. |
| GET | `/api/customers` | `name`, `phone` (matches the start **or** the end of the number), `page`, `pageSize` → `PagedResult<CustomerDto>` |
| GET | `/api/customers/{id:guid}` | `200` + `ETag` |
| PUT | `/api/customers/{id:guid}` | `UpdateCustomerRequest { name, phone, email?, address? }` |
| PUT | `/api/customers/{id:guid}/status` | **Admin only.** `{ status }` → `Normal` / `Suspended` / `Blocked` |
| DELETE | `/api/customers/{id:guid}` | soft delete → `204` |

### `CategoriesController` — `api/categories`

| Verb | Path | Role | Notes |
|---|---|---|---|
| GET | `/api/categories` | Auth | `CategoryLookupDto[]` ordered by sort order then name, with `productCount` |
| POST | `/api/categories` | Admin | `{ name, description?, parentCategoryId?, sortOrder }` → `201`. Code from `category_code_seq`. |
| PUT | `/api/categories/{id:guid}` | Admin | adds `status` |
| DELETE | `/api/categories/{id:guid}` | Admin | `204`; rejected while child categories or products reference it |

### `ProductsController` — `api/products`

| Verb | Path | Role | Notes |
|---|---|---|---|
| POST | `/api/products` | Admin | `CreateProductCommand { name, description?, categoryId, variants? }` → `201` |
| GET | `/api/products` | Auth | `productCode`, `name`, `categoryId`, `sku`, `colorId`, `sizeId`, `status`, `page`, `pageSize` |
| GET | `/api/products/{id:guid}` | Auth | published products only; `200` + `ETag`. Cached read. |
| PUT | `/api/products/{id:guid}` | Admin | `{ name, description?, categoryId, status }` |
| POST | `/api/products/{id:guid}/variants` | Admin | `{ colorId, sizeId, price, status }` |
| PUT | `/api/products/{id}/variants/{variantId}` | Admin | same body |
| POST | `/api/products/{id}/variants/{variantId}/deactivate` | Admin | discontinues the variant |
| DELETE | `/api/products/{id}/variants/{variantId}` | Admin | soft delete → returns updated `ProductDto` |
| DELETE | `/api/products/{id:guid}` | Admin | soft delete → `204` |

### `OrdersController` — `api/orders` (Admin, Sales)

| Verb | Path | `If-Match` | Notes |
|---|---|---|---|
| POST | `/api/orders` | — | `CreateOrder { customerId, lines[] }` → `201` + `ETag` |
| GET | `/api/orders` | — | `from`, `to`, `customer`, `status`, `page`, `pageSize` |
| GET | `/api/orders/{id:guid}` | — | `200` + `ETag` |
| PUT | `/api/orders/{id:guid}/lines` | **required** | `OrderLineInput[]`; Draft only |
| POST | `/api/orders/{id:guid}/confirm` | **required** | Draft → PendingInventory, publishes to Inventory |
| POST | `/api/orders/{id:guid}/cancel` | **required** | rejected for Confirmed / PendingInventory |
| POST | `/api/orders/{id:guid}/undo-confirm` | **required** | Confirmed → Draft, releases stock |

`from` is inclusive, `to` exclusive, both UTC. `status` binds by enum name.

### `CommonController` — `api/common` (Auth)

| Verb | Path | Returns |
|---|---|---|
| GET | `/api/common/colors` | `ColorLookupDto[]` ordered by code |
| GET | `/api/common/sizes` | `SizeLookupDto[]` ordered by seeded sort order |

Clients bind pickers to these and submit the returned `id`. No seeded GUID is hardcoded on the client.

### Other Sales surfaces

| Path | Notes |
|---|---|
| `GET /health` | anonymous, hidden from Swagger |
| `/hubs/orders` | SignalR, roles `Admin,Sales`; token via `access_token` query string |
| `/hangfire` | dashboard, loopback only |
| `/swagger` | Development only; lists the Inventory document from `Swagger:InventoryApiUrl` |

SignalR hub methods: `SubscribeToOrder(orderId)`, `UnsubscribeFromOrder(orderId)`, `SubscribeToOrderList()`, `UnsubscribeFromOrderList()`. Server event: `OrderStatusChanged` with `{ orderId, previousStatus, currentStatus, changedAt, version }`.

## Inventory API (`http://localhost:5001`)

### `InventoryController` — `api/inventory` (Auth)

| Verb | Path | Role | Returns |
|---|---|---|---|
| GET | `/api/inventory/summary` | Auth | `InventorySummary { totalItems, totalQuantity, inStock, lowStock, outOfStock, lowStockThreshold }`; optional `lowStockThreshold` query |
| GET | `/api/inventory/{productId:guid}` | Auth | `InventorySnapshot { productId, sku, available, reserved, version }`, `404` if none |
| GET | `/api/inventory/reservations/{orderId:guid}` | Auth | `ReservationSnapshot { orderId, status, createdAt, lines[] }`, `404` if none |
| POST | `/api/inventory/{productId:guid}/adjust` | Admin, Warehouse | `{ sku, quantityDelta }` → adjusted snapshot; creates the item if absent |

`GET /health` is anonymous and hidden from Swagger.

Note: these endpoints return the payload wrapped in `ApiResponse<T>`, but `404` is a bare `NotFound()` rather than the shared error envelope — see [discrepancies.md](discrepancies.md).

## Dashboard BFF (`http://localhost:5002`)

### `DashboardController` — `api/dashboard` (Auth)

| Verb | Path | Returns |
|---|---|---|
| GET | `/api/dashboard` | `DashboardSnapshot { metrics, inventory, recentOrders, orderChart, refreshedAt }` |

The BFF is aggregation-only for the Angular dashboard. It serves `dashboard:snapshot` from Redis when available, falls back to in-memory cache when Redis is unavailable/configured off, and rebuilds synchronously only on cache miss. Its recurring Hangfire job refreshes the same snapshot once per minute by default.

## Related

- [api-conventions.md](api-conventions.md)
- [business/order-lifecycle.md](business/order-lifecycle.md)
- [security.md](security.md)

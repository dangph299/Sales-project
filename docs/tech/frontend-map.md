# Frontend Map

Angular 18 standalone client at `src/Web/Sales.Web`. It exists to exercise the APIs manually; it is not a production storefront.

## Stack

| Concern | Choice |
|---|---|
| Framework | Angular 18.2, standalone components, no NgModules |
| UI | ng-zorro-antd 18.2 (`en_US`, explicit icon registration) |
| State | Angular signals only |
| HTTP | `HttpClient` behind a single `ApiClientService` |
| Realtime | `@microsoft/signalr` 10 |
| Styling | component-scoped SCSS |
| Tests | Jasmine + Karma |
| TypeScript | 5.5, `strict` plus four extra safety flags |

## Routes

All lazy-loaded from `app.routes.ts`; `''` and `**` redirect to `dashboard`.

| Path | Feature | Page component |
|---|---|---|
| `/dashboard` | dashboard | `DashboardPageComponent` |
| `/customers` | customers | `CustomerListPageComponent` |
| `/categories` | categories | `CategoryHierarchyPageComponent` |
| `/products` | products | `ProductListPageComponent` |
| `/inventory` | inventory | `InventoryListPageComponent` |
| `/orders` | orders | `OrderListPageComponent` |
| `/common` | common | `CommonPageComponent` |

`app-routes-smoke.spec.ts` asserts every lazy route resolves.

## Core services

| Service | Responsibility |
|---|---|
| `ApiClientService` | the only class touching `HttpClient`; headers, params, response reading, `ApiClientError` |
| `ApiResponseReader` | parses `ApiResponse<T>` / `ApiErrorResponse` into `ApiClientResult<T>` for every status branch |
| `SessionService` | access/refresh token signals, persisted in `localStorage`, `isAuthenticated` computed |
| `AuthApiService` | login/logout; the only writer of session tokens |
| `ApiEndpointConfigurationService` | `salesBase()` / `inventoryBase()`, defaulting to `/sales-api` and `/inventory-api` |
| `SignalrConnectionService` | hub-agnostic connection lifecycle, state signal, event dispatch, resubscribe callbacks |
| `HealthApiService` | status-bar health probe |
| `navigation.config.ts` | sidebar items |

## Shared

- Components: `PageStateComponent` (loading/empty/error), `StatusTagComponent` (renders a `StatusDisplay`).
- Models: `StatusDisplay` + `StatusTone` (`success | warning | danger | info | neutral`).
- Pipes: `money`, `dateTime`, `compactText`, `priceRange`.
- Utilities: `confirmAction`, `describeApiError`, display formatters.

`shared/` is presentation-only — no HTTP, no business vocabulary.

## Features

| Feature | API service | Backend endpoints | Notable pieces |
|---|---|---|---|
| `dashboard` | `DashboardApiService` | aggregates several reads | `dashboard.mapper.ts` |
| `customers` | `CustomerApiService` | `/api/customers*` | `customer-form`, status/detail/toolbar/action-menu components |
| `categories` | `CategoryApiService` | `/api/categories*` | `category-tree.mapper`, `category-parent-options.mapper`, parent selector |
| `products` | `ProductApiService` | `/api/products*` | `product-form`, `product-variant-form` |
| `inventory` | `InventoryApiService` | `/api/inventory*` | `stock-adjustment-form`, `stock-row.model` |
| `orders` | `OrderApiService` | `/api/orders*` | `order-line-editor`, `cart-line.model`, `OrderRealtimeService` |
| `common` | `CommonApiService`, `CustomerLookupApiService`, `ProductLookupApiService` | `/api/common/colors|sizes`, `/api/categories` | `CommonStore`, code constants, `build-sku-preview` |

## Reference data

`CommonStore` loads colors, sizes, and categories once for the application lifetime, sharing a single in-flight promise between concurrent callers and clearing it on failure so `ensureLoaded()` can retry. It triggers automatically once the session is authenticated.

Business decisions match on **code** (`CategoryCodes.Uncategorized`, `SizeCodes.Medium`); the `id` that comes back with it is what write requests submit. **No backend GUID is hardcoded anywhere in the client.**

## Realtime

`SignalrConnectionService` owns connect/reconnect/state and knows nothing about hubs. `OrderRealtimeService` owns the order specifics: hub URL `${salesBase()}/hubs/orders`, token from `SessionService`, group membership (`SubscribeToOrder`, `SubscribeToOrderList`), the `OrderStatusChanged` event, and a resubscribe callback that replays memberships after a reconnect.

Notifications are treated as a hint to re-read, never as authoritative data.

## Error handling

`ApiClientService` throws `ApiClientError` carrying `status` and the parsed `ApiClientResult`, which includes `errorCode`, `message`, `errors[]`, `validationErrors[]`, `traceId`, `correlationId`. Components convert with `describeApiError(error)` into an `errorMessage` signal and render it through `PageStateComponent`. `api-client-result.spec.ts` covers 200/201/204, paged bodies, 400/404/409/500, empty and malformed bodies.

## Concurrency

`getWithEtag` returns `ApiResult<T>` carrying the `ETag`; `putWithEtag` / `postWithEtag` send it as `If-Match`. A `409` is surfaced as "reload and retry" — never auto-retried with a stale ETag.

## Running

```bash
cd src/Web/Sales.Web
npm install
npm start          # http://localhost:4200, proxied to :5000 / :5001
npm test
npm run build
```

`proxy.conf.json` maps `/sales-api` → `localhost:5000` (with `ws: true` for SignalR) and `/inventory-api` → `localhost:5001`, stripping the prefix. Log in with `admin` / `Admin123!`.

## Related

- Rules: [../project/frontend/](../project/frontend/)
- Learning: [../guides/16-frontend-architecture.md](../guides/16-frontend-architecture.md)

# 16. Frontend Architecture

## Purpose

The Angular client at `src/Web/Sales.Web` exists to exercise the APIs by hand. It is small, but it makes several decisions worth understanding — especially about where knowledge of the backend is allowed to live.

## Shape

```
core/      transport, session, config, realtime, navigation   (singletons)
layout/    header, sidebar, status bar, breadcrumbs           (the shell)
shared/    presentation-only components, pipes, utilities
features/  one folder per business area, lazy-loaded
```

Dependencies flow `features → shared → core`, never the reverse. `core/` importing from `features/` would make the transport layer depend on a screen.

Every feature is lazy-loaded:

```typescript
{ path: 'products',
  loadChildren: () => import('./features/products/products.routes').then(r => r.productsRoutes) }
```

Standalone components throughout — there is not a single `NgModule` in the project.

## One place touches HttpClient

```
component  ->  <Feature>ApiService  ->  ApiClientService  ->  HttpClient
```

`ApiClientService` is the only class importing `HttpClient`, `HttpHeaders`, or `HttpParams`. It owns URL building, auth headers, query-parameter construction, response reading, and error translation.

Feature services are thin:

```typescript
@Injectable({ providedIn: 'root' })
export class ProductApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);

  private get baseUrl(): string { return this.endpoints.salesBase(); }

  search(filters: SearchProductsFilters = {}): Promise<PagedResult<ProductResponse>> {
    return this.client.getPage<ProductResponse>(this.baseUrl, '/api/products/', { ...filters });
  }
}
```

The payoff: adding a request header, changing the auth scheme, or handling a new status code is a one-file change.

## Reading the envelope

`ApiResponseReader` is the most carefully built piece of the client, because every backend response is wrapped:

```typescript
static readSuccess<T>(response: HttpResponse<string>): ApiClientResult<T>
static readFailure<T>(error: unknown): ApiClientResult<T>
static ensureSuccess<T>(result: ApiClientResult<T>, requireData: boolean)
```

It handles 204 with no body, an empty body, malformed JSON, a `success: false` envelope on a 200, and the `ApiErrorResponse` shape on 4xx/5xx — normalising all of them into one `ApiClientResult<T>`. Responses are read as `text` and parsed manually so a non-JSON body (an HTML error page from a proxy) produces a sensible message instead of an Angular parse error.

Failures become `ApiClientError`, which carries `status` and the parsed result including `errorCode`, `validationErrors`, `traceId`, and `correlationId`. Components convert with:

```typescript
this.errorMessage.set(describeApiError(error));
```

`api-client-result.spec.ts` covers every branch. That is where to add a case when the backend gains a new error shape.

## State: signals only

No NgRx, no `BehaviorSubject` stores. State lives at the narrowest scope that works:

| Scope | Home |
|---|---|
| one page | the page component |
| one feature, many pages | a root-provided feature service |
| whole app | a `core/` service |

Every data-loading page follows the same shape:

```typescript
readonly rows = signal<ProductResponse[]>([]);
readonly loading = signal(false);
readonly errorMessage = signal('');
```

and renders them through `PageStateComponent`. Consistency here is worth more than cleverness.

## No backend GUID in the client

This is the design decision most worth copying. The backend seeds colours, sizes, and a default category with fixed GUIDs. The client never hardcodes one.

`CommonStore` loads them once per application lifetime:

```typescript
ensureLoaded(): Promise<void> {
  if (this.inFlight) return this.inFlight;
  this.inFlight = this.load().finally(() => { if (this.loadError()) this.inFlight = null; });
  return this.inFlight;
}
```

Concurrent callers share one request; a *failed* load clears the promise so a retry is possible, while a successful one is cached forever.

Business decisions then match on **code**, and submit the **id** that came with it:

```typescript
defaultSizeId(): string {
  return this.sizeByCode(SizeCodes.Medium)?.id ?? this.sizes()[0]?.id ?? '';
}
```

Fall back to the first loaded item so the form still works if the seeded default is renamed. Codes are business identity; GUIDs are persistence detail that only the backend should own.

## Realtime, split in two

```
SignalrConnectionService   connect, reconnect, state, event dispatch, resubscribe hooks
OrderRealtimeService       hub URL, groups, event names, order-specific subscriptions
```

The generic service knows nothing about orders. The feature service registers a resubscribe callback:

```typescript
private async resubscribe(): Promise<void> {
  if (this.orderListSubscribed) await this.connection.invoke('SubscribeToOrderList');
  for (const orderId of this.subscribedOrderIds) await this.connection.invoke('SubscribeToOrder', orderId);
}
```

SignalR group membership is **connection-scoped** — a reconnect silently drops every group, and without this the UI would go quiet after a network blip with no error to show for it.

Notifications are treated as a hint to re-read, never as authoritative data.

## Optimistic concurrency in the UI

```typescript
const result = await this.orderApi.getById(orderId);   // ApiResult<T> with etag
await this.orderApi.confirm(orderId, result.etag);      // sent as If-Match
```

A `409` is surfaced as "reload and retry". Never auto-retry with a stale ETag — that is an infinite loop against a server doing exactly the right thing.

## Status vocabulary

```typescript
export type ProductStatus = 'Draft' | 'Published' | 'Discontinued';

export const productStatusDisplays: Readonly<Record<ProductStatus, StatusDisplay>> = {
  Draft: { label: 'Draft', tone: 'neutral' },
  Published: { label: 'Published', tone: 'success' },
  Discontinued: { label: 'Discontinued', tone: 'warning' }
};
```

A string union, not a TS enum — the comment in the file says why: *values are backend codes and must match exactly*. A TS enum invites `ProductStatus.Draft` with a numeric value that does not match the wire.

Colour is expressed as a **tone**, not an nz colour. `StatusTagComponent` maps tones to the UI kit, so swapping the UI kit is one file.

## Configurable base URLs

```typescript
readonly salesBase = signal(localStorage.getItem('salesBase') || '/sales-api');
readonly inventoryBase = signal(localStorage.getItem('inventoryBase') || '/inventory-api');
```

Relative paths by default, proxied in development by `proxy.conf.json` (with `ws: true` so the SignalR upgrade works). Overridable at runtime, which is exactly what you want in a manual test client.

## Common mistakes

| Mistake | Consequence |
|---|---|
| `HttpClient` in a component | auth, error handling, and URL building get duplicated |
| Hardcoding a seeded GUID | breaks when the environment is reseeded |
| A TS enum for a backend status | the numeric value silently mismatches the wire |
| A presentational component injecting an API service | untestable, unreusable |
| Not unsubscribing a realtime handler | duplicate handlers after navigation |
| No resubscribe after reconnect | the UI goes quiet with no error |
| Auto-retrying a 409 with the same ETag | an infinite loop |
| Status colour chosen in a template | the vocabulary scatters across HTML files |

## Related

- [../tech/frontend-map.md](../tech/frontend-map.md)
- [../project/frontend/](../project/frontend/)
- [../tech/api-endpoints.md](../tech/api-endpoints.md)

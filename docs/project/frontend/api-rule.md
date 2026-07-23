# Frontend API Rules

## Layers

```
component  ->  <Feature>ApiService  ->  ApiClientService  ->  HttpClient
```

- `ApiClientService` is the only class that imports `HttpClient`, `HttpHeaders`, `HttpParams`.
- Feature API services are `@Injectable({ providedIn: 'root' })`, inject `ApiClientService` and `ApiEndpointConfigurationService`, and expose one method per endpoint.
- Components call feature API services only.

## Feature API service

```ts
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

Rules:

- Base URL comes from `endpoints.salesBase()` / `endpoints.inventoryBase()`. Never a literal host or port.
- Paths are literal strings matching the backend route exactly.
- Query parameters are passed as an object; `undefined`, `null`, and `''` are dropped by `ApiClientService`.
- Return the unwrapped payload (`T`, `PagedResult<T>`, `ApiResult<T>`, `void`). Never leak `HttpResponse`.

## Choosing a client method

| Need | Method |
|---|---|
| Read a resource | `get<T>` |
| Read where 404 is expected | `getOptional<T>` (returns `null`) |
| Read a page | `getPage<T>` |
| Read where you need the ETag | `getWithEtag<T>` → `ApiResult<T>` |
| Write | `post<T>` / `put<T>` |
| Write that needs `If-Match` | `postWithEtag<T>` / `putWithEtag<T>` with the ETag |
| Delete | `delete` (returns `void`) |

## Optimistic concurrency

- Reading an order/product/customer you intend to modify uses `getWithEtag`; keep `result.etag`.
- Sending a mutation for a versioned aggregate uses `postWithEtag`/`putWithEtag` with that ETag.
- On `409`, tell the user to reload; do not auto-retry with a stale ETag.

## Contracts

- `api/requests/*.ts` and `api/responses/*.ts` mirror the backend DTOs exactly, in camelCase, one interface per file.
- Never reshape or rename a field in the response interface. Do that in `mappers/`.
- `PagedResult<T>` mirrors `BuildingBlocks.Application.PagedResult<T>`: `items`, `page`, `pageSize`, `total`.
- Never define a second shape for an envelope that `core/api` already models.

## Errors

- `ApiClientService` throws `ApiClientError`, which carries `status` and the parsed `ApiClientResult`.
- Components convert with `describeApiError(error)` for display.
- Read field errors from `error.result.validationErrors` when binding messages to form controls.
- Read `error.result.correlationId` / `traceId` when the user needs a support reference.

## Auth

- The bearer token is attached by `authInterceptor` from `SessionService`. Never set an `Authorization` header anywhere else.
- `ApiClientService` stays token-agnostic; it builds request shape and reads API responses only.
- Only `AuthApiService` writes tokens (`session.setTokens`) and owns refresh-token retry coordination.
- The auth interceptor may retry a `401` once after `AuthApiService.refreshAccessToken()` succeeds. It must skip auth endpoints to avoid refresh loops.

## Related

- [state-management.md](state-management.md)
- Reference: [../../tech/api-endpoints.md](../../tech/api-endpoints.md)

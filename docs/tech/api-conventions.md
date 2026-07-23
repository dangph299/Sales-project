# API Conventions

## What exists

Three HTTP hosts, all controller-based ASP.NET Core, all composed by `BuildingBlocks.Web.WebHostRegistration`.

| API | Port (compose) | Base | Swagger |
|---|---|---|---|
| Sales | 5000 → 8080 | `/api/...` | `/swagger` (Development) |
| Inventory | 5001 → 8080 | `/api/...` | `/swagger`, `/swagger/v1/swagger.json` |
| Dashboard.Bff | 5002 → 8080 | `/api/dashboard` | `/swagger` (Development) |

Sales also hosts a SignalR hub at `/hubs/orders` and the Hangfire dashboard at `/hangfire` (loopback only).
Dashboard.Bff is a dashboard aggregation host, not a general gateway.

## Why it exists

One shared composition means one error shape, one auth scheme, one OpenAPI style, and one request-logging story across services. `AddBuildingBlocksWeb(configuration, options)` supplies problem details, exception handling, the error catalog, controllers, Swagger, JWT, authorization, and web observability. Each host adds only what is genuinely its own.

Implemented in `src/Shared/BuildingBlocks.Web/WebHostRegistration.cs`.

## Middleware order

`UseBuildingBlocksRequestPipeline()` fixes the shared prefix; the rest stays explicit per host so order is visible.

Sales (`Sales.Api/Extensions/ApplicationBuilderExtensions.cs`):

```
UseExceptionHandler
UseSerilogRequestLogging(RequestLoggingDefaults.Configure)
RequestObservabilityMiddleware
UseCors("SalesWeb")
UseAuthentication
UseAuthorization
UseHangfireDashboard("/hangfire", LocalDashboardAuthorizationFilter)
UseApiDocumentation("Sales API", + Inventory document)
MapControllers
MapSalesRealtime  -> /hubs/orders
```

Inventory adds `UseRouting` and `UseSwaggerCors` instead of the Sales-specific steps.

## Success envelope

Every successful response is `ApiResponse<T>` (`BuildingBlocks.Web/Models/Responses/`):

```json
{ "success": true, "message": null, "correlationId": "…", "data": { } }
```

Produced by the `ControllerBase` extensions in `ApiModelExtensions`: `ToOkResponse`, `ToCreatedResponse`, `ToNoContentResponse`. `204 No Content` carries no body.

## Error envelope

Every failure is `ApiErrorResponse`, produced only by `ApiExceptionHandler`:

```json
{ "status": 409, "errorCode": "concurrency_conflict", "message": "…",
  "traceId": "…", "correlationId": "…",
  "errors": [ { "code": "current_version", "message": "3" } ],
  "validationErrors": [ { "field": "Name", "message": "…", "code": "NotEmpty" } ] }
```

Model-binding failures are shaped identically by `AddSharedApiModelResponses` overriding `InvalidModelStateResponseFactory`.

Status mapping is documented in [exception-and-error-catalog.md](exception-and-error-catalog.md).

## Correlation

- `TraceId` — `Activity.Current.TraceId` hex, via `HttpContext.GetTraceId()`.
- `CorrelationId` — the `X-Correlation-Id` request header, falling back to the trace id, via `HttpContext.GetCorrelationId()`.

Both are the single solution-wide definitions (`BuildingBlocks.Web/Extensions/ApiModelExtensions.cs`), returned to clients and pushed onto the log context, so a client-reported id pastes straight into Seq or Kibana. `BuildingBlocks.Web.Tests.TraceCorrelationContractTests` locks this in.

## Optimistic concurrency

- `ETag` response header = the aggregate `Version`, set by `ControllerEtagExtensions.SetEtag`.
- `If-Match` request header is required by mutating order endpoints; parsed by `RequireVersion()`, which throws `BadHttpRequestException(428)` when missing or non-numeric.
- Mismatch → `ConflictException` → `409` with `errors[0].code = "current_version"`.

## Paging

`PagedResult<T>` — `items`, `page`, `pageSize`, `total`. `page` defaults to 1, `pageSize` to 20, clamped to 1–100 by `Paging.Normalize`.

## Versioning

There is no URL or header API version today. All routes are unversioned `api/<resource>`. Contract versioning exists only on Kafka topics. Introducing HTTP versioning would be a new convention — see [discrepancies.md](discrepancies.md).

## Related

- [api-endpoints.md](api-endpoints.md)
- [exception-and-error-catalog.md](exception-and-error-catalog.md)
- [security.md](security.md)
- Rules: [../project/backend/api-guideline.md](../project/backend/api-guideline.md)

# Seq structured logging usage guide trong Sales Management

Tài liệu này giải thích riêng cách project đang dùng Serilog + Seq: cấu hình nằm ở đâu, sink/enricher nào đang bật, log nào có structured property, và những chỗ hiện chưa nhất quán để biết giới hạn thật của observability hiện tại. Phần log cũng được gửi qua OpenTelemetry (OTLP) nằm ở [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 6 — tài liệu đó không lặp lại, chỉ nói phần nhánh OTLP mới thêm. Cùng phong cách với [kafka-usage-guide.md](kafka-usage-guide.md).

## Tóm tắt nhanh

- Cả 3 service (Sales.Api, Inventory.Api, AuditLog.Worker) dùng **chung 1 helper** để cấu hình Serilog: `BuildingBlocks.Observability.SerilogBootstrap.ConfigureSharedSinks(...)` — không còn 3 block cấu hình copy/paste riêng (mục 4).
- Middleware log HTTP request cũng dùng chung 1 class: `BuildingBlocks.Web.RequestObservabilityMiddleware` — không còn `CorrelationLoggingMiddleware`/`HttpLoggingMiddleware` riêng cho từng service (mục 6, 8).
- MediatR pipeline của Sales có **2 behavior** phụ trách log: `LoggingBehavior` (Debug, theo dõi tiến trình) và `ErrorLoggingBehavior` (Warning/Error, nơi duy nhất log lỗi command/query) — cả 2 giờ nằm ở `Shared/BuildingBlocks.Application/Behaviors/` dùng chung skeleton, không còn trong `Sales.Application` — mục 7.
- Log body request/response chỉ được đọc/ghi khi log level `Debug` đang bật, không phải luôn luôn — mục 8.
- Muốn tra 1 workflow end-to-end trong Seq, filter theo `CorrelationId`; muốn nối tiếp sang trace Kibana, dùng `TraceId` (cùng field, đẩy vào cả Seq lẫn OTLP) — mục 6, 9.

## 1. Seq dùng để làm gì trong project?

Seq là nơi tập trung structured log của các service .NET, phục vụ tra cứu theo field (không chỉ full-text) — ví dụ tìm theo `RequestPath`, `StatusCode`, `EventId` được truyền vào message template.

Cả 3 service (Sales.Api, Inventory.Api, AuditLog.Worker) đều ghi log vào Seq — xem mục 5.

## 2. Package Serilog đang dùng

Khai báo trong `Sales.Api.csproj` và `Inventory.Api.csproj`:

```text
Serilog.AspNetCore     10.0.0
Serilog.Sinks.Seq      9.0.0
```

Khai báo trong `AuditLog.Worker.csproj` (Generic Host, không phải web host nên không dùng `Serilog.AspNetCore`):

```text
Serilog.Extensions.Hosting     10.0.0
Serilog.Settings.Configuration 10.0.0
Serilog.Sinks.Console          6.1.1
Serilog.Sinks.Seq              9.0.0
```

Cả 3 project còn có `ProjectReference` tới `Shared/BuildingBlocks.Observability` — project này khai `Serilog` 4.3.0, `Serilog.Settings.Configuration`, `Serilog.Sinks.Console`, `Serilog.Sinks.Seq`, và thêm **`Serilog.Sinks.OpenTelemetry` 4.2.0** (package mới, phục vụ nhánh log OTLP — xem [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 6). `Sales.Api`/`Inventory.Api` còn `ProjectReference` tới `Shared/BuildingBlocks.Web` (chứa middleware HTTP logging dùng chung, mục 8); `AuditLog.Worker` không cần vì không có HTTP.

## 3. Seq broker nằm ở đâu?

`docker/docker-compose.yml`:

```yaml
seq:
  image: datalust/seq:2025.2
  environment:
    ACCEPT_EULA: Y
  ports: ["5341:5341", "8081:80"]
  volumes: ["seq-data:/data"]
```

- Port `5341`: Seq ingestion API — đây là endpoint app dùng để ghi log.
- Port `8081` (map vào `80` trong container): Seq web UI, mở tại `http://localhost:8081`.
- Volume `seq-data` giữ dữ liệu Seq qua các lần restart container.
- `sales-api`/`inventory-api` **không** khai `depends_on: seq` — log ghi vào Seq là best-effort, app vẫn start bình thường kể cả khi Seq chưa sẵn sàng.

## 4. Serilog khởi tạo trong từng service — dùng chung 1 helper

Trước đây mỗi service tự viết 1 block `UseSerilog(...)` gần giống hệt nhau (copy/paste). Giờ cả 3 gọi chung 1 extension method.

`src/Shared/BuildingBlocks.Observability/SerilogBootstrap.cs`:

```csharp
public static LoggerConfiguration ConfigureSharedSinks(this LoggerConfiguration config, IConfiguration configuration, string defaultServiceName)
{
    var serviceName = configuration["OTEL_SERVICE_NAME"] ?? defaultServiceName;
    var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"] ?? "Production";
    var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://otel-collector:4317";

    return config
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", serviceName)
        .Enrich.WithProperty("Environment", environment)
        .WriteTo.Console()
        .WriteTo.Seq(configuration["Seq:Url"] ?? "http://seq:5341")
        .WriteTo.OpenTelemetry(otel => { /* xem open-telemetry-usage-guide.md mục 6 */ });
}
```

### 4.1 Sales.Api / Inventory.Api

`src/Services/Sales/Sales.Api/Program.cs` và `src/Services/Inventory/Inventory.Api/Program.cs` — cả 2 giờ chỉ còn đúng 1 dòng:

```csharp
builder.Host.UseSerilog((context, config) => config.ConfigureSharedSinks(context.Configuration, "sales-api"));
// Inventory.Api: cùng dòng, chỉ khác "inventory-api"
```

Và sau khi build app, cả 2 dùng chung 1 delegate cấu hình level (mục 8):

```csharp
app.UseSerilogRequestLogging(RequestLoggingDefaults.Configure);
```

### 4.2 AuditLog.Worker

`src/Services/AuditLog/AuditLog.Worker/Program.cs`:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog((_, loggerConfig) => loggerConfig.ConfigureSharedSinks(builder.Configuration, "audit-worker"));
builder.Services.AddAuditLogWorker(builder.Configuration);
builder.Services.AddOpenTelemetry()
    .WithTracing(x => x.AddSource("AuditLog.Infrastructure.Kafka").AddOtlpExporter())
    .WithMetrics(x => x.AddRuntimeInstrumentation().AddOtlpExporter());
await builder.Build().RunAsync();
```

`Host.CreateApplicationBuilder(args)` trả về `HostApplicationBuilder`, không có `.Host`/`IHostBuilder` như `WebApplicationBuilder`, nên dùng overload `services.AddSerilog(Action<IServiceProvider, LoggerConfiguration>, ...)` thay vì `UseSerilog` — cùng gọi `ConfigureSharedSinks`, chỉ khác API bề mặt vì khác kiểu builder. Không có HTTP nên không có `UseSerilogRequestLogging`.

### 4.3 `appsettings.json` tương ứng (giống nhau cho cả 3 service)

```json
"Seq": { "Url": "http://seq:5341" },
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": { "Microsoft": "Warning" }
  }
}
```

Sales.Api/Inventory.Api còn thêm override `Microsoft.EntityFrameworkCore.Database.Command: Warning` (mục 11).

## 5. Bảng tổng hợp: service nào log vào đâu

| Service | Console | Seq | OTLP (log) | Serilog:MinimumLevel riêng | Request logging |
|---|---|---|---|---|---|
| Sales.Api | ✅ | ✅ | ✅ | ✅ có override `Microsoft: Warning` + `...Database.Command: Warning` | `UseSerilogRequestLogging(RequestLoggingDefaults.Configure)` |
| Inventory.Api | ✅ | ✅ | ✅ | ✅ có override `Microsoft.EntityFrameworkCore.Database.Command: Warning` | `UseSerilogRequestLogging(RequestLoggingDefaults.Configure)` |
| AuditLog.Worker | ✅ | ✅ | ✅ | ✅ có override `Microsoft: Warning` (không dùng EF Core nên không có override riêng cho `Database.Command`) | N/A (không có HTTP) |

Cột "OTLP (log)" là kênh mới — cả 3 service giờ giống hệt nhau vì dùng chung `ConfigureSharedSinks` (mục 4), khác với trước đây khi mỗi service tự cấu hình và dễ lệch nhau.

## 6. Enricher và correlation — `LogContext` và `IDiagnosticContext`

Cả 3 service bật `Enrich.FromLogContext()` (trong `ConfigureSharedSinks`, mục 4). 2 nguồn push property vào `LogContext`:

**HTTP request (Sales.Api/Inventory.Api)** — `BuildingBlocks.Web.RequestObservabilityMiddleware` (dùng chung cho cả 2 service, không còn middleware riêng per-service), đăng ký ngay sau `UseSerilogRequestLogging(...)`:

```csharp
app.UseSerilogRequestLogging(RequestLoggingDefaults.Configure);
app.UseMiddleware<RequestObservabilityMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
```

Bên trong middleware:

```csharp
using (LogContext.PushProperty("RequestId", requestId))
using (LogContext.PushProperty("CorrelationId", correlationId))
{
    await next(context);
}
```

`RequestId` là `HttpContext.TraceIdentifier`. `CorrelationId` ưu tiên header `X-Correlation-Id` do client gửi lên, nếu không có thì dùng `Activity.Current?.TraceId` (tức là **khớp `TraceId` của OpenTelemetry**, không phải sinh ngẫu nhiên riêng) — đây là điểm nối giữa log Seq và trace Kibana ngay từ tầng HTTP, không chỉ ở tầng Kafka consumer (mục dưới).

Middleware này đăng ký **trước** `UseAuthentication()`/`UseAuthorization()`, nhưng field cần dữ liệu sau khi authenticate (`UserId`) vẫn đúng vì được set trong khối `finally` — khối này chạy **sau** `await next(context)`, tức là sau khi toàn bộ pipeline phía dưới (kể cả auth) đã chạy xong. Toàn bộ field HTTP (`UserId`, `Url`, `Route`, `ClientIp`, `UserAgent`, `TraceId`...) được ghi vào `IDiagnosticContext` chứ không phải `LogContext` — xem lý do ở mục 8.

**Kafka consumer (Sales/Inventory/AuditLog)** — mỗi handler bọc toàn bộ `Handle(...)`:

```csharp
using (LogContext.PushProperty("EventId", envelope.EventId))
using (LogContext.PushProperty("EventType", envelope.EventType))
using (LogContext.PushProperty("CorrelationId", envelope.CorrelationId))
using (LogContext.PushProperty("TraceId", activity?.TraceId.ToHexString()))
{
    ...
}
```

`TraceId` ở đây lấy từ `Activity` do `ActivitySource` tự tạo khi consume message (xem [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 5) — cùng cơ chế filter Seq theo `TraceId` rồi tra tiếp Kibana APM theo đúng `TraceId` đó cho 1 message Kafka cụ thể. Lưu ý: `CorrelationId` ở Kafka handler là `envelope.CorrelationId` (correlation **nghiệp vụ**, xuyên nhiều message trong 1 workflow), khác `CorrelationId` ở HTTP middleware phía trên (theo `TraceId` của 1 request) — 2 chỗ dùng cùng tên property nhưng ngữ nghĩa khác nhau tùy ngữ cảnh HTTP hay Kafka.

`SalesIntegrationEventHandler`, `InventoryEventHandler`, `AuditEventHandler` đều log 3 mốc qua `ILogger<T>` — start (`LogInformation "Consumed..."` sau khi xử lý xong) và fail (`LogError "Consume failed..."`), không log `envelope.Data` (payload).

## 7. MediatR pipeline behavior (chỉ Sales) — 2 behavior, 2 trách nhiệm khác nhau

**Cập nhật 2026-07-10**: cả 3 behavior chuyển từ `Sales.Application/Services/Behaviors/` sang `Shared/BuildingBlocks.Application/Behaviors/` (namespace `BuildingBlocks.Application`), dùng chung skeleton cho mọi CQRS service tương lai — hiện chỉ Sales dùng vì Inventory không có MediatR pipeline. `Sales.Application/DependencyInjection.cs` (`AddSalesApplication()`) đăng ký `SalesApplicationExceptionClassifier` của riêng Sales rồi gọi `BuildingBlocks.Application.DependencyInjection.AddApplicationBuildingBlocks()`, extension này đăng ký 3 pipeline behavior theo đúng thứ tự cũ:

```csharp
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ErrorLoggingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

Thứ tự đăng ký = thứ tự bọc từ ngoài vào trong: `ErrorLoggingBehavior` bọc ngoài cùng (thấy được exception từ cả `LoggingBehavior` lẫn `ValidationBehavior`), rồi mới tới `LoggingBehavior`, rồi `ValidationBehavior`.

### 7.1 `LoggingBehavior` — theo dõi tiến trình, mức Debug

`src/Shared/BuildingBlocks.Application/Behaviors/LoggingBehavior.cs`:

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogDebug("Pipeline started {RequestName}", requestName);
        try
        {
            var response = await next(cancellationToken);
            logger.LogDebug("Pipeline completed {RequestName}", requestName);
            return response;
        }
        catch (Exception ex)
        {
            // Debug only: the owning handler is responsible for the single Error log per failure.
            logger.LogDebug(ex, "Pipeline failed {RequestName}", requestName);
            throw;
        }
    }
}
```

Log ở mức **Debug** (không phải Information như trước) — mặc định `Serilog:MinimumLevel:Default` là `Information` nên dòng "Pipeline started/completed/failed" **không** xuất hiện trừ khi bật Debug (giống cách bật lại SQL log EF Core ở mục 11). Mục đích: theo dõi tiến trình khi cần debug sâu, không làm ồn log Information hằng ngày.

### 7.2 `ErrorLoggingBehavior` — nơi duy nhất log lỗi command/query

`src/Shared/BuildingBlocks.Application/Behaviors/ErrorLoggingBehavior.cs`:

```csharp
public sealed class ErrorLoggingBehavior<TRequest, TResponse>(
    ILogger<ErrorLoggingBehavior<TRequest, TResponse>> logger,
    IApplicationExceptionClassifier exceptionClassifier) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex) when (exceptionClassifier.IsExpected(ex))
        {
            logger.LogWarning(ex, "{RequestName} rejected {ElapsedMs} {@Request}", typeof(TRequest).Name, sw.ElapsedMilliseconds, request);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{RequestName} failed {ElapsedMs} {@Request}", typeof(TRequest).Name, sw.ElapsedMilliseconds, request);
            throw;
        }
    }
}
```

**Cập nhật 2026-07-10**: việc phân loại "exception nào là expected rejection" đã tách khỏi `ErrorLoggingBehavior` vào `IApplicationExceptionClassifier` (`src/Shared/BuildingBlocks.Application/Exceptions/IApplicationExceptionClassifier.cs`), injected qua DI thay vì hard-code trong 1 static method riêng của Sales. Lý do: `ErrorLoggingBehavior` giờ dùng chung được cho service khác mà không cần biết trước `NotFoundException`/`ConflictException` là type của Sales.

`DefaultApplicationExceptionClassifier` (`src/Shared/BuildingBlocks.Application/Exceptions/DefaultApplicationExceptionClassifier.cs`) coi **Warning** cho `ValidationException`, `DomainException`, và `DbUpdateConcurrencyException` (nhận diện qua tên type vì `BuildingBlocks.Application` không được reference EF Core) là expected; `SalesApplicationExceptionClassifier` (`src/Services/Sales/Sales.Application/Services/SalesApplicationExceptionClassifier.cs`) mở rộng thêm `NotFoundException`/`ConflictException` — đăng ký qua `services.AddSingleton<IApplicationExceptionClassifier, SalesApplicationExceptionClassifier>()` trong `Sales.Application/DependencyInjection.cs`, **trước** khi gọi `AddApplicationBuildingBlocks()` (extension đó dùng `TryAddSingleton` nên không ghi đè classifier Sales đã đăng ký). Mọi exception khác vẫn là **Error**. `{@Request}` log nguyên object request (structured, destructure toàn bộ property) — hữu ích để biết chính xác input nào gây lỗi khi tra Seq.

**Vì sao tách riêng 2 behavior thay vì gộp vào 1**: `LoggingBehavior` trả lời "tiến trình đang chạy tới đâu" (cần khi trace 1 request đang treo/chậm), `ErrorLoggingBehavior` trả lời "cái gì vừa thất bại và tại sao" (cần khi có lỗi thật). Gộp chung sẽ ép cùng 1 log level cho 2 mục đích khác nhau — tách ra cho phép bật Debug để xem tiến trình mà không kéo theo log ồn ào ở mức Warning/Error.

Inventory **không có** behavior tương đương — Inventory dùng Minimal API gọi thẳng `IInventoryService`, không qua MediatR pipeline (xem `ARCHITECTURE_CHECKLIST.md`), nên không có log tự động tương tự cho mỗi thao tác.

## 8. HTTP request logging — dùng chung `RequestObservabilityMiddleware`

Trước đây mỗi Api project có 2 middleware riêng (`CorrelationLoggingMiddleware`, `HttpLoggingMiddleware`). Giờ gộp thành 1 class dùng chung nằm ở `Shared/BuildingBlocks.Web`, cả Sales.Api và Inventory.Api cùng dùng.

### 8.1 `UseSerilogRequestLogging(RequestLoggingDefaults.Configure)` — middleware chịu trách nhiệm chính

Đây là **middleware duy nhất** phát ra 1 dòng log tổng kết cho mỗi request HTTP:

```text
HTTP GET /api/products responded 200 in 12.3456 ms
```

`RequestLoggingDefaults.Configure` (`Shared/BuildingBlocks.Web/RequestLoggingDefaults.cs`) tùy chỉnh level so với mặc định của `Serilog.AspNetCore`:

```csharp
public static void Configure(RequestLoggingOptions options)
{
    options.GetLevel = (context, _, exception) =>
    {
        if (exception is not null || context.Response.StatusCode > 499) return LogEventLevel.Error;
        return IsQuietPath(context.Request.Path) ? LogEventLevel.Debug : LogEventLevel.Information;
    };
}
```

- Exception hoặc status ≥ 500: `Error`.
- Request tới `/health`, `/hangfire` (`QuietPathPrefixes`): hạ xuống `Debug` — health check/dashboard polling định kỳ không phải incident, không nên làm ồn log Information mà on-call phải nhìn.
- Còn lại: `Information`.

Cơ chế bắt exception vẫn như cũ: `RequestLoggingMiddleware` (bên trong `Serilog.AspNetCore`) bọc `await next(context)`, log 1 lần kèm exception rồi rethrow — exception tiếp tục bay lên `UseExceptionHandler()` để build response, không bị nuốt mất. `ExceptionHandlingMiddleware` (`Sales.Api/Middleware/ExceptionHandlingMiddleware.cs`) chỉ build `ProblemDetails`, không tự gọi `ILogger` — tránh log exception 2 lần.

`Inventory.Api` vẫn **không có** `IExceptionHandler`/`UseExceptionHandler()` — exception vẫn được `RequestLoggingMiddleware` log đầy đủ, nhưng response trả client là 500 trần trụi thay vì `ProblemDetails` có cấu trúc. Gap có sẵn từ trước, không thuộc phạm vi logging.

### 8.2 `RequestObservabilityMiddleware` — enrich field, đăng ký sau bước 8.1

`Shared/BuildingBlocks.Web/RequestObservabilityMiddleware.cs`, đăng ký ngay sau `UseSerilogRequestLogging(...)`:

```csharp
app.UseSerilogRequestLogging(RequestLoggingDefaults.Configure);
app.UseMiddleware<RequestObservabilityMiddleware>();
app.UseAuthentication();
```

Middleware này **không tự ghi log dòng nào** — nó inject `Serilog.IDiagnosticContext` và gọi `.Set(...)` để đính field vào **đúng dòng log** mà `RequestLoggingMiddleware` (mục 8.1) phát ra khi request kết thúc, dù thành công hay lỗi (set trong khối `finally`).

Field được thêm (tất cả optional, `null` nếu không áp dụng):

| Field | Nguồn |
|---|---|
| `RequestId` | `HttpContext.TraceIdentifier` |
| `CorrelationId` | Header `X-Correlation-Id` nếu có, ngược lại `Activity.Current?.TraceId` (mục 6) |
| `TraceId` | `Activity.Current?.TraceId.ToHexString()` |
| `UserId` | `ClaimTypes.NameIdentifier` nếu đã authenticate, ngược lại `null` |
| `Url` | `HttpRequest.GetDisplayUrl()` |
| `Route` | `(context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText` |
| `ClientIp` | `HttpContext.Connection.RemoteIpAddress` |
| `UserAgent` | header `User-Agent` |

**Body request/response chỉ được đọc/log khi `logger.IsEnabled(LogLevel.Debug)` là `true`** — khác trước đây (luôn bật theo config bool, không phụ thuộc log level). Khi Debug bật:

```csharp
logger.LogDebug(
    "HTTP body {Method} {Path} Headers={RequestHeaders} Request={RequestBody} Response={ResponseBody}",
    ...);
```

**Masking dữ liệu nhạy cảm** (yêu cầu bắt buộc — không log JWT/password/refresh token/secret/cookie), đọc từ `appsettings.json`, fallback về default hard-code nếu thiếu config:

```json
"HttpLogging": {
  "LogRequestBody": true,
  "LogResponseBody": false,
  "MaxBodyBytes": 8192,
  "SensitiveHeaders": ["Authorization", "Cookie", "Set-Cookie"],
  "SensitiveJsonFields": ["password", "token", "accessToken", "refreshToken", "secret", "currentPassword", "newPassword"]
}
```

- Header khớp `SensitiveHeaders` (không phân biệt hoa/thường) bị thay giá trị bằng `"***"`, giữ tên header để biết request CÓ gửi hay không.
- Body JSON duyệt đệ quy toàn bộ property (kể cả lồng nhau/mảng), field khớp `SensitiveJsonFields` bị thay `"***"`.
- Body vượt `MaxBodyBytes` (mặc định 8192) bị cắt, thêm hậu tố `...[truncated]`.
- Body không parse được JSON thì log nguyên văn (đã giới hạn kích thước).

Đổi `LogRequestBody`/`LogResponseBody`/danh sách field nhạy cảm rồi restart service là có hiệu lực — không cần build lại.

## 9. Cách xem log trong Seq local

```bash
sudo docker compose -f docker/docker-compose.yml up -d --build
```

Mở `http://localhost:8081`.

Ví dụ query hữu ích (Seq dùng cú pháp lọc theo property):

```text
@Exception is not null
RequestPath like '%/orders%'
StatusCode >= 500
RequestName = 'ConfirmOrder'
EventId = 'f3b1...'
EventType = 'StockRejected'
CorrelationId = '2f7e...'
TraceId = '4bf92f3577b34da6a3ce929d0e0e4736'
Service = 'audit-worker'
Environment = 'Development'
```

Để tra 1 event Kafka cụ thể end-to-end: lấy `CorrelationId` (nghiệp vụ) từ log Sales lúc publish, filter `CorrelationId = '...'` trong Seq — sẽ thấy log của cả Sales (publish), Inventory (consume/reserve), và AuditLog (store) cho cùng workflow. Để nối tiếp sang trace kỹ thuật trong Kibana (xem span chi tiết, latency từng bước), lấy `TraceId` của 1 message cụ thể rồi tra tiếp bên Kibana APM.

## 10. Đã cải thiện / còn lại

Đã fix trong code (không còn là khuyến nghị):

- ✅ Serilog bootstrap dùng chung `BuildingBlocks.Observability.SerilogBootstrap.ConfigureSharedSinks(...)` cho cả 3 service — không còn 3 block copy/paste (mục 4).
- ✅ HTTP request logging dùng chung `BuildingBlocks.Web.RequestObservabilityMiddleware` + `RequestLoggingDefaults` — không còn `CorrelationLoggingMiddleware`/`HttpLoggingMiddleware` riêng per-service (mục 8).
- ✅ Log giờ có thêm nhánh OTLP (`WriteTo.OpenTelemetry(...)`), nối được `TraceId` giữa Seq và Kibana APM (mục 6, xem [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 6).
- ✅ Enricher `Environment` (`Enrich.WithProperty("Environment", ...)`) đã có, đọc từ `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT`.
- ✅ `ErrorLoggingBehavior` tách riêng khỏi `LoggingBehavior` — 1 nơi duy nhất log lỗi command/query, phân biệt Warning (nghiệp vụ) và Error (bất ngờ) (mục 7).
- ✅ `AuditLog.Worker` log vào Seq.
- ✅ `Serilog:MinimumLevel` cho `Inventory.Api/appsettings.json` khớp Sales.Api.
- ✅ SQL log của EF Core bật/tắt được thuần qua `appsettings.json` (mục 11).

Còn lại, chưa fix (mức độ ưu tiên thấp hơn):

- Chưa có enricher `WithMachineName()` — nếu sau này chạy nhiều instance cùng service, log không phân biệt được instance nào phát ra (khác với `Environment`, machine name vẫn chưa có).
- `Route`/`Url`/`ClientIp`... hiện chỉ enrich cho HTTP, Kafka consumer log không có field tương đương (chỉ có `EventId`/`EventType`/`CorrelationId`/`TraceId`) — chấp nhận được vì Kafka không có khái niệm route/URL.

## 11. Bật/tắt SQL log của EF Core qua `appsettings.json`

Mặc định, SQL text (`SELECT`/`INSERT`/`UPDATE`/`DELETE`) EF Core sinh ra được log qua category chuẩn của Microsoft:

```text
Microsoft.EntityFrameworkCore.Database.Command
```

Category này log ở level `Information` khi EF Core thực thi 1 lệnh SQL — nếu không override, nó bị cuốn theo `MinimumLevel.Default` (`Information`) và SQL sẽ xuất hiện tràn lan trong Console/Seq.

Cả `Sales.Api/appsettings.json` và `Inventory.Api/appsettings.json` có 2 section sau (đồng bộ giá trị với nhau — dự án dùng Serilog nên `Serilog:MinimumLevel:Override` mới là cái thật sự quyết định hành vi filter; `Logging:LogLevel` giữ đúng convention chuẩn ASP.NET Core để dễ đọc/dễ tool khác nhận diện):

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
  }
},
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  }
}
```

Muốn xem lại SQL (trace debug local), chỉ cần đổi `Warning` thành `Information` ở **cả 2 chỗ** (`Logging:LogLevel` và `Serilog:MinimumLevel:Override`) cho key `Microsoft.EntityFrameworkCore.Database.Command`, không cần build lại — restart service để `IConfiguration` đọc lại file là đủ.

**Vì sao đủ, không cần sửa code**: dự án không có `DbContextOptionsBuilder.LogTo(...)`, không `Console.WriteLine` thủ công, không `EnableSensitiveDataLogging()` — EF Core ghi log hoàn toàn qua `ILoggerFactory` chuẩn mà Serilog đã thay thế, nên override category là đủ.

**Lưu ý phân biệt với OpenTelemetry**: `AddEntityFrameworkCoreInstrumentation()` (xem [open-telemetry-usage-guide.md](open-telemetry-usage-guide.md) mục 3) là pipeline TRACE riêng, tạo span cho mỗi câu lệnh EF Core hiển thị trong Kibana APM — hoàn toàn tách biệt với log SQL text trong Seq. Đổi `Microsoft.EntityFrameworkCore.Database.Command` không ảnh hưởng tới span trong Kibana, và ngược lại.

`AuditLog.Worker` không có override riêng cho category này vì nó không dùng EF Core (chỉ dùng MongoDB Driver qua `IAuditWriter`/`MongoAuditWriter`) — category này sẽ không bao giờ xuất hiện ở service này nên không cần thêm.

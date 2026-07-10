# OpenTelemetry SDK usage guide trong Sales Management

Tài liệu này giải thích riêng phần **code trong solution** dùng OpenTelemetry SDK: khởi tạo tracing/metrics ở đâu, custom metric/ActivitySource nào đang có, log giờ có phải một phần của pipeline OTel không, và cách thêm instrumentation mới. Phần hạ tầng phía sau (OTel Collector → Elastic APM → Kibana) nằm ở [Elastic-usage-guide.md](Elastic-usage-guide.md) — 2 tài liệu này bổ sung cho nhau, không lặp lại nội dung. Cùng phong cách với [kafka-usage-guide.md](kafka-usage-guide.md).

## Tóm tắt nhanh

- 3 service (`Sales.Api`, `Inventory.Api`, `AuditLog.Worker`) đều bật `AddOpenTelemetry()` cho **traces** và **metrics**, export qua OTLP gRPC tới `otel-collector:4317` (mục 2–3).
- **Log cũng đi qua OTLP rồi**, không chỉ Console/Seq — Serilog dùng chung 1 helper (`BuildingBlocks.Observability.SerilogBootstrap.ConfigureSharedSinks`) có thêm `WriteTo.OpenTelemetry(...)` (mục 6). Đây là điểm nhiều người dễ tưởng nhầm là "log tách biệt hoàn toàn với OTel" — không còn đúng.
- Kafka publish/consume có tracing thủ công qua `ActivitySource` riêng của từng service, propagate W3C `traceparent`/`tracestate` qua Kafka header thật (mục 5) — nối được span HTTP request ban đầu với span Kafka consumer ở service khác.
- Custom metric nghiệp vụ (`sales.outbox.*`, `inventory.reservation.*`...) nằm ở `SalesMetrics`/`InventoryMetrics` (mục 4).
- Muốn xem danh sách panel Kibana cần dựng, xem [observability.md](observability.md). Muốn hiểu APM Server/Elasticsearch/Kibana pipeline, xem [Elastic-usage-guide.md](Elastic-usage-guide.md).

## 1. OpenTelemetry dùng để làm gì trong project?

OpenTelemetry (OTel) là SDK .NET đứng giữa code và mọi hệ observability phía sau — code chỉ gọi API của OTel (`Activity`, `Meter`), không tự viết logic gửi dữ liệu đi đâu. Trong project, OTel phục vụ 3 tín hiệu (signal):

| Signal | Dùng để làm gì | Xuất đi đâu |
|---|---|---|
| **Traces** | Span cho mỗi HTTP request, HTTP client call, câu lệnh EF Core, và Kafka publish/consume thủ công | OTLP → `otel-collector` → Elastic APM |
| **Metrics** | Counter/gauge runtime (GC, thread pool) + custom counter nghiệp vụ (outbox, inbox, reservation) | OTLP → `otel-collector` → Elastic APM |
| **Logs** | Serilog log event (mục 6) — kênh **mới** so với trước, chạy song song Console/Seq | OTLP → `otel-collector` → Elastic APM |

Cả 3 signal cùng đi qua 1 cổng OTLP gRPC (`4317`), cùng 1 `otel-collector`, cùng đích Elastic APM — xem pipeline tổng ở [Elastic-usage-guide.md](Elastic-usage-guide.md) mục 1.

## 2. Package OpenTelemetry SDK đang dùng

`Sales.Api.csproj` và `Inventory.Api.csproj` (giống hệt nhau):

```text
OpenTelemetry.Extensions.Hosting                  1.15.1
OpenTelemetry.Exporter.OpenTelemetryProtocol      1.16.0
OpenTelemetry.Instrumentation.AspNetCore          1.15.1
OpenTelemetry.Instrumentation.Http                1.15.1
OpenTelemetry.Instrumentation.Runtime             1.15.1
OpenTelemetry.Instrumentation.EntityFrameworkCore 1.16.0-beta.1
```

`AuditLog.Worker.csproj` — không cần instrumentation ASP.NET Core/HttpClient/EF Core (worker không phải web host, không dùng EF Core), chỉ cần phần lõi + runtime + OTLP exporter.

**Lưu ý version**: `OpenTelemetry.Instrumentation.EntityFrameworkCore` vẫn là bản `1.16.0-beta.1` phía upstream — toàn bộ dòng 1.x của package này chưa GA, đây là package chính thức duy nhất cho EF Core nên không có lựa chọn stable khác.

## 3. Khởi tạo tracing + metrics trong từng service

### 3.1 Sales.Api

`src/Services/Sales/Sales.Api/Program.cs`:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(x => x.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddEntityFrameworkCoreInstrumentation()
        .AddSource("Sales.Infrastructure.Kafka").AddOtlpExporter())
    .WithMetrics(x => x.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation()
        .AddMeter("Sales.Infrastructure").AddOtlpExporter());
```

- `AddAspNetCoreInstrumentation()`: span cho mỗi HTTP request vào.
- `AddHttpClientInstrumentation()`: span cho mỗi `HttpClient` call ra ngoài.
- `AddEntityFrameworkCoreInstrumentation()`: span cho mỗi câu lệnh EF Core chạy trên `SalesDbContext`.
- `AddSource("Sales.Infrastructure.Kafka")`: lắng nghe `ActivitySource` tự viết tay cho Kafka (mục 5) — không có dòng này thì span Kafka publish/consume bị tạo ra nhưng **không** được `TracerProvider` thu thập, coi như biến mất.
- `AddMeter("Sales.Infrastructure")`: đăng ký `Meter` custom (`SalesMetrics`, mục 4) vào `MeterProvider`.

### 3.2 Inventory.Api

`src/Services/Inventory/Inventory.Api/Program.cs` — cấu trúc giống hệt Sales.Api, chỉ khác tên:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(x => x.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddEntityFrameworkCoreInstrumentation()
        .AddSource("Inventory.Infrastructure.Kafka").AddOtlpExporter())
    .WithMetrics(x => x.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation()
        .AddMeter("Inventory.Infrastructure").AddOtlpExporter());
```

### 3.3 AuditLog.Worker

`src/Services/AuditLog/AuditLog.Worker/Program.cs` — không phải web host nên không có ASP.NET Core/HttpClient/EF Core instrumentation, không có custom Meter, nhưng vẫn có `AddSource` cho Kafka:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(x => x.AddSource("AuditLog.Infrastructure.Kafka").AddOtlpExporter())
    .WithMetrics(x => x.AddRuntimeInstrumentation().AddOtlpExporter());
```

### 3.4 Bảng so sánh nhanh

| | Sales.Api | Inventory.Api | AuditLog.Worker |
|---|---|---|---|
| ASP.NET Core instrumentation | ✅ | ✅ | ❌ (không phải web host) |
| HttpClient instrumentation | ✅ | ✅ | ❌ |
| EF Core instrumentation | ✅ | ✅ | ❌ (không dùng EF Core) |
| Runtime instrumentation | ✅ | ✅ | ✅ |
| Custom `AddSource` (Kafka) | `Sales.Infrastructure.Kafka` | `Inventory.Infrastructure.Kafka` | `AuditLog.Infrastructure.Kafka` |
| Custom `AddMeter` | `Sales.Infrastructure` | `Inventory.Infrastructure` | không có |

## 4. Custom metric — SalesMetrics / InventoryMetrics

`src/Services/Sales/Sales.Infrastructure/Observability/SalesMetrics.cs`:

```csharp
internal static class SalesMetrics
{
    private static readonly Meter Meter = new("Sales.Infrastructure");

    public static readonly Counter<long> OutboxPublished = Meter.CreateCounter<long>("sales.outbox.published");
    public static readonly Counter<long> OutboxFailed = Meter.CreateCounter<long>("sales.outbox.failed");
    public static readonly Counter<long> OutboxDeadLettered = Meter.CreateCounter<long>("sales.outbox.deadlettered");
    public static readonly Counter<long> InboxDuplicate = Meter.CreateCounter<long>("sales.inbox.duplicate");
    public static readonly Counter<long> InboxProcessed = Meter.CreateCounter<long>("sales.inbox.processed");

    private static long _outboxBacklog;
    private static long _outboxDeadLetters;

    static SalesMetrics()
    {
        Meter.CreateObservableGauge("sales.outbox.backlog", () => Interlocked.Read(ref _outboxBacklog));
        Meter.CreateObservableGauge("sales.outbox.deadletters", () => Interlocked.Read(ref _outboxDeadLetters));
    }

    public static void SetOutboxSnapshot(long backlog, long deadLetters)
    {
        Interlocked.Exchange(ref _outboxBacklog, backlog);
        Interlocked.Exchange(ref _outboxDeadLetters, deadLetters);
    }
}
```

`InventoryMetrics` (`src/Services/Inventory/Inventory.Infrastructure/Observability/InventoryMetrics.cs`) cùng hình dạng, thêm 2 counter nghiệp vụ riêng: `inventory.reservation.rejected`, `inventory.reservation.reserved`.

Tên `Meter("Sales.Infrastructure")` / `Meter("Inventory.Infrastructure")` phải khớp chính xác chuỗi truyền vào `AddMeter(...)` ở mục 3 — đây là cách 1 `Meter` custom được đưa vào `MeterProvider` để `AddOtlpExporter()` xuất đi. Sai tên (dù chỉ khác hoa/thường) là counter âm thầm biến mất khỏi Kibana, không có exception nào báo.

**Bẫy đặt tên dễ nhầm**: `sales.outbox.deadlettered` (Counter, tăng dần — 1 event "row vừa bị đưa vào DLQ") khác `sales.outbox.deadletters` (ObservableGauge, snapshot — "hiện có bao nhiêu row đang nằm trong DLQ"). Tên rất giống nhau nhưng khác loại metric. Bảng dashboard ở `observability.md` dùng tên gauge (`deadletters`).

### Nơi tăng counter

| Metric | File | Ngữ cảnh |
|---|---|---|
| `SalesMetrics.OutboxPublished`/`OutboxFailed` | `Sales.Infrastructure/Kafka/SalesOutboxPublisher.cs` | Mỗi vòng publish outbox (2 giây/lần) |
| `SalesMetrics.OutboxDeadLettered` | `Sales.Infrastructure/Kafka/SalesOutboxPublisher.cs` | Row vượt `MaxAttempts` |
| `SalesMetrics.SetOutboxSnapshot` | `Sales.Infrastructure/Kafka/SalesOutboxPublisher.cs` | Cập nhật lại backlog/DLQ mỗi vòng |
| `SalesMetrics.InboxDuplicate`/`InboxProcessed` | `Sales.Infrastructure/Kafka/SalesIntegrationEventHandler.cs` | Insert Inbox trùng/thành công |
| `InventoryMetrics.OutboxPublished`/`OutboxFailed`/`OutboxDeadLettered` | `Inventory.Infrastructure/Kafka/InventoryOutboxPublisher.cs` | Tương tự Sales |
| `InventoryMetrics.InboxDuplicate`/`InboxProcessed` | `Inventory.Infrastructure/Kafka/InventoryEventHandler.cs` | Tương tự Sales |
| `InventoryMetrics.ReservationRejected`/`ReservationReserved` | `Inventory.Infrastructure/Kafka/InventoryEventHandler.cs` (hàm `Reserve`) | Thiếu hàng / reserve thành công |

Redis cache (`ProductCache`) và HTTP request pipeline hiện **không có** custom metric riêng — chỉ dựa vào `AddAspNetCoreInstrumentation()`/`AddHttpClientInstrumentation()` chuẩn (xem mục 10).

## 5. Distributed tracing xuyên qua Kafka — `ActivitySource` thủ công

ASP.NET Core/HttpClient instrumentation (mục 3) tự tạo span cho HTTP, nhưng Kafka publish/consume thì KafkaFlow không tự tạo span — phải tự viết bằng `System.Diagnostics.ActivitySource`.

### 5.1 `ActivitySource` mỗi service

`src/Services/Sales/Sales.Infrastructure/Observability/SalesActivitySource.cs` (Inventory/AuditLog tương tự, khác tên):

```csharp
internal static class SalesActivitySource
{
    public const string Name = "Sales.Infrastructure.Kafka";
    public static readonly ActivitySource Instance = new(Name);
}
```

`Name` phải khớp chuỗi truyền vào `AddSource(...)` ở mục 3 — cùng nguyên tắc với `Meter` ở mục 4.

### 5.2 Producer — mở span, ghi header W3C

`Sales.Infrastructure/Kafka/KafkaOutboxPublisher.cs` (Inventory tương tự, khác tên `ActivitySource`/producer):

```csharp
using var activity = SalesActivitySource.Instance.StartActivity($"kafka.publish {message.Topic}", ActivityKind.Producer);
activity?.SetTag("messaging.system", "kafka");
activity?.SetTag("messaging.destination.name", message.Topic);

var headers = new KafkaHeaders();
var traceParent = activity?.Id ?? Activity.Current?.Id;
if (traceParent is not null) headers.SetString(ContractHeaders.TraceParent, traceParent);
var traceState = activity?.TraceStateString ?? Activity.Current?.TraceStateString;
if (!string.IsNullOrEmpty(traceState)) headers.SetString(ContractHeaders.TraceState, traceState);

var producer = producers.GetProducer("sales-outbox");
await producer.ProduceAsync(message.Topic, envelope.AggregateId.ToString(), envelope, headers);
```

`Activity.Id` khi `ActivityIdFormat` là W3C (mặc định .NET hiện tại) trả về đúng format `traceparent` (`00-{traceId}-{spanId}-{flags}`) — không cần tự format tay. `ContractHeaders.TraceParent`/`.TraceState` là 2 hằng số trong `BuildingBlocks.Contracts/Messaging/MessageHeaders.cs`.

### 5.3 Consumer — đọc header, nối span con qua helper dùng chung

Trước đây mỗi handler (`SalesIntegrationEventHandler`, `InventoryEventHandler`, `AuditEventHandler`) tự viết riêng 1 hàm parse `traceparent`. Giờ cả 3 dùng chung 1 helper:

`src/Shared/BuildingBlocks.Contracts/Messaging/TraceContextParser.cs`:

```csharp
public static class TraceContextParser
{
    public static ActivityContext Parse(string? traceParent, string? traceState)
    {
        if (string.IsNullOrEmpty(traceParent)) return default;
        return ActivityContext.TryParse(traceParent, traceState, out var parsed) ? parsed : default;
    }
}
```

Gọi ở đầu mỗi handler, ví dụ `SalesIntegrationEventHandler.Handle`:

```csharp
var parentContext = TraceContextParser.Parse(
    context.Headers.GetString(ContractHeaders.TraceParent),
    context.Headers.GetString(ContractHeaders.TraceState));
using var activity = SalesActivitySource.Instance.StartActivity(
    $"kafka.consume {context.ConsumerContext.Topic}", ActivityKind.Consumer, parentContext);
activity?.SetTag("messaging.system", "kafka");
activity?.SetTag("messaging.destination.name", context.ConsumerContext.Topic);
activity?.SetTag("messaging.kafka.consumer.group", context.ConsumerContext.GroupId);
```

`context.Headers.GetString(...)` trên header không tồn tại trả về `null` (không throw) nên message cũ/không có header vẫn consume bình thường — span consumer chỉ trở thành root mới thay vì có parent.

**`activity?.TraceId` còn được đẩy vào Serilog `LogContext`** (`LogContext.PushProperty("TraceId", activity?.TraceId.ToHexString())`, thấy trong cả 3 handler) — đây là cầu nối giữa log Seq và trace Kibana: filter Seq theo `TraceId` là ra đúng log của message Kafka đó, rồi lấy cùng `TraceId` tra tiếp trong Kibana APM là ra đúng trace/span.

### 5.4 Còn lại / giới hạn

- `EventEnvelope.CorrelationId` là correlation **nghiệp vụ** (Inbox/Mongo dedup theo workflow), khác `traceparent` (correlation **kỹ thuật**, nối span). 2 cơ chế tồn tại song song, xem mục 7.
- Chưa propagate `traceparent` xuyên outbox row: HTTP request gốc → ghi outbox → publish thật (có thể cách nhau vài giây/phút do outbox pattern decouple qua DB). Span Kafka publish hiện là root span mới tại thời điểm publish, không nối ngược về trace của request HTTP đã tạo ra outbox row đó — giới hạn hợp lý của outbox pattern, muốn nối cần lưu thêm `traceparent` vào chính outbox row lúc ghi (đổi schema, chưa làm).

## 6. Log giờ cũng là một phần của pipeline OpenTelemetry

Đây là điểm dễ nhầm nhất nếu chỉ đọc code cũ hoặc tài liệu cũ: **log không còn tách biệt hoàn toàn khỏi OTel nữa.**

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
        .WriteTo.OpenTelemetry(otel =>
        {
            otel.Endpoint = otlpEndpoint;
            otel.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
            otel.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = serviceName };
        });
}
```

Cả 3 service gọi đúng 1 helper này (`Sales.Api`/`Inventory.Api` qua `builder.Host.UseSerilog((ctx, cfg) => cfg.ConfigureSharedSinks(...))`, `AuditLog.Worker` qua `builder.Services.AddSerilog((_, cfg) => cfg.ConfigureSharedSinks(...))` vì dùng `HostApplicationBuilder` không có `.Host`) — không còn 3 block cấu hình Serilog copy/paste riêng như trước.

Package: `Serilog.Sinks.OpenTelemetry` `4.2.0`, khai trong `BuildingBlocks.Observability.csproj` — đóng gói cùng `Serilog`, `Serilog.Sinks.Console`, `Serilog.Sinks.Seq`.

Hệ quả:

- Mỗi log event giờ đi **3 nơi cùng lúc**: Console (docker logs), Seq (structured query theo property), và OTLP → `otel-collector` → Elastic APM (cùng đích với traces/metrics).
- `otel-collector-config.yaml` (`docker/`) giờ có **3 pipeline**, không phải 2 như trước:

```yaml
service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [otlp/elastic]
    metrics:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [otlp/elastic]
    logs:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [otlp/elastic]
```

- Enricher mới `Environment` (đọc `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT`, mặc định `Production`) xuất hiện trên mọi log — trước đây chỉ có `Service`.

**Vì sao vẫn giữ Seq song song thay vì chỉ dùng OTel logs?** Seq có UI query theo property nhanh, quen thuộc cho việc tra cứu thủ công hằng ngày (xem [Seqlog-usage-guide.md](Seqlog-usage-guide.md)); OTLP logs cho phép Kibana correlate log với đúng trace/span khi cần điều tra sâu 1 request/message cụ thể. Hai kênh phục vụ 2 thói quen tra cứu khác nhau, không kênh nào dư thừa.

Phần chi tiết Serilog (sink, enricher, masking HTTP body, `LoggingBehavior`/`ErrorLoggingBehavior`...) nằm ở [Seqlog-usage-guide.md](Seqlog-usage-guide.md) — tài liệu này chỉ nói phần OTLP sink mới thêm.

## 7. CorrelationId (nghiệp vụ) vs traceparent (kỹ thuật) — đừng nhầm 2 khái niệm

| | `EventEnvelope.CorrelationId` | `traceparent` (W3C) |
|---|---|---|
| Sinh ra ở đâu | `EventEnvelopeFactory` lúc tạo integration event | `Activity.Id` lúc `ActivitySource.StartActivity` |
| Dùng để làm gì | Gom log/event theo 1 **workflow nghiệp vụ** (confirm order → reserve → audit) | Nối **span kỹ thuật** trong Kibana APM (`TraceId`/`SpanId`/`ParentSpanId`) |
| Sống được bao lâu | Xuyên suốt cả workflow nhiều bước, nhiều Kafka message | Theo `ActivityIdFormat` W3C, tự sinh mới nếu không có parent |
| Tìm ở đâu | Seq: filter `CorrelationId = '...'` | Kibana APM: filter `trace.id` hoặc `TraceId` |

Cả 2 cùng tồn tại song song và **không thay thế nhau**: `CorrelationId` để tra theo nghiệp vụ ("workflow confirm order số X đi qua những bước nào"), `traceparent`/`TraceId` để tra theo kỹ thuật ("request/message này chậm ở span nào"). Xem thêm mục 5.4 cho giới hạn hiện tại của việc nối 2 khái niệm này qua outbox.

## 8. Biến môi trường điều khiển exporter

`AddOtlpExporter()` (traces/metrics) và `WriteTo.OpenTelemetry(...)` (logs, mục 6) đều không hard-code endpoint trong code — lấy từ biến môi trường/`IConfiguration`, khai trong `docker/docker-compose.yml`:

```yaml
environment:
  OTEL_EXPORTER_OTLP_ENDPOINT: http://otel-collector:4317
  OTEL_SERVICE_NAME: sales-api   # inventory-api / audit-worker tương ứng
```

| Biến | Đọc bởi | Ý nghĩa |
|---|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTel SDK (mặc định chuẩn), `SerilogBootstrap` (đọc tay qua `IConfiguration`) | Địa chỉ OTLP gRPC collector |
| `OTEL_SERVICE_NAME` | OTel SDK (mặc định chuẩn), `SerilogBootstrap` (đọc tay, fallback về tên service hard-code nếu thiếu) | Field `service.name` — khớp cột "Service" trong dashboard `observability.md`, và khớp property `Service` trong log Seq |
| `ASPNETCORE_ENVIRONMENT` | `SerilogBootstrap` | Field `Environment` trong log (mục 6) |

Cổng `4317` là gRPC — .NET OTLP exporter mặc định dùng gRPC khi không set `OTEL_EXPORTER_OTLP_PROTOCOL`. Chi tiết pipeline phía sau (`otel-collector` → APM Server → Elasticsearch → Kibana, thứ tự start container...) xem [Elastic-usage-guide.md](Elastic-usage-guide.md) mục 2, 5–7.

## 9. Cách thêm custom metric hoặc ActivitySource mới

Ví dụ muốn thêm metric đếm cache hit/miss cho Redis (hiện chưa có, xem [Redis-cache-usage-guide.md](Redis-cache-usage-guide.md) mục 9):

### Bước 1: Thêm counter vào `Meter` có sẵn

Trong `SalesMetrics.cs` (dùng lại `Meter("Sales.Infrastructure")` đã đăng ký, không tạo `Meter` mới):

```csharp
public static readonly Counter<long> CacheHit = Meter.CreateCounter<long>("sales.cache.hit");
public static readonly Counter<long> CacheMiss = Meter.CreateCounter<long>("sales.cache.miss");
```

### Bước 2: Gọi counter tại call site

Trong `GetProductHandler` (hoặc trong `CacheService<T>` nếu muốn áp dụng chung cho mọi entity cache-aside sau này):

```csharp
var cached = await cache.GetAsync(request.Id, ct);
if (cached is not null) { SalesMetrics.CacheHit.Add(1); return cached; }
SalesMetrics.CacheMiss.Add(1);
```

### Bước 3: Không cần đổi `AddMeter(...)` ở Program.cs

Vì `Meter("Sales.Infrastructure")` đã được đăng ký từ trước (mục 3) — counter mới tự động được `MeterProvider` thu thập.

### Nếu cần `ActivitySource` mới cho 1 luồng khác Kafka

1. Tạo class tương tự `SalesActivitySource` (`internal static class`, `Name` là chuỗi định danh, `Instance` là `ActivitySource`).
2. Thêm `.AddSource("Tên.ActivitySource")` vào `.WithTracing(...)` ở `Program.cs` — thiếu bước này thì span tạo ra không bao giờ tới Kibana.
3. Gọi `Instance.StartActivity(...)` bọc quanh đoạn code cần trace, set tag theo [semantic convention của OTel](https://opentelemetry.io/docs/specs/semconv/) nếu có (ví dụ `messaging.system`, `db.system`) để field hiển thị nhất quán trong Kibana.

## 10. Nên cải thiện trong production

Local MVP đang ổn cho bài thực hành, nhưng production nên thêm:

- Sampling cho traces (hiện không cấu hình sampler nào → mặc định `AlwaysOnSampler`, mọi request/message đều tạo trace — production tải cao nên cân nhắc `TraceIdRatioBasedSampler` để giảm chi phí lưu trữ).
- Custom metric cho Redis cache hit/miss (mục 9 là ví dụ, chưa implement thật).
- Propagate `traceparent` xuyên outbox row từ HTTP request gốc tới lúc publish thật (mục 5.4) — cần đổi schema outbox.
- Resource attribute đầy đủ hơn cho log OTLP sink (hiện chỉ set `service.name`, mục 6) — nên thêm `service.version`, `deployment.environment` để khớp semantic convention OTel resource.
- Bật `auth` cho APM Server, `xpack.security` cho Elasticsearch (xem [Elastic-usage-guide.md](Elastic-usage-guide.md) mục 10).

## 11. Các vị trí code cần nhớ

| Mục | File |
|---|---|
| Serilog + OTLP log sink dùng chung | `src/Shared/BuildingBlocks.Observability/SerilogBootstrap.cs` |
| HTTP request logging middleware dùng chung | `src/Shared/BuildingBlocks.Web/RequestObservabilityMiddleware.cs`, `RequestLoggingDefaults.cs` |
| Trace context parser dùng chung (Kafka consumer) | `src/Shared/BuildingBlocks.Contracts/Messaging/TraceContextParser.cs` |
| Header W3C traceparent/tracestate | `src/Shared/BuildingBlocks.Contracts/Messaging/MessageHeaders.cs` |
| Sales OTel init (traces+metrics) | `src/Services/Sales/Sales.Api/Program.cs` |
| Sales custom metric | `src/Services/Sales/Sales.Infrastructure/Observability/SalesMetrics.cs` |
| Sales ActivitySource | `src/Services/Sales/Sales.Infrastructure/Observability/SalesActivitySource.cs` |
| Sales Kafka producer tracing | `src/Services/Sales/Sales.Infrastructure/Kafka/KafkaOutboxPublisher.cs` |
| Sales Kafka consumer tracing | `src/Services/Sales/Sales.Infrastructure/Kafka/SalesIntegrationEventHandler.cs` |
| Inventory OTel init | `src/Services/Inventory/Inventory.Api/Program.cs` |
| Inventory custom metric | `src/Services/Inventory/Inventory.Infrastructure/Observability/InventoryMetrics.cs` |
| Inventory ActivitySource | `src/Services/Inventory/Inventory.Infrastructure/Observability/InventoryActivitySource.cs` |
| AuditLog OTel init | `src/Services/AuditLog/AuditLog.Worker/Program.cs` |
| AuditLog ActivitySource + consumer tracing | `src/Services/AuditLog/AuditLog.Infrastructure/Observability/AuditActivitySource.cs`, `Mongo/AuditEventHandler.cs` |
| OTel Collector pipeline config | `docker/otel-collector-config.yaml` |

## 12. Lệnh kiểm tra nhanh OTel local

Start stack:

```bash
sudo docker compose -f docker/docker-compose.yml up -d --build
```

Gọi vài request/confirm order để sinh trace, rồi mở Kibana:

```text
http://localhost:5601
```

Vào APM → chọn service `sales-api`/`inventory-api`/`audit-worker` → xem trace, hoặc Discover trên index `logs-*`/`traces-apm-*`/`metrics-apm-*` để tra theo `TraceId`.

Nếu không thấy trace nào xuất hiện: kiểm tra theo thứ tự

```text
1. otel-collector có start thành công không (logs container)
2. app có log lỗi export OTLP không (thường log ở Debug/Warning nếu Console.SDK bật)
3. OTEL_EXPORTER_OTLP_ENDPOINT đúng chưa (phải là http://otel-collector:4317 trong mạng Docker)
4. apm-server có nhận traffic không (logs container apm-server)
```

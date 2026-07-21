# 14. Background job & lập lịch

## Mục đích

Có bốn cơ chế khác nhau chạy công việc bên ngoài request. Giải thích mỗi cơ chế dùng để làm gì, và vì sao lựa chọn không hề tùy tiện.

## Bốn cơ chế

| Cơ chế | Dùng cho | Ở đâu |
|---|---|---|
| Hangfire recurring job | công việc nghiệp vụ theo lịch cron, xem được trên dashboard | chỉ Sales |
| `BackgroundService` | vòng lặp liên tục gắn với vòng đời host | cả hai API, và worker |
| Kafka consumer | phản ứng với một service khác | cả hai API, và worker |
| Startup task | chuẩn bị một lần trước khi nhận traffic | mọi host |

## Hangfire (Sales)

Nơi lưu trữ là database PostgreSQL `hangfire`, nên lịch và lịch sử sống sót qua restart. Ba hàng đợi:

```csharp
options.Queues = [HangfireQueueNames.Critical, HangfireQueueNames.Default, HangfireQueueNames.Maintenance];
```

Hiện có hai job:

| Job id | Class | Cron | Queue | Công việc |
|---|---|---|---|---|
| `sales-cleanup` | `MaintenanceCleanupJob` | `0 0 * * *` | `maintenance` | xóa các dòng inbox/outbox đã xử lý cũ hơn 14 ngày |
| `orders:cancel-expired` | `CancelExpiredPendingOrdersJob` | `*/5 * * * *` | `critical` | hủy các đơn để không quá `ExpirationMinutes` |

### Id nằm trong code, lịch nằm trong config

```csharp
public static class SalesRecurringJobIds
{
    public const string MaintenanceCleanup = "sales-cleanup";
    public const string CancelExpiredPendingOrders = "orders:cancel-expired";
}
```

`RecurringJobSettings` mang `Enabled`, `Cron` và `Queue` — nhưng cố ý **không** mang job id:

> Định danh job cố ý vắng mặt: chúng nằm trong các hằng số do service sở hữu, để một thay đổi cấu hình không thể tạo ra một recurring job thứ hai.

Đổi một id có thể cấu hình được thì Hangfire sẽ vui vẻ đăng ký thêm một job *thứ hai* theo lịch cũ. Cả hai cùng chạy. Đó là một sự cố production thực sự khó chịu, được ngăn bằng một quyết định thiết kế.

### Cơ chế đăng ký được dùng chung

```csharp
public static void ScheduleRecurringJob<TJob>(this IRecurringJobManager manager,
    string recurringJobId, RecurringJobSettings settings, Expression<Func<TJob, Task>> jobExpression)
{
    if (!settings.Enabled) { manager.RemoveIfExists(recurringJobId); return; }
    ArgumentException.ThrowIfNullOrWhiteSpace(settings.Queue);
    ArgumentException.ThrowIfNullOrWhiteSpace(settings.Cron);
    manager.AddOrUpdate(recurringJobId, settings.Queue, jobExpression, settings.Cron,
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}
```

Hai chi tiết đáng học theo:

- **tắt job thì xóa hẳn nó đi** chứ không phải bỏ qua. Bỏ qua sẽ để nó vẫn đăng ký theo lịch cũ, nên bật lại sau này sẽ làm sống dậy một cron cũ.
- **luôn dùng UTC.** Một cron theo giờ địa phương sẽ âm thầm dịch chuyển hai lần mỗi năm.

### Cấu hình được kiểm tra lúc khởi động

```csharp
services.AddOptions<SalesRecurringJobsOptions>()
    .Bind(configuration.GetSection(SalesRecurringJobsOptions.SectionName))
    .ValidateOnStart();
```

`SalesRecurringJobsOptionsValidator` nêu đích danh job có lỗi trong thông báo. `RecurringJobSettings.IsValid()` parse cron bằng Cronos (5 hoặc 6 trường), và `CancelExpiredPendingOrdersJobOptions.IsValid()` chỉ kiểm tra tham số nghiệp vụ khi job được bật — một job đã tắt thì không cần lịch.

Một cron gõ sai sẽ làm fail lần deploy, chứ không phải lần chạy lúc 3 giờ sáng.

### Job chỉ là một adapter

```csharp
public async Task ExecuteAsync(int expirationMinutes, int batchSize, CancellationToken ct = default)
{
    var result = await sender.Send(new CancelExpiredPendingOrders(clock.UtcNow, expirationMinutes, batchSize), ct);
    SalesMetrics.RecordExpiredOrderCancellation(result.ScannedOrderCount, result.CancelledOrderCount,
        result.SkippedOrderCount, result.FailedOrderCount, stopwatch.Elapsed.TotalMilliseconds);
    logger.LogInformation("CancelExpiredPendingOrdersJob completed {ScannedOrderCount} …");
}
```

Job không chứa logic nghiệp vụ — nó gửi một command, ghi metric, ghi log. Quy tắc nghiệp vụ nằm trong một handler và unit-test được mà không cần Hangfire, còn `IClock` nghĩa là thời gian có thể inject được.

### Cô lập theo từng đơn hàng

`CancelExpiredPendingOrdersHandler` xử lý mỗi đơn trong **scope riêng của nó**:

```csharp
using var scope = serviceScopeFactory.CreateScope();
var scopedOrderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
```

Một đơn hỏng không thể làm fail cả batch; một lỗi sẽ tăng `failedOrderCount`, ghi warning, và vòng lặp tiếp tục. Nó cũng kiểm tra lại `CancelDueToExpiration` cho từng đơn, vì kết quả quét có thể đã cũ vào lúc xử lý.

## `BackgroundService`

| Service | Nhịp chạy |
|---|---|
| `SalesOutboxPublisher` / `InventoryOutboxPublisher` | theo tín hiệu, có poll dự phòng |
| `SalesInboxRedriveService` / `InventoryInboxRedriveService` | cố định 15 s |
| `InventoryMaintenanceWorker` | `PeriodicTimer`, một lần lúc khởi động rồi mỗi ngày |
| `MongoStartupService`, `KafkaBusService` | chạy một lần / theo vòng đời |

Hình dạng luôn giống nhau:

```csharp
while (!stoppingToken.IsCancellationRequested)
{
    try { await using var scope = scopes.CreateAsyncScope(); /* work */ }
    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
    { logger.LogError(ex, "… cycle failed"); }
    await signal.WaitAsync(pollInterval, stoppingToken);
}
```

Scope mới cho mỗi chu kỳ (một `BackgroundService` là singleton; `DbContext` là scoped và không thread-safe), bắt lỗi rồi tiếp tục (một chu kỳ hỏng không được giết cả vòng lặp), và luôn tôn trọng stopping token.

### Tín hiệu tốt hơn polling

```csharp
public void Notify() => channel.Writer.TryWrite(true);

public async Task WaitAsync(TimeSpan fallbackInterval, CancellationToken ct)
{
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeout.CancelAfter(fallbackInterval);
    try { if (!await channel.Reader.WaitToReadAsync(timeout.Token)) return;
          while (channel.Reader.TryRead(out _)) { } }
    catch (OperationCanceledException) when (!ct.IsCancellationRequested) { /* poll fallback */ }
}
```

Một channel giới hạn kích thước 1 với `DropWrite`: bao nhiêu lần lưu cũng gộp lại thành một lần đánh thức. Độ trễ publish giảm từ "tối đa bằng khoảng poll" xuống "ngay lập tức", trong khi polling vẫn ở đó như đường phục hồi — một tín hiệu trong tiến trình sẽ mất khi restart, và các lượt ghi của instance khác không bao giờ báo hiệu cho bạn.

### Vì sao Inventory không dùng Hangfire

`InventoryMaintenanceWorker` dùng `PeriodicTimer` thay thế. Inventory không phụ thuộc Hangfire, và thêm nguyên một framework job chỉ để dọn dẹp mỗi ngày một lần là quá mức cần thiết. Nó phối hợp bằng advisory lock của Postgres thay vì Redis, giữ danh sách phụ thuộc ngắn gọn.

## Startup task

```csharp
public static async Task RunStartupTasksAsync(this WebApplication app)
{
    var kafkaBus = await KafkaBusLifecycle.StartAsync(app.Services);
    app.Lifetime.ApplicationStopping.Register(() => KafkaBusLifecycle.StopAsync(kafkaBus).GetAwaiter().GetResult());
    await app.Services.SeedIdentityAsync();
    app.Services.RegisterSalesRecurringJobs();
}
```

Đoạn `GetAwaiter().GetResult()` là lời gọi chặn duy nhất được chấp nhận trong solution — `ApplicationStopping` nhận một callback đồng bộ.

`MongoStartupService` cho thấy hình dạng đúng cho một phụ thuộc có thể chưa sẵn sàng: ping có retry (20 lần, cách nhau 2 s), tạo index theo kiểu idempotent, và ném lại ở lần thử cuối để container fail nhanh thay vì phục vụ trong trạng thái hỏng.

## Idempotency là bắt buộc

Mọi thao tác theo lịch hoặc chạy vòng lặp đều phải an toàn khi chạy hai lần, vì khóa hết hạn và instance restart:

| Thao tác | Bảo vệ bởi | Vì sao tự nó vẫn an toàn |
|---|---|---|
| Publish outbox | lease 30 s | publish trùng được inbox của consumer khử |
| Dọn dẹp Sales | khóa Redis | xóa theo điều kiện — chạy hai lần cũng không xóa thêm gì |
| Dọn dẹp Inventory | advisory lock | như trên |
| Hủy đơn hết hạn | kiểm tra lại theo từng đơn | `CancelDueToExpiration` trả về false với đơn đã hủy |

Khóa là một tối ưu. Chính thao tác mới là sự đảm bảo.

## Lỗi thường gặp

| Sai lầm | Hậu quả |
|---|---|
| Inject một scoped service vào constructor của `BackgroundService` | captive dependency, `DbContext` bị chia sẻ giữa các thread |
| Cho phép cấu hình job id | một thay đổi config tạo ra job trùng |
| Cron theo giờ địa phương | lịch dịch chuyển hai lần mỗi năm |
| Tắt job bằng cách bỏ qua thay vì xóa | nó sống lại theo lịch cũ |
| Không có try/catch trong vòng lặp | một chu kỳ hỏng giết cả vòng lặp cho tới khi restart |
| Logic nghiệp vụ nằm trong class job | không test được nếu không có Hangfire |
| Dựa vào khóa để đảm bảo tính đúng đắn | khóa hết hạn giữa chừng thao tác |

## Liên quan

- [../tech/background-jobs.md](../tech/background-jobs.md)
- [07-domain-events-and-outbox.md](07-domain-events-and-outbox.md)
- [../tech/retry-and-dead-letter.md](../tech/retry-and-dead-letter.md)

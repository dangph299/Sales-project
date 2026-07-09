# Refactor Shared Infrastructure (Sales + Inventory) — Design

## Mục tiêu

Loại bỏ trùng lặp thực sự trong Infrastructure layer giữa Sales và Inventory, chuyển các thành phần dùng chung vào `Shared/BuildingBlocks.*`, không đổi business behavior, không đổi database schema nếu không thực sự cần thiết. Tuân thủ `docs/CODING_RULES.md`.

## Phạm vi

Chỉ Sales + Inventory Infrastructure layer. Không đụng Domain/Application. Không đụng AuditLog (dù `AuditActivitySource` có cùng pattern trùng lặp với `SalesActivitySource`/`InventoryActivitySource` — cố ý loại khỏi phạm vi lần này theo quyết định của chủ dự án; thiết kế Phase 4 vẫn để ngỏ khả năng AuditLog dùng lại sau này mà không cần rework).

## Hiện trạng đã khảo sát

Đã đối chiếu trực tiếp từng cặp file giữa `Sales.Infrastructure` và `Inventory.Infrastructure`:

| Thành phần | Sales | Inventory | Mức trùng lặp |
|---|---|---|---|
| Outbox entity | `OutboxMessage` | `OutboxRow` | 100% giống cấu trúc (11 property + const), chỉ khác tên type |
| `IOutboxPublisher` | có | có | 100% giống, chỉ khác type tham chiếu |
| `EventEnvelopeFactory` | có | có | 100% giống hệt (65 dòng, chỉ khác namespace) |
| `KafkaOutboxPublisher` | có | có | Giống hệt logic, khác ActivitySource instance + producer name string |
| Inbox entity | `InboxMessage` (có cột `Consumer` NOT NULL) | `InboxRow` (không có cột này) | **Khác schema thật sự**, không phải chỉ khác tên |
| ActivitySource | `SalesActivitySource` | `InventoryActivitySource` | 100% giống cấu trúc, chỉ khác `Name` string |
| Metrics | `SalesMetrics` | `InventoryMetrics` | 5 counter + 2 gauge giống hệt, Inventory có thêm 2 counter riêng (Reservation) |
| Outbox BackgroundService | `SalesOutboxPublisher` | `InventoryOutboxPublisher` | ~95% giống (poll/lock/backoff/dead-letter), khác DbContext/DbSet/Metrics |
| Consumer handler | `SalesIntegrationEventHandler` | `InventoryEventHandler` | Business dispatch khác hẳn nhau; chỉ `IsUniqueViolation` + đoạn mở activity là trùng y hệt |
| `TraceContextParser`, `MessageHeaders` | đã ở `BuildingBlocks.Contracts` | đã ở `BuildingBlocks.Contracts` | **Đã dùng chung từ trước — không cần làm gì** |

Xác nhận qua migration files: cả `OutboxMessageConfiguration` (Sales) và `OutboxRowConfiguration` (Inventory) đều dùng `entity.ToTable("outbox_messages")` tường minh — đổi tên CLR type không ảnh hưởng schema. Ngược lại, `Consumer` là cột `nullable: false` chỉ tồn tại trong migration của Sales (`20260706080857_InitialSales.cs`), không tồn tại ở Inventory — xác nhận Inbox là khác schema thật, không unify.

## Quyết định phạm vi đã chốt với chủ dự án

- **AuditLog**: ngoài phạm vi, không đụng.
- **Inbox**: giữ nguyên `InboxMessage`/`InboxRow` riêng biệt — khác schema thật (cột `Consumer`), không phải chỉ khác tên.
- **ActivitySource (Phase 4)**: chọn **Option A** — DI-inject `ActivitySource` trực tiếp, không tạo wrapper class/factory mới.
- **BackgroundService polling outbox (Phase 6)**: **giữ nguyên, không hợp nhất**. Chấp nhận ~120 dòng trùng lặp giữa `SalesOutboxPublisher`/`InventoryOutboxPublisher` để không tăng rủi ro cho phần code nhạy cảm nhất về độ tin cậy publish message. Có thể xem xét lại trong một effort riêng sau khi Phase 1-5, 7 đã chạy ổn định trong production.

## Dự án mới: `BuildingBlocks.Infrastructure`

`src/Shared/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj`:
- `ProjectReference` → `BuildingBlocks.Contracts`.
- Packages: `KafkaFlow`, `Microsoft.EntityFrameworkCore` (chỉ cho `DbUpdateException`/EF exception types dùng trong `PostgresExceptions`), `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.Extensions.Logging.Abstractions`.
- Thêm vào solution. `Sales.Infrastructure.csproj` và `Inventory.Infrastructure.csproj` thêm `ProjectReference` tới project này.

---

## Phase 1 — `EventEnvelopeFactory` → `BuildingBlocks.Contracts`

**Thay đổi:** Di chuyển nguyên văn `EventEnvelopeFactory.Create<T>(...)` từ cả hai service vào `BuildingBlocks.Contracts/Messaging/EventEnvelopeFactory.cs`. Đổi `internal static class` → `public static class` (bắt buộc để gọi được xuyên assembly). Xóa 2 file gốc. Cập nhật các nơi gọi (`DomainEventMapper`, `InventoryEventHandler`, `InventoryService`, `SalesDbContext`/`InventoryDbContext` nếu có) để dùng type từ `BuildingBlocks.Contracts` (namespace đã có sẵn `using BuildingBlocks.Contracts;` ở hầu hết các file này).

1. **Code cũ hạn chế ở đâu:** Hai bản `internal static class` giống hệt nhau 65 dòng, bất kỳ thay đổi logic serialize/envelope nào (ví dụ đổi cách sinh `CorrelationId` mặc định) đều phải sửa 2 nơi, dễ lệch nhau âm thầm.
2. **Vì sao có duplication:** Sales và Inventory được bootstrap từ cùng một template lúc đầu, không ai rút ra dùng chung.
3. **Vì sao thiết kế mới tốt hơn:** Một điểm định nghĩa duy nhất cho khái niệm "build EventEnvelope" — đúng với vai trò của `BuildingBlocks.Contracts` là nơi chứa contract/mapping logic không phụ thuộc framework hạ tầng.
4. **SOLID cải thiện:** DRY (không phải SOLID formal nhưng là mục tiêu chính); gián tiếp cải thiện Single Responsibility ở mức solution — logic tạo envelope chỉ có một chủ sở hữu.
5. **Vì sao vẫn đơn giản:** Không đổi signature, không thêm generic/abstraction mới — chỉ đổi vị trí file và access modifier.
6. **Rủi ro tương thích ngược:** Không — cùng namespace `BuildingBlocks.Contracts` đã được import ở hầu hết call site; `internal` → `public` không phá vỡ gì vì trước đây private mỗi assembly.
7. **Cần migration:** Không.

---

## Phase 2 — Outbox entity + `IOutboxPublisher` dùng chung; Inbox giữ nguyên

**Thay đổi:**
- Tạo `BuildingBlocks.Infrastructure.OutboxMessage` (giữ tên `OutboxMessage`, mô tả đúng vai trò hơn `OutboxRow`) với đúng 11 property + `MaxAttempts` const như bản hiện tại.
- Thêm factory tĩnh `OutboxMessage.From(EventEnvelope envelope, string topic)` để dedupe khối object-initializer đang lặp lại y hệt ở `SalesDbContext.SaveChangesAsync` và `InventoryDbContext.Enqueue`.
- Di chuyển `IOutboxPublisher` (nay tham chiếu `BuildingBlocks.Infrastructure.OutboxMessage`) vào cùng project.
- Xóa `Sales.Infrastructure/Persistence/Outbox/OutboxMessage.cs`, `Inventory.Infrastructure/Persistence/Outbox/OutboxRow.cs`, và 2 file `IOutboxPublisher.cs` gốc.
- Cập nhật `OutboxMessageConfiguration`/`OutboxRowConfiguration` (đổi tên file Inventory → `OutboxMessageConfiguration.cs`) trỏ tới type mới, `ToTable("outbox_messages")` giữ nguyên ở cả hai bên.
- Đổi tên DbSet Inventory từ `Outbox` → giữ nguyên tên hiện tại (không bắt buộc đổi ở Phase 2 vì Phase 6 bị bỏ qua — không có nhu cầu thống nhất tên property).
- **Inbox: không đổi gì.** `InboxMessage`/`InboxRow` giữ nguyên như hiện tại.

1. **Code cũ hạn chế ở đâu:** Hai class định nghĩa cùng một khái niệm hạ tầng (outbox row cho transactional outbox pattern) bằng 2 tên khác nhau (`OutboxMessage`/`OutboxRow`), khiến người đọc mới phải xác nhận lại 2 lần rằng chúng thực sự giống nhau.
2. **Vì sao có duplication:** Không có nơi nào để đặt một "entity hạ tầng thuần" dùng chung trước khi có `BuildingBlocks.Infrastructure`.
3. **Vì sao thiết kế mới tốt hơn:** Một type, một field spec, một index spec (mỗi service tự khai `IEntityTypeConfiguration` riêng vì đó là mapping cho DbContext/database riêng — không phải duplication, là đúng bản chất 2 database độc lập).
4. **SOLID cải thiện:** Open/Closed nhẹ — thêm field mới vào outbox pattern chỉ sửa 1 chỗ áp dụng cho cả hai; Single Responsibility — `OutboxMessage` chỉ mô tả dữ liệu, không lẫn business.
5. **Vì sao vẫn đơn giản:** Là POCO thuần, không EF dependency trực tiếp trên class; không thêm generic; `From(...)` là 1 static factory method ngắn, không side-effect.
6. **Rủi ro tương thích ngược:** Không — `.ToTable("outbox_messages")` tường minh ở cả 2 bên, xác nhận đổi CLR type name không đổi schema.
7. **Cần migration:** Không, nhưng **bắt buộc chạy probe** `dotnet ef migrations add ZZ_Probe` cho cả 2 DbContext sau khi đổi để xác nhận `Up`/`Down` rỗng (theo CODING_RULES.md §8), rồi xóa migration probe.

---

## Phase 3 — `KafkaOutboxPublisher` dùng chung

**Thay đổi:** Di chuyển `KafkaOutboxPublisher` vào `BuildingBlocks.Infrastructure`, nhận `ActivitySource` và `producerName` qua constructor thay vì hardcode:

```csharp
public sealed class KafkaOutboxPublisher(
    IProducerAccessor producers, ILogger<KafkaOutboxPublisher> logger,
    ActivitySource activitySource, string producerName) : IOutboxPublisher
```

Đăng ký DI ở mỗi service qua factory lambda (không tạo type Options mới cho 1 giá trị string dùng 1 lần, theo CODING_RULES.md §10):

```csharp
services.AddSingleton<IOutboxPublisher>(sp => new KafkaOutboxPublisher(
    sp.GetRequiredService<IProducerAccessor>(), sp.GetRequiredService<ILogger<KafkaOutboxPublisher>>(),
    sp.GetRequiredService<ActivitySource>(), "sales-outbox"));
```

Xóa 2 file `KafkaOutboxPublisher.cs` gốc.

1. **Code cũ hạn chế ở đâu:** Logic publish (mở span, set tag, propagate traceparent/tracestate, log) lặp lại y hệt; producer name và ActivitySource là 2 điểm khác biệt duy nhất nhưng nằm rải rác trong thân method thay vì được tham số hóa.
2. **Vì sao có duplication:** Copy-paste ban đầu, chưa từng tham số hóa 2 điểm khác biệt.
3. **Vì sao thiết kế mới tốt hơn:** Behavior publish-với-tracing chỉ định nghĩa 1 lần; khác biệt giữa 2 service chỉ còn là data (constructor arg), không phải code.
4. **SOLID cải thiện:** Dependency Inversion — publisher phụ thuộc `ActivitySource`/tên producer được inject thay vì tự biết static class cụ thể của từng service.
5. **Vì sao vẫn đơn giản:** Không dùng generic, không dùng Options pattern thừa — chỉ thêm 2 tham số constructor nguyên thủy (string, ActivitySource).
6. **Rủi ro tương thích ngược:** Thấp — hành vi publish (topic, key, header, log field) giữ nguyên 100%; chỉ thay đổi nội bộ DI wiring.
7. **Cần migration:** Không.

---

## Phase 4 — ActivitySource: DI-inject trực tiếp (Option A)

**Thay đổi:** Xóa `SalesActivitySource.cs` và `InventoryActivitySource.cs`. Đăng ký `ActivitySource` là singleton trong `AddSalesMessaging`/`AddInventoryInfrastructure`:

```csharp
services.AddSingleton(new ActivitySource("Sales.Infrastructure.Kafka"));
```

`KafkaOutboxPublisher`, `SalesIntegrationEventHandler`, `InventoryEventHandler` nhận `ActivitySource` qua constructor injection thay vì `XxxActivitySource.Instance`. Chuỗi tên nguồn (`"Sales.Infrastructure.Kafka"`) giữ nguyên giá trị, chỉ đổi nơi khai báo — `Program.cs` vẫn giữ literal riêng cho `.AddSource(...)` như hiện tại (đã là 2 nguồn sự thật từ trước, không phải regression mới).

1. **Code cũ hạn chế ở đâu:** 2 class `internal static class` chỉ bọc 1 const string + 1 static readonly field, không có logic gì khác — abstraction thừa cho một giá trị cấu hình đơn giản.
2. **Vì sao có duplication:** Copy nguyên khung khi tạo Inventory từ template Sales.
3. **Vì sao thiết kế mới tốt hơn:** `ActivitySource` (BCL type) chính là "shared component" — không cần đặt tên, không cần bảo trì thêm 1 type mới. Đây là lựa chọn advocate bởi chính CODING_RULES tinh thần "generic quá mức"/"tránh tạo abstraction khi không cần".
4. **SOLID cải thiện:** Dependency Inversion (constructor injection thay vì static field access) — dễ test hơn (có thể inject `ActivitySource` giả trong unit test nếu cần).
5. **Vì sao vẫn đơn giản:** Zero custom code mới — chỉ là 1 dòng đăng ký DI mỗi service.
6. **Rủi ro tương thích ngược:** Trung bình-thấp — đổi constructor signature của 3 class (`KafkaOutboxPublisher`, 2 handler); cần cập nhật mọi nơi `new`/DI resolve các class này (chủ yếu chỉ trong `DependencyInjection.cs` và test setup).
7. **Cần migration:** Không.

---

## Phase 5 — Metrics dùng chung, giữ nguyên call site

**Thay đổi:** Thêm `BuildingBlocks.Observability.OutboxMetrics` (constructor `(string meterName, string prefix)`, expose `Published`/`Failed`/`DeadLettered` counters + `SetSnapshot(backlog, deadLetters)`) và `InboxMetrics` (constructor tương tự, expose `Duplicate`/`Processed`). `SalesMetrics`/`InventoryMetrics` giữ nguyên là `internal static class`, nhưng nội bộ ủy quyền:

```csharp
internal static class SalesMetrics
{
    private static readonly OutboxMetrics Outbox = new("Sales.Infrastructure", "sales");
    private static readonly InboxMetrics Inbox = new("Sales.Infrastructure", "sales");
    public static Counter<long> OutboxPublished => Outbox.Published;
    public static Counter<long> OutboxFailed => Outbox.Failed;
    public static Counter<long> OutboxDeadLettered => Outbox.DeadLettered;
    public static Counter<long> InboxDuplicate => Inbox.Duplicate;
    public static Counter<long> InboxProcessed => Inbox.Processed;
    public static void SetOutboxSnapshot(long backlog, long deadLetters) => Outbox.SetSnapshot(backlog, deadLetters);
}
```

`InventoryMetrics` tương tự, cộng thêm 2 counter riêng (`ReservationRejected`, `ReservationReserved`) định nghĩa tại chỗ như hiện tại. **Toàn bộ call site hiện có (`SalesMetrics.OutboxPublished.Add(1)`...) không đổi.**

1. **Code cũ hạn chế ở đâu:** 5 counter + 2 gauge + hàm snapshot lặp lại gần như y hệt (chỉ khác tiền tố chuỗi tên metric), ai thêm counter mới cho outbox phải nhớ thêm ở cả 2 nơi đúng convention đặt tên.
2. **Vì sao có duplication:** Không có khái niệm "outbox metrics" tái sử dụng được, mỗi service tự định nghĩa `Meter` riêng.
3. **Vì sao thiết kế mới tốt hơn:** Logic tạo counter/gauge cho outbox/inbox chỉ định nghĩa 1 lần; phần khác biệt thật sự (business: `ReservationRejected/Reserved`) vẫn ở đúng chỗ của nó tại Inventory.
4. **SOLID cải thiện:** Single Responsibility — `OutboxMetrics`/`InboxMetrics` chỉ chịu trách nhiệm nhóm counter tương ứng; `SalesMetrics`/`InventoryMetrics` chỉ còn là composition root cho metrics của từng service.
5. **Vì sao vẫn đơn giản:** Instance-based nhưng không DI-inject (giữ static forwarding) để **zero call-site churn** — đây là lựa chọn cố ý ưu tiên an toàn hơn "thuần idiomatic".
6. **Rủi ro tương thích ngược:** Rất thấp — tên metric string (`"sales.outbox.published"`...) truyền tường minh qua `prefix`, không suy luận, nên dashboard/alert dựa vào tên cũ không bị ảnh hưởng.
7. **Cần migration:** Không (không phải DB).

---

## Phase 7 — Consumer handler: chỉ dedupe phần thuần kỹ thuật

**Thay đổi:**
1. Thêm `BuildingBlocks.Infrastructure.PostgresExceptions.IsUniqueViolation(DbUpdateException ex)`, thay cho 2 bản private static method y hệt trong `SalesIntegrationEventHandler`/`InventoryEventHandler`.
2. Thêm `BuildingBlocks.Infrastructure.KafkaConsumerActivity.Start(ActivitySource source, IMessageContext context)` — gói đoạn `TraceContextParser.Parse(...)` + `StartActivity(..., ActivityKind.Consumer, parentContext)` + 3 `SetTag` đang lặp y hệt ở cả 2 handler, trả về `Activity?`.

**Cố ý KHÔNG làm:** không gộp toàn bộ `Handle()` (log scope, stopwatch, structured log, business dispatch) vào 1 template method dùng chung — logic dispatch khác hẳn nhau về bản chất nghiệp vụ (Sales: order status transition; Inventory: stock reservation), gộp sẽ tạo ra "Delegate phức tạp" đúng như điều cấm trong yêu cầu.

1. **Code cũ hạn chế ở đâu:** `IsUniqueViolation` và đoạn mở activity là 2 khối code thuần kỹ thuật (không chứa nghiệp vụ) bị copy-paste nguyên văn.
2. **Vì sao có duplication:** Cả 2 handler được viết độc lập theo cùng 1 pattern nhưng không rút ra static helper.
3. **Vì sao thiết kế mới tốt hơn:** Đây là ví dụ đúng chuẩn "trích xuất khi có ít nhất 2 nơi dùng và duplication rõ ràng" — không đụng vào phần business.
4. **SOLID cải thiện:** Single Responsibility mức nhỏ — việc "nhận diện lỗi trùng khóa Postgres" và "mở tracing span cho consumer" tách khỏi logic điều phối nghiệp vụ.
5. **Vì sao vẫn đơn giản:** 2 static method nhỏ, không state, không side-effect ẩn, dễ đọc, dễ debug hơn vì logic dispatch nghiệp vụ giờ ngắn gọn hơn, không lẫn với boilerplate tracing.
6. **Rủi ro tương thích ngược:** Không — hành vi log/trace/duplicate-detection giữ nguyên hệt, chỉ đổi vị trí code.
7. **Cần migration:** Không.

---

## Ngoài phạm vi (đã quyết định, không làm trong effort này)

- **AuditLog / `AuditActivitySource`**: không đụng.
- **Inbox unification**: không làm — khác schema thật (`Consumer` column).
- **Phase 6 (BackgroundService polling outbox)**: không làm — giữ nguyên `SalesOutboxPublisher`/`InventoryOutboxPublisher` như hiện tại, chấp nhận duplicate để không tăng rủi ro cho phần code nhạy cảm nhất.

## Kế hoạch xác minh (áp dụng cho từng phase)

Theo đúng yêu cầu "không refactor nhiều thành phần cùng lúc nếu chưa xác nhận bước trước hoạt động chính xác":

1. Thực hiện đúng 1 phase.
2. `dotnet build` toàn solution.
3. Chạy toàn bộ `tests/` (đặc biệt `Sales.Infrastructure.Tests`, `Inventory.Infrastructure.Tests`, `Sales.Architecture.Tests`).
4. Với Phase 2: chạy thêm `dotnet ef migrations add ZZ_Probe` cho `SalesDbContext` và `InventoryDbContext`, xác nhận `Up`/`Down` rỗng, xóa migration probe.
5. Cập nhật `using`/namespace ở test files tham chiếu trực tiếp các type đã di chuyển (`OutboxReliabilityTests.cs`, `InventoryPostgresReliabilityTests.cs`) — không đổi assertion logic.
6. Chỉ sang phase tiếp theo sau khi build + test xanh.

## Thứ tự thực hiện

Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 7. (Phase 6 bị loại khỏi kế hoạch.)

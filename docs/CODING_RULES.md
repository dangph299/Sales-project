# Coding Rules

Quy tắc bắt buộc cho toàn bộ solution (Sales, Inventory, AuditLog, BuildingBlocks.Contracts). Mục tiêu: codebase dễ đọc, dễ tìm, đúng biên giới kiến trúc (DDD / Clean Architecture / CQRS).

## 1. One top-level type per file

- Mỗi `class` / `interface` / `record` / `enum` / `struct` ở cấp namespace (top-level) phải nằm trong **một file riêng**.
- **Tên file phải trùng chính xác với tên type** (bao gồm cả generic, ví dụ `Repository<T>` → `Repository.cs`, không viết `Repository{T}.cs`).
- Không gộp nhiều command/query/handler/DTO/entity/repository vào cùng một file, dù chúng liên quan chặt chẽ về nghiệp vụ.

**Ví dụ đúng:**
```
Commands/Customers/CreateCustomer.cs        -> record CreateCustomer
Commands/Customers/CreateCustomerHandler.cs -> class CreateCustomerHandler
Repositories/Repository.cs                  -> abstract class Repository<T>
Repositories/ProductRepository.cs           -> class ProductRepository
Repositories/CustomerRepository.cs          -> class CustomerRepository
Repositories/OrderRepository.cs             -> class OrderRepository
```

### Ngoại lệ

- **Nested private/helper class** chỉ dùng nội bộ bởi class cha, không được tham chiếu từ bên ngoài, **được phép giữ lại trong file của class cha**. Ví dụ: một fake/stub `private sealed class TestExecutionContext : IExecutionContext` khai báo bên trong một test class để phục vụ riêng test đó.
- Điều kiện: type đó phải là `private` (không phải `internal`/`public`), và không có bất kỳ file nào khác reference tới nó bằng tên. Nếu type cần dùng lại ở nơi khác (dù chỉ trong cùng project), phải tách ra file riêng — không còn là "ngoại lệ".
- **Top-level statement Program.cs**: dòng `public partial class Program;` ở cuối file top-level-statement (dùng để `WebApplicationFactory<Program>` trong test) được coi là cùng một type với chương trình top-level statement sinh ra — không tách sang file khác.

## 2. No business logic trong architecture skeleton

- Khi tạo class/interface để hoàn thiện skeleton kiến trúc (base class, abstraction, DI extension...), **không tự chế thêm business rule, validation, hay side-effect** không có trong yêu cầu/nghiệp vụ hiện tại.
- Nếu một pattern (Specification, Domain Service...) chưa có use case thật trong code hiện tại, **không tạo class rỗng/giả chỉ để "trông đầy đủ"** — xem mục 4.

## 3. Empty architecture folder must contain `.gitkeep`

- Thư mục được tạo sẵn cho một pattern/kiến trúc (vd `Services/Specifications/`, `Repositories/` khi chưa có repository nào) nhưng **chưa có type thật nào cần đặt vào đó** thì phải chứa một file `.gitkeep` để Git track được thư mục và để người sau biết đây là chỗ dự trù, không phải bị bỏ quên.
- Khi thêm type thật đầu tiên vào thư mục đó, **phải xoá `.gitkeep`**.

## 4. Không tạo class dư chỉ để lấp folder trống

- Một interface/class chỉ được tạo khi có ít nhất một trong hai điều kiện:
  - Có type/consumer thật sự cần nó ngay bây giờ, HOẶC
  - Nó là abstraction bắt buộc của một pattern đã được yêu cầu rõ ràng (vd `IOutboxPublisher` cho Outbox Pattern) và việc thêm nó không đổi hành vi hiện tại.
- Nếu không chắc pattern có cần hay không, **để `.gitkeep` và ghi rõ lý do trong tài liệu review/PR**, đừng đoán và tạo class giả.

## 5. Dependency direction (Clean Architecture / DDD)

- **Domain** không được reference Application, Infrastructure, EF Core, MediatR, Kafka, Hangfire, MongoDB Driver hay bất kỳ framework nào. Domain chỉ chứa C# thuần + business rule.
- **Application** không được reference Infrastructure. Application có thể reference Domain và `BuildingBlocks.Contracts` (chỉ khi cần dùng chung contract, ví dụ mapping sang integration event) — không bắt buộc nếu Application chưa cần.
- **Infrastructure** implement các abstraction khai báo ở Domain/Application (repository interfaces, `IUnitOfWork`, `IOutboxPublisher`...). Infrastructure được reference Domain, Application, `BuildingBlocks.Contracts`, và mọi package hạ tầng (EF Core, Npgsql, KafkaFlow, MongoDB.Driver, Hangfire...).
- **Api/Worker** (entrypoint) được reference Application + Infrastructure để wiring DI; không chứa business logic.
- **Cross-service**: Sales không được reference trực tiếp implementation của Inventory (và ngược lại). Giao tiếp liên service chỉ qua **integration event** định nghĩa trong `Shared/BuildingBlocks.Contracts` + Kafka topic, không qua project reference trực tiếp giữa hai service.
- `Shared/BuildingBlocks.Contracts` không được reference bất kỳ project nghiệp vụ nào (`Sales.*`, `Inventory.*`, `AuditLog.*`). Nó chỉ chứa contract thuần (record/enum/interface), không phụ thuộc EF Core/Kafka/MediatR.
- Các rule này được enforce một phần bằng test tĩnh trong `tests/Sales.Architecture.Tests` (dùng NetArchTest) — khi thêm dependency mới, chạy lại test này trước khi merge.

## 6. Vị trí file theo vai trò

| Loại | Vị trí |
|---|---|
| Domain event nội bộ | `<Service>.Domain/Events/` (có thể chia theo aggregate: `Events/Orders/`, `Events/Products/`...) |
| Integration event (giao tiếp liên service) | `Shared/BuildingBlocks.Contracts/IntegrationEvents/{Common,Sales,Inventory}/` |
| Repository interface | `<Service>.Domain/Repositories/` (ownership theo Domain trong solution này) |
| Repository implementation | `<Service>.Infrastructure/Repositories/` |
| CQRS command/query + handler | `<Service>.Application/Commands/<Aggregate>/`, `Queries/<Aggregate>/` — mỗi record và mỗi handler một file riêng |
| FluentValidation validator | `<Service>.Application/Validators/<Aggregate>/` |
| MediatR pipeline behavior | `<Service>.Application/Services/Behaviors/` |
| DbContext | `<Service>.Infrastructure/Persistence/DbContexts/` |
| EF Core `IEntityTypeConfiguration<T>` | `<Service>.Infrastructure/Persistence/Configurations/` |
| EF Core Migrations | `<Service>.Infrastructure/Persistence/Migrations/` (sinh bởi `dotnet ef migrations add`, không sửa tay) |
| Outbox entity + abstraction | `<Service>.Infrastructure/Persistence/Outbox/` |
| Inbox entity | `<Service>.Infrastructure/Persistence/Inbox/` |
| Kafka producer/consumer/handler implementation | `<Service>.Infrastructure/Kafka/` |
| Hangfire job | `<Service>.Infrastructure/Hangfire/` |
| DI registration extension | `<Service>.<Layer>/DependencyInjection.cs` (`AddXApplication`, `AddXInfrastructure`, `AddXWorker`...) |
| Options / strongly-typed config | `<Service>.Infrastructure/Options/` |
| Enum dùng chung nhiều layer trong 1 service (không thuộc riêng 1 query/command) | `<Service>.Application/Common/Enums/` (ví dụ `PhoneMatch`, dùng ở cả query, Infrastructure read-service, và controller) |

## 7. Namespace

- Namespace của một project là **phẳng theo tên project** (ví dụ mọi file trong `Sales.Infrastructure` dùng `namespace Sales.Infrastructure;` dù nằm ở `Persistence/Outbox/` hay `Kafka/`). Không đổi namespace theo từng sub-folder — đây là convention đã có từ trước trong solution, giữ nguyên để tránh phải sửa hàng loạt `using`.
- IDE có thể gợi ý (hint) namespace nên khớp folder — bỏ qua gợi ý này, đây là chủ ý.

## 8. Trước khi coi một thay đổi cấu trúc là an toàn

- Build toàn bộ solution (`dotnet build Sales.sln`).
- Chạy toàn bộ test suite trong `tests/`.
- Nếu thay đổi động tới EF Core model (tách `OnModelCreating` thành `IEntityTypeConfiguration<T>`, đổi vị trí entity...): chạy thử `dotnet ef migrations add ZZ_Probe` rồi kiểm tra migration sinh ra — nếu `Up`/`Down` rỗng thì model không đổi, xoá migration probe đi. Nếu migration không rỗng, nghĩa là bạn vô tình đổi schema — phải điều tra trước khi tiếp tục.

## 9. CRUD/repository reuse

- Tránh lặp CRUD (`GetById`, `GetByIds`, `Add`, `Update`, `Delete`) trong repository cụ thể.
- Dùng `IRepository<T>` cho thao tác aggregate phổ biến; implementation chung nằm ở `Repository<T>`.
- Chỉ giữ repository cụ thể khi có query/thao tác đặc thù của aggregate, ví dụ `IProductRepository.GetBySkuAsync(...)` hoặc `IOrderRepository.GetWithLinesAsync(...)`.
- Không tạo interface rỗng hoặc interface chỉ wrap `IRepository<T>` nếu aggregate chưa có method riêng.
- Transaction/`SaveChangesAsync` thuộc `IUnitOfWork`, không đặt trong repository.

## 10. Reuse without over-engineering

- Ưu tiên MediatR pipeline behavior cho cross-cutting concern lặp ở handler như validation, logging, tracing, transaction.
- Ưu tiên extension method nhỏ cho rule validation, DI/configuration setup, mapping hoặc helper lặp rõ ràng.
- Ưu tiên Options pattern cho binding configuration lặp hoặc có nhiều consumer; không tạo options class cho một giá trị dùng một lần.
- Ưu tiên abstraction `Result`/`PagedResult`/pagination helper khi response/query paging lặp ở nhiều read service.
- Không đưa business logic vào base class/generic service; base/helper chỉ chứa flow kỹ thuật dùng chung.

## 11. Expression-bodied member vs block body

- Chỉ dùng expression-bodied member (`=>`) cho method/property/constructor **rất ngắn, xử lý đơn giản, dễ đọc ngay** (1 phép chiếu/1 lời gọi/1 điều kiện đơn giản) — ví dụ getter tính toán 1 dòng, delegate thẳng sang 1 method khác, guard-clause 1 ternary.
- Đổi sang block body (`{ }`) khi method rơi vào **bất kỳ** điều kiện nào sau (không cần đủ cả — 1 điều kiện là đủ):
  - Nhiều tham số (constructor/factory ghép nhiều field).
  - Nhiều bước logic nối tiếp nhau (nhiều `.Select()`/`.Where()` lồng nhau, nhiều field map).
  - Có conditional (ternary/`switch`) **kết hợp** với object creation hoặc serialization — không tính ternary đơn lẻ kiểu guard-clause (`=> string.IsNullOrWhiteSpace(x) ? throw ... : x.Trim();`).
  - Tạo object (`new(...)`/object initializer) với nhiều property hoặc lồng thêm 1 object creation khác bên trong (map DTO có collection con, event envelope nhiều field...).
- Khi đổi sang block body, **tách biến trung gian có tên rõ nghĩa** cho từng bước (ví dụ `eventType`, `payload`, `lines`) thay vì giữ nguyên toàn bộ biểu thức lồng nhau trong `return` — ưu tiên đọc hiểu hơn viết gọn.
- Không đổi behavior/signature/access modifier/generic type/null handling khi refactor thuần style — chỉ đổi cách trình bày.
- Sau khi refactor style, build lại toàn solution và chạy lại test suite trước khi coi là xong (mục 8 vẫn áp dụng).
- Ví dụ đã áp dụng trong solution: `EventEnvelopeFactory.Create<T>` (Sales & Inventory — 6 tham số + ternary + serialization + object creation), `DtoMapping.ToDto(this Order order)` (10 tham số + object creation lồng cho `Lines`), `ReservationLine.Create` (object initializer 5 property). Ngược lại `Repository<T>.GetByIdAsync`, `Paging.Normalize`, `CustomerReadService.GetAsync` giữ nguyên `=>` vì chỉ 1 pipeline/phép chiếu đơn giản, không rơi vào điều kiện nào ở trên.

## 12. Strongly typed model thay vì tuple cho business data

- Business data không được biểu diễn bằng C# tuple khi nó băng qua ranh giới public/internal API (tham số method, kiểu trả về, property, interface).

  ❌ Tránh:
  ```csharp
  IEnumerable<(ProductSnapshot Product, int Quantity, decimal DiscountPercent)>
  ```

  ✔ Dùng:
  ```csharp
  IEnumerable<OrderLineItem>
  ```

- Tạo một Model/DTO/Record/Value Object riêng đại diện cho khái niệm nghiệp vụ đó, đặt đúng layer theo mục 5/6 (Domain nếu là khái niệm nghiệp vụ, Application nếu chỉ phục vụ use case, `BuildingBlocks.Contracts` nếu chia sẻ giữa API và client). Infrastructure không tự định nghĩa business DTO trừ khi nó thuần thuộc hạ tầng (vd tuple `(string Topic, EventEnvelope Envelope)` trong `DomainEventMapper.Map` — routing nội bộ Kafka, không phải domain concept, được phép giữ nguyên).
- Đặt tên theo nghĩa nghiệp vụ (`OrderLineItem`, `InventoryReservation`) — không dùng tên chung chung (`TupleModel`, `TempData`, `Data1`).
- Ưu tiên type bất biến: positional record (`public sealed record OrderLineItem(ProductSnapshot Product, int Quantity, decimal DiscountPercent);`) hoặc class với `init`-only property.
- Tuple vẫn được phép cho: biến local ngắn hạn, deconstruction, pattern `TryParse`, hoặc thuật toán helper mà kết quả không đại diện cho một business concept băng qua API boundary (vd `Paging.Normalize` trả `(int Page, int PageSize)` — thuần clamp giá trị, giữ nguyên).
- Ví dụ đã áp dụng trong solution: `OrderLineItem` (`Sales.Domain/ValueObjects/OrderLineItem.cs`) thay cho tuple `(ProductSnapshot Product, int Quantity, decimal DiscountPercent)` từng dùng ở `Order.Create`, `Order.ReplaceLines`, `Order.SetLines`, và `OrderCommandSupport.Materialize`.

## 13. XML documentation comment cho mọi member public/internal/protected/interface

- Mọi `class`/`interface`/`record`/`struct` và mọi member `public`, `internal`, `protected` của nó (method, property, constructor, cả member trên `interface`) phải có XML documentation comment (`///`) đầy đủ theo convention chuẩn của Microsoft — không chỉ `<summary>` sơ sài.
- Bắt buộc có `<summary>` mô tả member đó **làm gì** (không lặp lại tên method bằng lời), cộng thêm các tag sau **khi áp dụng được**:
  - `<typeparam name="...">` cho mỗi type parameter generic.
  - `<param name="...">` cho mỗi tham số method/constructor.
  - `<returns>` cho method có giá trị trả về khác `void`/`Task` (không cần cho `Task` non-generic thuần async void-like, nhưng nên có nếu `Task<T>`).
  - `<exception cref="...">` cho mỗi exception method chủ đích ném ra (không cần liệt kê exception không kiểm soát được từ dependency).
- Việc thêm/sửa comment này **không được đổi logic, tên, tham số, access modifier, hay format code** ngoài phần comment — đây thuần là tài liệu, không phải refactor.
- `private` member không bắt buộc XML doc (thường tự rõ nghĩa qua tên + ngữ cảnh nội bộ class), nhưng vẫn có thể thêm nếu logic không hiển nhiên.
- Ví dụ format chuẩn:

  ```csharp
  /// <summary>
  /// Appends a new domain event to the event stream for the specified aggregate.
  /// </summary>
  /// <typeparam name="T">
  /// The type of the domain event.
  /// </typeparam>
  /// <param name="aggregateId">
  /// The unique identifier of the aggregate.
  /// </param>
  /// <param name="version">
  /// The expected aggregate version.
  /// </param>
  /// <param name="data">
  /// The event payload to persist.
  /// </param>
  /// <param name="actor">
  /// The user or system responsible for the operation.
  /// </param>
  /// <param name="correlationId">
  /// The correlation identifier used to trace the request across services.
  /// </param>
  /// <param name="causationId">
  /// The identifier of the command or event that caused this operation.
  /// </param>
  /// <returns>
  /// A task representing the asynchronous operation.
  /// </returns>
  ```

- Áp dụng cho toàn bộ solution (Sales, Inventory, AuditLog, `BuildingBlocks.*`), trừ `<Service>.Infrastructure/Persistence/Migrations/` (sinh tự động bởi `dotnet ef migrations add`, không sửa tay — xem mục 6) và `tests/` (test method không phải public API surface, không cần XML doc theo convention chuẩn).

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

- **Domain** không được reference Application, Infrastructure, EF Core, MediatR, Kafka, Hangfire, MongoDB Driver hay bất kỳ framework nào. Domain chỉ chứa C# thuần + business rule. `Sales.Domain`/`Inventory.Domain` được phép reference `Shared/BuildingBlocks.Domain` (base type framework-independent dùng chung — `AggregateRoot<TId>`, `Entity<TId>`, `IDomainEvent`/`DomainEvent`, `DomainException`), không được reference `BuildingBlocks.Application`/`BuildingBlocks.Infrastructure`/`BuildingBlocks.Web`.
- **Application** không được reference Infrastructure. Application có thể reference Domain, `BuildingBlocks.Contracts` (chỉ khi cần dùng chung contract, ví dụ mapping sang integration event — không bắt buộc nếu Application chưa cần), và `Shared/BuildingBlocks.Application` (pipeline behavior, `IUnitOfWork`, pagination dùng chung — dùng bởi cả `Sales.Application` và `Inventory.Application`, cả hai đều dùng CQRS/MediatR). Application không được reference EF Core/Npgsql hay bất kỳ package hạ tầng nào, kể cả để bắt exception persistence (vd `DbUpdateConcurrencyException`) — việc dịch exception đó sang HTTP response thuộc về Api (xem `Sales.Api/Middleware/ExceptionHandlingMiddleware` và `Inventory.Api/Middleware/ExceptionHandlingMiddleware`).
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
| MediatR pipeline behavior | `Shared/BuildingBlocks.Application/Behaviors/` (shared behaviors: `ErrorLoggingBehavior`, `LoggingBehavior`, `ValidationBehavior`); service-specific behavior (nếu có) vẫn ở `<Service>.Application/Services/Behaviors/` |
| Aggregate/entity base type, domain event, domain exception dùng chung | `Shared/BuildingBlocks.Domain/Abstractions/` (`AggregateRoot<TId>`, `Entity<TId>`, `IDomainEvent`/`DomainEvent`), `Shared/BuildingBlocks.Domain/Exceptions/` (`DomainException`) |
| `IUnitOfWork`, pagination (`PagedResult<T>`/`Paging`) dùng chung | `Shared/BuildingBlocks.Application/Persistence/`, `Shared/BuildingBlocks.Application/Pagination/` |
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

## 13. XML Documentation

- XML documentation chỉ áp dụng cho API `public` và `protected` cần giải thích intent hoặc business meaning. Không document `private`/`internal` member trừ khi logic đặc biệt phức tạp và không thể tự hiểu từ code.
- Mọi member đã document phải có `<summary>` ngắn gọn, một đến hai câu ngắn, tập trung vào mục đích nghiệp vụ hoặc hành vi không hiển nhiên. Không lặp lại tên class/method/property và không mô tả chi tiết implementation.
- Dùng `<param>` chỉ khi parameter cần làm rõ business meaning. Viết một dòng, ví dụ `/// <param name="version">Expected aggregate version.</param>`.
- Không thêm `<typeparam>` nếu chỉ lặp lại generic type name, ví dụ "The type of ...". Chỉ giữ khi generic parameter có semantic đặc biệt không nhìn ra từ chữ ký.
- Chỉ dùng `<returns>` khi giá trị trả về có ý nghĩa nghiệp vụ hoặc hành vi đặc biệt cần giải thích, ví dụ `bool`, identifier, domain object, DTO, result object. Không viết boilerplate như `A task representing the asynchronous operation.`
- Chỉ dùng `<exception>` khi exception là một phần có chủ ý của public API contract. Không liệt kê framework/infrastructure exception bị rò từ dependency.
- Không đưa implementation detail vào XML documentation: EF Core, LINQ, Kafka, SQL, repository usage, internal algorithm, hoặc framework-specific behavior chỉ nên xuất hiện trong code/comment nội bộ khi thật sự cần.
- Việc thêm/sửa XML documentation không được đổi logic, method signature, public API, namespace, dependency, hoặc behavior hiện tại.
- Áp dụng cho toàn bộ solution (Sales, Inventory, AuditLog, `BuildingBlocks.*`), trừ `<Service>.Infrastructure/Persistence/Migrations/` (sinh tự động bởi `dotnet ef migrations add`, không sửa tay — xem mục 6) và `tests/` (test method không phải public API surface, không cần XML doc theo convention chuẩn).

## 14. Program.cs

- `Program.cs` is only the application composition root.
- `Program.cs` must not contain business logic.
- `Program.cs` must not contain long-form infrastructure configuration.
- `Program.cs` should only call clear extension methods for service registration, middleware configuration, and startup tasks.
- Do not declare request-handling lambdas in `Program.cs`.

## 15. API

- Do not use Minimal API for service APIs.
- All HTTP APIs must use ASP.NET Core Controllers.
- Controllers only receive requests, rely on ASP.NET Core validation/model binding, call the Application layer, and return `IActionResult`.
- Controllers must not contain business logic.
- Controllers must not access `DbContext` or repositories directly for business use cases.
- Controllers must communicate with the Application layer.

## 16. Swagger / OpenAPI

- Every API service must configure Swagger/OpenAPI.
- Do not configure Swagger directly in `Program.cs`.
- Swagger must be packaged behind extension methods.
- Controllers and actions must have XML documentation (`<summary>`) so Swagger can display useful descriptions.
- Swagger must support JWT Bearer Authentication and expose the Authorize button.
- Do not duplicate Swagger configuration across microservices; shared pieces belong in `BuildingBlocks`.
- Swagger UI should only be enabled in Development unless a service has an explicit documented convention.

## 17. Service Registration

- Each concern must have its own extension method.
- Do not create god extensions that hide unrelated responsibilities.
- One extension should have one clear responsibility.
- Shared infrastructure registration used by multiple services belongs in `BuildingBlocks`.

## 18. Middleware

- Middleware configuration must be grouped behind extension methods.
- Do not configure middleware directly in `Program.cs`.
- Keep exception handling, request logging, observability, authentication, authorization, and API documentation configuration separated by concern.

## 19. Startup Tasks

- Do not run database migrations, start Kafka, or register application lifetime callbacks directly in `Program.cs`.
- Put startup work behind explicit startup task extensions or hosted services.
- Startup tasks must preserve existing execution behavior and DI lifetimes.

## 20. Shared Infrastructure

- Components reused by multiple services, such as authentication, observability, logging, problem details, and Swagger configuration, belong in `BuildingBlocks`.
- Do not duplicate infrastructure configuration across services.
- Infrastructure projects must not depend on API projects.

## 21. Clean Architecture

- Follow the Dependency Rule.
- API depends on Application and Infrastructure only at the composition root.
- Application must not depend on Infrastructure or API.
- Domain must not depend on Application, Infrastructure, or API.
- Infrastructure must not depend on API.
- Controllers must not bypass the Application layer for business use cases.

## 22. Application Abstraction Reuse and Audit-Before-Create Rules

- Trước khi tạo folder, class, interface, command, query, handler, DTO, validator, mapper, behavior, repository, service, decorator hoặc wrapper mới, phải tìm trong toàn solution theo thứ tự: `BuildingBlocks.Application`, shared project phù hợp khác, Application/Domain của service hiện tại, rồi implementation hiện có trong Infrastructure.
- Ưu tiên dùng trực tiếp abstraction shared đã có trong `BuildingBlocks.Application`, ví dụ `IUnitOfWork`, CQRS marker, pagination model, clock, exception classifier và MediatR pipeline behaviors. Không tạo alias/wrapper theo tên service nếu không bổ sung hành vi thật.
- `Inventory.Application/Abstractions` hoặc folder tương tự ở service khác chỉ chứa contract đặc thù use case của service đó. Không đặt abstraction dùng chung hoặc contract domain-agnostic trùng với BuildingBlocks vào đây.
- Không tạo folder theo template như `Abstractions`, `Common`, `Interfaces`, `Services`, `Mappings`, `Validators`, `Behaviors`, `Models` nếu chưa có file thật cần đặt vào đó. Không giữ folder rỗng để chuẩn bị cho tương lai.
- Không đưa abstraction domain-specific vào BuildingBlocks quá sớm. Chỉ chuyển vào shared project khi có ít nhất hai service dùng thật, contract ổn định, domain-agnostic, và không làm service coupling với nhau.
- Application không được phụ thuộc Infrastructure, API hoặc Application của service khác. Implementation của Application abstractions đặt ở Infrastructure.
- Command/query/handler chỉ được tạo cho use case thực tế đang tồn tại hoặc được yêu cầu rõ. Không tạo đủ bộ command/query giả định chỉ vì kiến trúc mẫu liệt kê.
- AI coding agent hoặc người refactor phải báo rõ thành phần tái sử dụng từ BuildingBlocks, thành phần tạo mới, lý do tạo mới, và thành phần xóa vì trùng/không dùng.

## 23. General

- Do not duplicate code.
- Do not use generic static helper classes as a dumping ground.
- Do not create abstractions without a clear need.
- Keep naming consistent across the solution.
- Do not use `#region`.
- Each class must have one clear responsibility.

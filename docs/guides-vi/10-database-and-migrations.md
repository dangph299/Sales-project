# 10. Thiết kế database & migration

## Mục đích

Giải thích domain model đi tới PostgreSQL bằng cách nào, các quy ước mà phần mapping tuân theo, và hai quyết định thiết kế dễ làm bạn bất ngờ nhất: filtered unique index và mã nghiệp vụ do database cấp phát.

## Mỗi context một database

| Database | Chủ sở hữu |
|---|---|
| `sales` | Sales.Api |
| `inventory` | Inventory.Api |
| `hangfire` | Sales.Api (nơi lưu job) |
| `audit` (MongoDB) | AuditLog.Worker |

Được tạo bởi `docker/seed/postgres-init.sql`. Không có truy vấn liên database và không có khóa ngoại liên database. Inventory lưu các id của Sales dưới dạng `Guid` không mang ý nghĩa.

## Mapping nằm trong configuration

Các kiểu domain không mang attribute nào của EF. Mỗi entity có một `IEntityTypeConfiguration<T>` được áp bởi `ApplyConfigurationsFromAssembly`:

```csharp
public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> entity)
    {
        var money = new ValueConverter<Money, decimal>(x => x.Amount, x => Money.Vnd(x));

        entity.ToTable("product_variants");
        entity.HasKey(x => x.Id);
        entity.HasQueryFilter(x => !x.IsDelete);
        entity.HasIndex(x => x.Sku).IsUnique().HasFilter("NOT \"IsDelete\"");
        entity.Property(x => x.Price).HasConversion(money).HasColumnType("numeric(18,0)");
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Version).IsConcurrencyToken();
    }
}
```

Đây là lý do domain vẫn không dính framework: `ProductVariant` không biết là nó được lưu trữ.

Các quy ước áp dụng ở mọi nơi: `HasMaxLength` cho mọi chuỗi, enum lưu dạng string, `Money` đi qua converter, `Version` làm concurrency token, các property tính toán được `Ignore`, collection private được truy cập qua field.

## Enum lưu dạng string

```csharp
entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
```

Cột chứa `'PendingInventory'`, không phải `1`. Nhờ đó sắp xếp lại thứ tự các thành viên enum là an toàn; **nhưng đổi tên một thành viên là một thay đổi dữ liệu phá vỡ tương thích**. Đánh đổi này được chọn có chủ đích: đổi tên hiếm hơn sắp xếp lại, và một cột đọc được rất đáng giá khi debug trong `psql`.

## Soft delete và cái bẫy filtered index

Bốn entity hỗ trợ soft delete: `Category`, `Product`, `ProductVariant`, `Customer`.

```csharp
entity.HasQueryFilter(x => !x.IsDelete);
```

Giờ mọi truy vấn đều âm thầm loại bỏ dòng đã xóa — và điều đó tạo ra một lỗi tinh vi với unique index. Hãy xét một unique index thông thường trên `ProductCode`:

1. `PRD001` được tạo, rồi bị xóa.
2. Dòng dữ liệu vẫn còn đó và vẫn đang chiếm `PRD001`.
3. Tạo lại `PRD001` sẽ fail vì unique violation.
4. Bạn đi tìm sản phẩm gây xung đột — và query filter giấu nó đi mất.

Một lỗi 409 chỉ về một bản ghi mà chẳng gì hiển thị được. Cách sửa, migration `UniqueIndexesExcludeSoftDeleted`:

```csharp
entity.HasIndex(x => x.ProductCode).IsUnique().HasFilter("NOT \"IsDelete\"");
```

**Mọi unique index trên bảng có soft delete đều phải mang filter đó.** Chú ý định danh có dấu nháy kép — EF giữ tên cột kiểu PascalCase, nên các đoạn SQL thô phải đặt trong nháy.

Lý lẽ tương tự áp dụng cho `(ProductId, ColorId, SizeId)` trên variant: không có filter thì việc xóa một variant sẽ vĩnh viễn chặn việc thêm lại đúng cặp màu/size đó.

## Mã nghiệp vụ sinh từ sequence

`CUS001`, `PRD007`, `CAT003` được PostgreSQL cấp phát, không phải ứng dụng:

```csharp
var sequenceNumber = await db.Database
    .SqlQuery<long>($"SELECT nextval({codeSequence.SequenceName}::regclass) AS \"Value\"")
    .SingleAsync(cancellationToken);
return codeSequence.Prefix + sequenceNumber.ToString("D3", CultureInfo.InvariantCulture);
```

Vì sao không dùng `MAX(code) + 1`? Vì hai lệnh tạo đồng thời sẽ đọc ra cùng một giá trị lớn nhất. `nextval` là nguyên tử và không bao giờ trả về cùng một giá trị hai lần, nên bao nhiêu instance API cũng nhận được mã khác nhau mà không cần khóa.

Các hệ quả đáng nói thẳng ra:

- mã là duy nhất và tăng dần, **nhưng không liền mạch** — sequence không rollback, nên số bị tiêu tốn bởi một lệnh tạo thất bại sẽ bị bỏ qua;
- việc cấp phát diễn ra *sau* khi validate, nên một request bị từ chối không đốt mất số nào;
- prefix và tên sequence được khai báo một lần trong `EntityCodeSequence`, rồi được model, migration và các bộ sinh mã đọc lại.

Các sequence được khai báo trên model nhưng có rào chắn:

```csharp
if (Database.IsNpgsql())
    foreach (var codeSequence in EntityCodeSequence.All)
        builder.HasSequence<long>(codeSequence.SequenceName).StartsAt(1).IncrementsBy(1);
```

Các test chạy trên SQLite dựng schema từ chính model này và nếu không có rào chắn thì sẽ fail ngay. Bất kỳ cấu trúc đặc thù provider nào cũng cần rào chắn tương tự.

## Index

Mọi cột dùng để lọc và sắp xếp trên đường tìm kiếm đều có index. Hai kỹ thuật đáng biết:

**Tìm kiếm văn bản bằng trigram.** `EF.Functions.ILike(x.Name, $"%{value}%")` không dùng được B-tree, nên các cột `Name` được đánh index GIN với `gin_trgm_ops` (bật bằng `HasPostgresExtension("pg_trgm")`).

**Số điện thoại đảo ngược.** Tìm theo hậu tố (`LIKE '%456'`) cũng không dùng được index. Vì vậy `Customer` lưu thêm `ReversedPhone`, và tìm theo hậu tố trở thành tìm theo tiền tố trên cột đảo ngược — việc *có* dùng được index.

## Snapshot thay cho khóa ngoại

`orders.CustomerId` không có khóa ngoại. `order_lines` cũng không có khóa ngoại tới `product_variants`. Đó là chủ ý: đơn hàng lưu một **snapshot** của khách hàng và sản phẩm tại thời điểm đặt, và nó phải sống sót khi catalog thay đổi hoặc khi khách hàng bị xóa.

Ở những nơi thực sự có quan hệ chứa đựng, khóa ngoại vẫn có với hành vi cascade đúng: `order_lines` → `orders` cascade, `product_variants` → `products` cascade, còn `products` → `categories` và variant → color/size dùng `Restrict` để dữ liệu tham chiếu không bị xóa ngay dưới chân các dòng đang dùng.

## Migration

```bash
dotnet ef migrations add UniqueIndexesExcludeSoftDeleted \
  --project src/Services/Sales/Sales.Infrastructure \
  --startup-project src/Services/Sales/Sales.Api \
  --output-dir Persistence/Migrations
```

Quy tắc:

- đặt tên theo thay đổi, kiểu PascalCase, không thêm tiền tố ngày (EF tự thêm timestamp);
- commit migration, file `.Designer.cs` của nó **và** model snapshot đã cập nhật cùng lúc — thiếu snapshot sẽ khiến migration kế tiếp sai;
- không bao giờ sửa hay xóa một migration đã được áp dụng; hãy tạo migration mới;
- khai báo trên model rồi scaffold thay vì viết tay, trừ trường hợp backfill dữ liệu và seed sequence;
- xem lại SQL sinh ra — đôi khi scaffold drop rồi tạo lại một index một cách không cần thiết.

Sales có 12 migration, Inventory có 7. Cả hai đều được áp dụng lúc khởi động: Sales bên trong `IdentitySeeder.SeedIdentityAsync`, Inventory trong `RunStartupTasksAsync`. Không bao giờ dùng `EnsureCreated()` — nó bỏ qua hoàn toàn migration.

## Dữ liệu tham chiếu

Được seed bằng `HasData` với các GUID cố định khai báo trong `Persistence/SeedData/ReferenceData/`: 5 màu, 8 size, và một category `Uncategorized`.

Các GUID đó xuất hiện đúng một chỗ duy nhất trong solution. Client lấy chúng qua `GET /api/common/colors|sizes` và `GET /api/categories` rồi submit lại `id` nhận được — đó là lý do không có GUID seed nào bị hardcode trong ứng dụng Angular.

## Lỗi thường gặp

| Sai lầm | Hậu quả |
|---|---|
| Unique index không có filter soft-delete | mã vĩnh viễn không dùng lại được, xung đột vô hình |
| Sửa một migration đã áp dụng | môi trường kế tiếp bị lệch |
| Quên model snapshot | migration tiếp theo được sinh dựa trên model cũ |
| Dùng `EnsureCreated()` | không có lịch sử migration nào cả |
| Cấu trúc đặc thù provider mà không có `IsNpgsql()` | test chạy trên SQLite không dựng được schema |
| Đổi tên một thành viên enum | các giá trị chuỗi mồ côi trong dữ liệu hiện có |
| Cột lọc mà không có index | quét tuần tự trong mọi lần tìm kiếm |

## Liên quan

- [../tech/database-conventions.md](../tech/database-conventions.md)
- [../tech/database-schema.md](../tech/database-schema.md)
- [../project/backend/migration-rule.md](../project/backend/migration-rule.md)

# 6. DDD trong dự án này

## Mục đích

Cho thấy các tactical pattern của DDD thực sự được dùng thế nào ở đây — bao gồm cả những chỗ dự án lệch khỏi sách vở, và vì sao.

## Aggregate

Aggregate là một ranh giới nhất quán: một cụm object phải được lưu cùng nhau và các quy tắc của chúng được áp cùng nhau.

| Aggregate root | Con | Bảo vệ điều gì |
|---|---|---|
| `Order` | `OrderLine` | máy trạng thái, tính duy nhất của dòng hàng, tổng tiền |
| `Product` | `ProductVariant` | máy trạng thái, tính duy nhất của cặp màu/size, sinh SKU |
| `Customer` | — | chuẩn hóa số điện thoại, máy trạng thái |
| `Category` | — | máy trạng thái, tự làm cha chính mình |
| `Reservation` | `ReservationLine` | máy trạng thái, chống dữ liệu cũ |

Hình dạng luôn nhất quán:

```csharp
public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderLine> _lines = [];

    private Order() { }                                    // EF materialization
    private Order(Guid id, CustomerSnapshot customer) { … } // real construction

    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Order Create(CustomerSnapshot customer, IEnumerable<OrderLineItem> lines) { … }
}
```

Mọi property đều `private set`. Collection là một `List<T>` private được phơi ra ở dạng chỉ đọc. Không có cách nào đưa một `Order` vào trạng thái không hợp lệ từ bên ngoài.

### Hành vi, không phải setter

```csharp
public void RequestConfirmation()
{
    EnsureDraft();
    Status = OrderStatus.PendingInventory;
    Touch();
    Raise(new OrderConfirmationRequestedDomainEvent(Id,
        _lines.Select(x => new OrderLineReservation(x.ProductVariantId, x.Sku, x.Quantity)).ToArray()));
}
```

Kiểm tra điều kiện → thay đổi → `Touch()` → phát sự kiện. Thứ tự đó quan trọng: event mang theo trạng thái *sau* khi thay đổi, và `Touch()` tăng version mà event báo cáo.

### Entity con bị đóng kín

`OrderLine.Create` và `OrderLine.ReplaceWith` là `internal`, nên chỉ `Order` gọi được. Bạn không thể lấy một `OrderLine` ra rồi đổi số lượng của nó.

`Order.SetLines` hiện thực ngữ nghĩa thay-thế-tập-hợp: xóa các dòng không có trong tập mới, cập nhật dòng khớp, thêm dòng mới — vẫn giữ nguyên định danh của những dòng sống sót qua lần sửa.

### Aggregate tham chiếu nhau bằng id

Một `Order` giữ `CustomerId`, không giữ `Customer`. Khi cần dữ liệu của khách hàng, nó chụp một **snapshot**:

```csharp
var customerSnapshot = CustomerSnapshot.Create(customer.Id, customer.Name, customer.Phone);
var order = Order.Create(customerSnapshot, orderLineItems);
```

Đổi tên một khách hàng không bao giờ viết lại các đơn hàng cũ của họ. Điều tương tự áp dụng cho `ProductSnapshot` trên dòng đơn hàng — giá, SKU, tên, màu và size đều được đóng băng tại thời điểm tạo dòng.

## Value object

Bất biến, so sánh theo giá trị, kiểm tra trong factory.

```csharp
public readonly record struct Money
{
    public decimal Amount { get; }
    private Money(decimal amount) => Amount = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);

    public static Money Vnd(decimal amount)
    {
        if (amount < 0) throw new DomainException("Money cannot be negative.");
        return new Money(amount);
    }

    public static Money operator +(Money left, Money right) => Vnd(left.Amount + right.Amount);
}
```

Không thể quên làm tròn và ràng buộc không âm, bởi vì không còn cách nào khác để tạo ra nó. Chú ý là không có toán tử `-`: trong domain không có chỗ nào trừ tiền, nên nó không được cung cấp.

## Domain event

Sự kiện đã xảy ra, thì quá khứ, chỉ chứa dữ liệu nghiệp vụ:

```csharp
public sealed record OrderConfirmedDomainEvent(Guid OrderId) : DomainEvent;
```

Chúng không biết gì về Kafka. `AggregateRoot` đệm chúng lại; `SalesDbContext.SaveChangesAsync` rút bộ đệm, map những gì cần rời khỏi tiến trình, và ghi các dòng outbox. Xem [07-domain-events-and-outbox.md](07-domain-events-and-outbox.md).

## Specification

Các quy tắc truy vấn tái sử dụng được, biểu diễn dưới dạng expression để EF dịch được:

```csharp
public sealed class ActiveCustomerSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression() =>
        x => x.Status == ECustomerStatus.Normal && !x.IsDelete;
}
```

`Specification<T>.And` ghép hai specification bằng cách viết lại tham số của chúng, nhờ đó `OrderReadService` có thể dựng bộ lọc từ bất kỳ tiêu chí nào mà bên gọi cung cấp.

## Repository

Chỉ phía command, chỉ làm việc với aggregate:

```csharp
public interface IRepository<T> where T : AggregateRoot<Guid>
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task AddAsync(T aggregate, CancellationToken ct = default);
    void Update(T aggregate);
    void Delete(T aggregate);
}
```

Không `IQueryable`, không tham số `Expression`, không DTO. Một architecture test khẳng định điều đó. Phía đọc đi qua `I*ReadService` thay thế.

## Những chỗ dự án này lệch chuẩn

Đáng biết, vì chúng trông như lỗi cho tới khi bạn biết lý do.

**`InventoryItem` không phải aggregate root.** Nó là `IEntity<Guid>` khóa theo `ProductId` với `Version` riêng. Nó không có domain event và không có entity con, nên `AggregateRoot` chỉ thêm nghi thức thừa. Vì vậy Inventory khai báo các interface repository độc lập thay vì dùng `IRepository<T>`.

**`ProductVariant` giữ `Version` và `Touch()` riêng.** Nó là entity con, nhưng được map với concurrency token riêng vì variant được sửa độc lập thông qua product. Đừng "sửa" điều này bằng cách nâng nó lên thành root — product mới là ranh giới nhất quán.

**Domain event không được dispatch trong tiến trình.** Không có handler `INotification` nào cho `OrderConfirmedDomainEvent`. Bên tiêu thụ duy nhất là `DomainEventMapper`, và chỉ với hai event buộc phải rời khỏi tiến trình. Một domain event không có mapping thì đơn giản là không được publish — cố ý, không phải sơ suất.

**`Reservation` bỏ qua `Version` và `UpdatedAt` mà nó kế thừa.** `ReservationConfiguration` ignore chúng một cách tường minh; aggregate dùng `LastOrderVersion` (version của đơn hàng phía *Sales*) để chống dữ liệu cũ, thay vì version của chính nó.

**Aggregate tự đóng dấu thời gian bằng `DateTimeOffset.UtcNow`.** Ở mọi nơi khác `IClock` là bắt buộc. Domain không có phụ thuộc nào nên không thể nhận một clock — đây là đánh đổi được chấp nhận và các mốc thời gian này không mang tính sống còn về nghiệp vụ.

## Lỗi thường gặp

| Sai lầm | Hậu quả |
|---|---|
| Đặt setter public trên aggregate | invariant trở thành tùy chọn |
| Load một aggregate khác bên trong aggregate | hai ranh giới nhất quán trong một transaction |
| Giữ navigation tới một root khác | cascade load và ghi nhầm ngoài ý muốn |
| Đặt quy tắc trong handler thay vì trong aggregate | domain test không thấy, lần sau lại lặp lại |
| Quên `Touch()` | ETag không đổi, nên một client với dữ liệu cũ vẫn ghi thành công |
| Phát sự kiện trước khi thay đổi trạng thái | event mô tả trạng thái chưa từng được lưu |
| Phơi ra `List<T>` phía sau | code bên ngoài âm thầm thay đổi aggregate |

## Liên quan

- [../guides/DDD-structure-guide.md](DDD-structure-guide.md) — deep dive (tiếng Việt)
- [../tech/business/](../tech/business/) — quy tắc thực tế theo từng aggregate
- [../project/backend/ddd-rule.md](../project/backend/ddd-rule.md), [aggregate-rule.md](../project/backend/aggregate-rule.md)

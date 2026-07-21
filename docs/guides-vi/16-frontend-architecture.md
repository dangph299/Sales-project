# 16. Kiến trúc frontend

## Mục đích

Client Angular ở `src/Web/Sales.Web` tồn tại để thao tác tay với các API. Nó nhỏ, nhưng chứa vài quyết định đáng hiểu — đặc biệt là về việc kiến thức của backend được phép nằm ở đâu.

## Hình dạng

```
core/      transport, session, config, realtime, navigation   (singletons)
layout/    header, sidebar, status bar, breadcrumbs           (the shell)
shared/    presentation-only components, pipes, utilities
features/  one folder per business area, lazy-loaded
```

Phụ thuộc chảy theo hướng `features → shared → core`, không bao giờ ngược lại. `core/` mà import từ `features/` sẽ khiến tầng transport phụ thuộc vào một màn hình.

Mọi feature đều lazy-load:

```typescript
{ path: 'products',
  loadChildren: () => import('./features/products/products.routes').then(r => r.productsRoutes) }
```

Toàn bộ dùng standalone component — không có một `NgModule` nào trong dự án.

## Chỉ một chỗ chạm vào HttpClient

```
component  ->  <Feature>ApiService  ->  ApiClientService  ->  HttpClient
```

`ApiClientService` là class duy nhất import `HttpClient`, `HttpHeaders` hay `HttpParams`. Nó sở hữu việc dựng URL, header xác thực, dựng query parameter, đọc response và dịch lỗi.

Các service theo feature thì rất mỏng:

```typescript
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

Lợi ích: thêm một header cho request, đổi cơ chế xác thực, hay xử lý một status code mới đều chỉ là sửa một file.

## Đọc envelope

`ApiResponseReader` là phần được xây dựng cẩn thận nhất của client, vì mọi response từ backend đều được bọc:

```typescript
static readSuccess<T>(response: HttpResponse<string>): ApiClientResult<T>
static readFailure<T>(error: unknown): ApiClientResult<T>
static ensureSuccess<T>(result: ApiClientResult<T>, requireData: boolean)
```

Nó xử lý 204 không có body, body rỗng, JSON hỏng, envelope có `success: false` trên một response 200, và hình dạng `ApiErrorResponse` cho 4xx/5xx — chuẩn hóa tất cả về một `ApiClientResult<T>`. Response được đọc dưới dạng `text` rồi parse thủ công để một body không phải JSON (trang lỗi HTML từ proxy) cho ra thông báo hợp lý thay vì lỗi parse của Angular.

Lỗi trở thành `ApiClientError`, mang theo `status` và kết quả đã parse gồm `errorCode`, `validationErrors`, `traceId` và `correlationId`. Component chuyển đổi bằng:

```typescript
this.errorMessage.set(describeApiError(error));
```

`api-client-result.spec.ts` phủ mọi nhánh. Đó là chỗ để thêm case khi backend có thêm hình dạng lỗi mới.

## State: chỉ dùng signal

Không NgRx, không kho `BehaviorSubject`. State nằm ở phạm vi hẹp nhất mà vẫn dùng được:

| Phạm vi | Nơi ở |
|---|---|
| một trang | chính component của trang |
| một feature, nhiều trang | một service của feature cung cấp ở root |
| toàn ứng dụng | một service trong `core/` |

Mọi trang có tải dữ liệu đều theo cùng hình dạng:

```typescript
readonly rows = signal<ProductResponse[]>([]);
readonly loading = signal(false);
readonly errorMessage = signal('');
```

và render chúng qua `PageStateComponent`. Sự nhất quán ở đây đáng giá hơn sự thông minh.

## Không có GUID của backend trong client

Đây là quyết định thiết kế đáng học theo nhất. Backend seed màu, size và một category mặc định với các GUID cố định. Client không bao giờ hardcode cái nào.

`CommonStore` tải chúng một lần cho mỗi vòng đời ứng dụng:

```typescript
ensureLoaded(): Promise<void> {
  if (this.inFlight) return this.inFlight;
  this.inFlight = this.load().finally(() => { if (this.loadError()) this.inFlight = null; });
  return this.inFlight;
}
```

Nhiều bên gọi đồng thời dùng chung một request; lần tải *thất bại* sẽ xóa promise để có thể thử lại, còn lần thành công thì được cache mãi mãi.

Sau đó các quyết định nghiệp vụ khớp theo **code**, và submit lại **id** đi kèm nó:

```typescript
defaultSizeId(): string {
  return this.sizeByCode(SizeCodes.Medium)?.id ?? this.sizes()[0]?.id ?? '';
}
```

Có phương án dự phòng là phần tử đầu tiên đã tải để form vẫn chạy nếu giá trị mặc định được seed bị đổi tên. Code là danh tính nghiệp vụ; GUID là chi tiết lưu trữ mà chỉ backend nên sở hữu.

## Realtime, chia làm hai

```
SignalrConnectionService   connect, reconnect, state, event dispatch, resubscribe hooks
OrderRealtimeService       hub URL, groups, event names, order-specific subscriptions
```

Service tổng quát không biết gì về đơn hàng. Service của feature đăng ký một callback để đăng ký lại:

```typescript
private async resubscribe(): Promise<void> {
  if (this.orderListSubscribed) await this.connection.invoke('SubscribeToOrderList');
  for (const orderId of this.subscribedOrderIds) await this.connection.invoke('SubscribeToOrder', orderId);
}
```

Việc tham gia group của SignalR **gắn với kết nối** — một lần reconnect sẽ âm thầm đá bạn ra khỏi mọi group, và nếu không có đoạn này thì UI sẽ im lặng sau một cú chớp mạng mà chẳng có lỗi nào để hiển thị.

Thông báo được coi là gợi ý để đọc lại, không bao giờ là dữ liệu có thẩm quyền.

## Optimistic concurrency trong UI

```typescript
const result = await this.orderApi.getById(orderId);   // ApiResult<T> with etag
await this.orderApi.confirm(orderId, result.etag);      // sent as If-Match
```

Lỗi `409` được hiển thị thành "tải lại rồi thử lại". Đừng bao giờ tự động retry với ETag cũ — đó là vòng lặp vô tận chống lại một server đang làm đúng y như phải làm.

## Từ vựng trạng thái

```typescript
export type ProductStatus = 'Draft' | 'Published' | 'Discontinued';

export const productStatusDisplays: Readonly<Record<ProductStatus, StatusDisplay>> = {
  Draft: { label: 'Draft', tone: 'neutral' },
  Published: { label: 'Published', tone: 'success' },
  Discontinued: { label: 'Discontinued', tone: 'warning' }
};
```

Một union chuỗi, không phải enum của TS — comment trong file nói rõ vì sao: *các giá trị này là code của backend và phải khớp chính xác*. Enum của TS mời gọi viết `ProductStatus.Draft` với giá trị số không khớp với dữ liệu truyền đi.

Màu sắc được biểu diễn bằng **tone**, không phải màu của nz. `StatusTagComponent` ánh xạ tone sang bộ UI kit, nên đổi UI kit chỉ là sửa một file.

## Base URL có thể cấu hình

```typescript
readonly salesBase = signal(localStorage.getItem('salesBase') || '/sales-api');
readonly inventoryBase = signal(localStorage.getItem('inventoryBase') || '/inventory-api');
```

Mặc định dùng đường dẫn tương đối, được `proxy.conf.json` proxy trong môi trường development (với `ws: true` để việc nâng cấp lên WebSocket của SignalR hoạt động). Có thể ghi đè lúc chạy, đúng thứ bạn cần ở một client dùng để test tay.

## Lỗi thường gặp

| Sai lầm | Hậu quả |
|---|---|
| Dùng `HttpClient` trong component | xác thực, xử lý lỗi và dựng URL bị lặp lại |
| Hardcode một GUID được seed | hỏng khi môi trường được seed lại |
| Dùng enum TS cho một trạng thái của backend | giá trị số âm thầm lệch với dữ liệu truyền đi |
| Component thuần trình bày lại inject một API service | không test được, không tái sử dụng được |
| Không hủy đăng ký một handler realtime | handler bị nhân đôi sau khi điều hướng |
| Không đăng ký lại sau khi reconnect | UI im lặng mà không có lỗi nào |
| Tự động retry lỗi 409 với đúng ETag cũ | vòng lặp vô tận |
| Chọn màu trạng thái ngay trong template | từ vựng bị rải khắp các file HTML |

## Liên quan

- [../tech/frontend-map.md](../tech/frontend-map.md)
- [../project/frontend/](../project/frontend/)
- [../tech/api-endpoints.md](../tech/api-endpoints.md)

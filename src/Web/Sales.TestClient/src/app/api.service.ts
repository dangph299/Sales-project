import { HttpClient, HttpHeaders, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  ApiResult,
  CustomerDto,
  InventoryDto,
  OrderDto,
  OrderLineInput,
  PagedResult,
  PhoneMatch,
  PhoneMatchApiValue,
  ProductDto,
  TokenResponse
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  readonly salesBase = signal(localStorage.getItem('salesBase') || '/sales-api');
  readonly inventoryBase = signal(localStorage.getItem('inventoryBase') || '/inventory-api');
  readonly accessToken = signal(localStorage.getItem('accessToken') || '');
  readonly refreshToken = signal(localStorage.getItem('refreshToken') || '');

  constructor(private readonly http: HttpClient) {}

  setBaseUrls(salesBase: string, inventoryBase: string): void {
    this.salesBase.set(salesBase.trim() || '/sales-api');
    this.inventoryBase.set(inventoryBase.trim() || '/inventory-api');
    localStorage.setItem('salesBase', this.salesBase());
    localStorage.setItem('inventoryBase', this.inventoryBase());
  }

  async health(): Promise<{ sales: unknown; inventory: unknown }> {
    const [sales, inventory] = await Promise.all([
      firstValueFrom(this.http.get(`${this.salesBase()}/health`)),
      firstValueFrom(this.http.get(`${this.inventoryBase()}/health`))
    ]);
    return { sales, inventory };
  }

  async login(userName: string, password: string): Promise<TokenResponse> {
    const token = await firstValueFrom(this.http.post<TokenResponse>(`${this.salesBase()}/api/auth/login`, { userName, password }));
    this.accessToken.set(token.accessToken);
    this.refreshToken.set(token.refreshToken);
    localStorage.setItem('accessToken', token.accessToken);
    localStorage.setItem('refreshToken', token.refreshToken);
    return token;
  }

  logout(): void {
    this.accessToken.set('');
    this.refreshToken.set('');
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  }

  createProduct(payload: { sku: string; name: string; price: number }): Promise<ProductDto> {
    return firstValueFrom(this.http.post<ProductDto>(`${this.salesBase()}/api/products/`, payload, { headers: this.authHeaders() }));
  }

  updateProduct(id: string, payload: { name: string; price: number; isActive: boolean }): Promise<ProductDto> {
    return firstValueFrom(this.http.put<ProductDto>(`${this.salesBase()}/api/products/${id}`, payload, { headers: this.authHeaders() }));
  }

  searchProducts(name: string, page = 1, pageSize = 20): Promise<PagedResult<ProductDto>> {
    const params = this.params({ name, page, pageSize });
    return firstValueFrom(this.http.get<PagedResult<ProductDto>>(`${this.salesBase()}/api/products/`, { headers: this.authHeaders(), params }));
  }

  createCustomer(payload: { name: string; phone: string }): Promise<CustomerDto> {
    return firstValueFrom(this.http.post<CustomerDto>(`${this.salesBase()}/api/customers/`, payload, { headers: this.authHeaders() }));
  }

  updateCustomer(id: string, payload: { name: string; phone: string }): Promise<CustomerDto> {
    return firstValueFrom(this.http.put<CustomerDto>(`${this.salesBase()}/api/customers/${id}`, payload, { headers: this.authHeaders() }));
  }

  searchCustomers(name: string, phone: string, phoneMatch: PhoneMatch, page = 1, pageSize = 20): Promise<PagedResult<CustomerDto>> {
    const params = this.params({ name, phone, phoneMatch: PhoneMatchApiValue[phoneMatch], page, pageSize });
    return firstValueFrom(this.http.get<PagedResult<CustomerDto>>(`${this.salesBase()}/api/customers/`, { headers: this.authHeaders(), params }));
  }

  async createOrder(customerId: string, lines: OrderLineInput[]): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.post<OrderDto>(
      `${this.salesBase()}/api/orders/`,
      { customerId, lines },
      { headers: this.authHeaders(), observe: 'response' }
    )));
  }

  async getOrder(id: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.get<OrderDto>(
      `${this.salesBase()}/api/orders/${id}`,
      { headers: this.authHeaders(), observe: 'response' }
    )));
  }

  searchOrders(filters: { from?: string; to?: string; customer?: string }, page = 1, pageSize = 20): Promise<PagedResult<OrderDto>> {
    const params = this.params({ ...filters, page, pageSize });
    return firstValueFrom(this.http.get<PagedResult<OrderDto>>(`${this.salesBase()}/api/orders/`, { headers: this.authHeaders(), params }));
  }

  async replaceOrderLines(id: string, lines: OrderLineInput[], etag: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.put<OrderDto>(
      `${this.salesBase()}/api/orders/${id}/lines`,
      lines,
      { headers: this.authHeaders().set('If-Match', etag), observe: 'response' }
    )));
  }

  async confirmOrder(id: string, etag: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.post<OrderDto>(
      `${this.salesBase()}/api/orders/${id}/confirm`,
      {},
      { headers: this.authHeaders().set('If-Match', etag), observe: 'response' }
    )));
  }

  async cancelOrder(id: string, etag: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.post<OrderDto>(
      `${this.salesBase()}/api/orders/${id}/cancel`,
      {},
      { headers: this.authHeaders().set('If-Match', etag), observe: 'response' }
    )));
  }

  async undoConfirmOrder(id: string, etag: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.post<OrderDto>(
      `${this.salesBase()}/api/orders/${id}/undo-confirm`,
      {},
      { headers: this.authHeaders().set('If-Match', etag), observe: 'response' }
    )));
  }

  async adjustInventory(productId: string, sku: string, quantityDelta: number): Promise<InventoryDto> {
    return firstValueFrom(this.http.post<InventoryDto>(
      `${this.inventoryBase()}/api/inventory/${productId}/adjust`,
      { sku, quantityDelta },
      { headers: this.authHeaders() }
    ));
  }

  async getInventory(productId: string): Promise<InventoryDto> {
    return firstValueFrom(this.http.get<InventoryDto>(`${this.inventoryBase()}/api/inventory/${productId}`, { headers: this.authHeaders() }));
  }

  private async withEtag<T>(responsePromise: Promise<HttpResponse<T>>): Promise<ApiResult<T>> {
    const response = await responsePromise;
    return { body: response.body as T, etag: response.headers.get('ETag'), status: response.status };
  }

  private authHeaders(): HttpHeaders {
    const token = this.accessToken();
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }

  private params(values: Record<string, string | number | undefined | null>): HttpParams {
    let params = new HttpParams();
    Object.entries(values).forEach(([key, value]) => {
      if (value !== undefined && value !== null && `${value}` !== '') {
        params = params.set(key, `${value}`);
      }
    });
    return params;
  }
}

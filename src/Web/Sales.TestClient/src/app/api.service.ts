import { HttpClient, HttpHeaders, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClientError, ApiClientResult, ApiResponseReader } from './api-client-result';
import {
  ApiResult,
  CustomerDto,
  InventoryDto,
  OrderDto,
  OrderLineInput,
  PagedResponse,
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
  private readonly lastSuccessMessage = signal<string | null>(null);

  constructor(private readonly http: HttpClient) {}

  setBaseUrls(salesBase: string, inventoryBase: string): void {
    this.salesBase.set(salesBase.trim() || '/sales-api');
    this.inventoryBase.set(inventoryBase.trim() || '/inventory-api');
    localStorage.setItem('salesBase', this.salesBase());
    localStorage.setItem('inventoryBase', this.inventoryBase());
  }

  async health(): Promise<{ sales: unknown; inventory: unknown }> {
    const [sales, inventory] = await Promise.all([
      this.request<string>(firstValueFrom(this.http.get(`${this.salesBase()}/health`, this.textResponseOptions())), true),
      this.request<string>(firstValueFrom(this.http.get(`${this.inventoryBase()}/health`, this.textResponseOptions())), true)
    ]);
    return { sales, inventory };
  }

  async login(userName: string, password: string): Promise<TokenResponse> {
    const token = await this.request<TokenResponse>(
      firstValueFrom(this.http.post(`${this.salesBase()}/api/auth/login`, { userName, password }, this.textResponseOptions())),
      true);
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
    return this.request<ProductDto>(
      firstValueFrom(this.http.post(`${this.salesBase()}/api/products/`, payload, this.textResponseOptions())),
      true);
  }

  updateProduct(id: string, payload: { name: string; price: number; isActive: boolean }): Promise<ProductDto> {
    return this.request<ProductDto>(
      firstValueFrom(this.http.put(`${this.salesBase()}/api/products/${id}`, payload, this.textResponseOptions())),
      true);
  }

  deleteProduct(id: string): Promise<void> {
    return this.request<void>(
      firstValueFrom(this.http.delete(`${this.salesBase()}/api/products/${id}`, this.textResponseOptions())),
      false);
  }

  searchProducts(name: string, page = 1, pageSize = 20): Promise<PagedResult<ProductDto>> {
    const params = this.params({ name, page, pageSize });
    return this.requestPage<ProductDto>(
      firstValueFrom(this.http.get(`${this.salesBase()}/api/products/`, this.textResponseOptions(params))));
  }

  createCustomer(payload: { name: string; phone: string }): Promise<CustomerDto> {
    return this.request<CustomerDto>(
      firstValueFrom(this.http.post(`${this.salesBase()}/api/customers/`, payload, this.textResponseOptions())),
      true);
  }

  updateCustomer(id: string, payload: { name: string; phone: string }): Promise<CustomerDto> {
    return this.request<CustomerDto>(
      firstValueFrom(this.http.put(`${this.salesBase()}/api/customers/${id}`, payload, this.textResponseOptions())),
      true);
  }

  deleteCustomer(id: string): Promise<void> {
    return this.request<void>(
      firstValueFrom(this.http.delete(`${this.salesBase()}/api/customers/${id}`, this.textResponseOptions())),
      false);
  }

  searchCustomers(name: string, phone: string, phoneMatch: PhoneMatch, page = 1, pageSize = 20): Promise<PagedResult<CustomerDto>> {
    const params = this.params({ name, phone, phoneMatch: PhoneMatchApiValue[phoneMatch], page, pageSize });
    return this.requestPage<CustomerDto>(
      firstValueFrom(this.http.get(`${this.salesBase()}/api/customers/`, this.textResponseOptions(params))));
  }

  async createOrder(customerId: string, lines: OrderLineInput[]): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.post(
      `${this.salesBase()}/api/orders/`,
      { customerId, lines },
      this.textResponseOptions()
    )));
  }

  async getOrder(id: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.get(
      `${this.salesBase()}/api/orders/${id}`,
      this.textResponseOptions()
    )));
  }

  searchOrders(filters: { from?: string; to?: string; customer?: string }, page = 1, pageSize = 20): Promise<PagedResult<OrderDto>> {
    const params = this.params({ ...filters, page, pageSize });
    return this.requestPage<OrderDto>(
      firstValueFrom(this.http.get(`${this.salesBase()}/api/orders/`, this.textResponseOptions(params))));
  }

  async replaceOrderLines(id: string, lines: OrderLineInput[], etag: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.put(
      `${this.salesBase()}/api/orders/${id}/lines`,
      lines,
      this.textResponseOptions(undefined, this.authHeaders().set('If-Match', etag))
    )));
  }

  async confirmOrder(id: string, etag: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.post(
      `${this.salesBase()}/api/orders/${id}/confirm`,
      {},
      this.textResponseOptions(undefined, this.authHeaders().set('If-Match', etag))
    )));
  }

  async cancelOrder(id: string, etag: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.post(
      `${this.salesBase()}/api/orders/${id}/cancel`,
      {},
      this.textResponseOptions(undefined, this.authHeaders().set('If-Match', etag))
    )));
  }

  async undoConfirmOrder(id: string, etag: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.post(
      `${this.salesBase()}/api/orders/${id}/undo-confirm`,
      {},
      this.textResponseOptions(undefined, this.authHeaders().set('If-Match', etag))
    )));
  }

  async adjustInventory(productId: string, sku: string, quantityDelta: number): Promise<InventoryDto> {
    return this.request<InventoryDto>(firstValueFrom(this.http.post(
      `${this.inventoryBase()}/api/inventory/${productId}/adjust`,
      { sku, quantityDelta },
      this.textResponseOptions()
    )), true);
  }

  async getInventory(productId: string): Promise<InventoryDto> {
    return this.request<InventoryDto>(
      firstValueFrom(this.http.get(`${this.inventoryBase()}/api/inventory/${productId}`, this.textResponseOptions())),
      true);
  }

  consumeSuccessMessage(): string | null {
    const message = this.lastSuccessMessage();
    this.lastSuccessMessage.set(null);
    return message;
  }

  clearSuccessMessage(): void {
    this.lastSuccessMessage.set(null);
  }

  private async withEtag<T>(responsePromise: Promise<HttpResponse<string>>): Promise<ApiResult<T>> {
    try {
      const response = await responsePromise;
      const result = ApiResponseReader.readSuccess<T>(response);
      const data = ApiResponseReader.ensureSuccess<T>(result, true);
      this.recordSuccessMessage(result);
      return {
        body: data,
        etag: response.headers.get('ETag'),
        status: response.status,
        message: result.message,
        correlationId: result.correlationId
      };
    } catch (error) {
      this.throwApiError(error);
    }
  }

  private async request<T>(responsePromise: Promise<HttpResponse<string>>, requireData: true): Promise<NonNullable<T>>;
  private async request<T>(responsePromise: Promise<HttpResponse<string>>, requireData: false): Promise<void>;
  private async request<T>(responsePromise: Promise<HttpResponse<string>>, requireData: boolean): Promise<NonNullable<T> | void> {
    try {
      const response = await responsePromise;
      const result = ApiResponseReader.readSuccess<T>(response);
      this.recordSuccessMessage(result);
      if (requireData) {
        return ApiResponseReader.ensureSuccess<T>(result, true);
      }

      ApiResponseReader.ensureSuccess<T>(result, false);
      return;
    } catch (error) {
      this.throwApiError(error);
    }
  }

  private async requestPage<T>(responsePromise: Promise<HttpResponse<string>>): Promise<PagedResult<T>> {
    const page = await this.request<PagedResult<T> | PagedResponse<T>>(responsePromise, true);
    return this.normalizePage(page);
  }

  private normalizePage<T>(page: PagedResult<T> | PagedResponse<T>): PagedResult<T> {
    if ('totalCount' in page) {
      return {
        items: page.items || [],
        page: page.pageNumber,
        pageSize: page.pageSize,
        total: page.totalCount
      };
    }

    return {
      items: page.items || [],
      page: page.page,
      pageSize: page.pageSize,
      total: page.total
    };
  }

  private recordSuccessMessage<T>(result: ApiClientResult<T>): void {
    if (result.message && result.message.trim() !== '') {
      this.lastSuccessMessage.set(result.message);
      return;
    }

    this.lastSuccessMessage.set(null);
  }

  private throwApiError(error: unknown): never {
    if (error instanceof ApiClientError) {
      throw error;
    }

    const failure = ApiResponseReader.readFailure<unknown>(error);
    throw new ApiClientError(failure);
  }

  private textResponseOptions(params?: HttpParams, headers = this.authHeaders()): {
    headers: HttpHeaders;
    observe: 'response';
    params?: HttpParams;
    responseType: 'text';
  } {
    const options: {
      headers: HttpHeaders;
      observe: 'response';
      params?: HttpParams;
      responseType: 'text';
    } = {
      headers,
      observe: 'response',
      responseType: 'text'
    };

    if (params) {
      options.params = params;
    }

    return options;
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

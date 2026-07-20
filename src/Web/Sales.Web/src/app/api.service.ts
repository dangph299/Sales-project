import { HttpClient, HttpHeaders, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClientError, ApiClientResult, ApiResponseReader } from './api-client-result';
import {
  ApiResult,
  CategoryDto,
  CreateCategoryRequestDto,
  CreateCustomerRequestDto,
  CreateProductRequestDto,
  CreateProductVariantRequestDto,
  CustomerDto,
  ECustomerStatus,
  InventoryDto,
  OrderDto,
  OrderLineInput,
  PagedResponse,
  PagedResult,
  PhoneMatch,
  PhoneMatchApiValue,
  ProductDto,
  ReservationDto,
  TokenResponse,
  UpdateCategoryRequestDto,
  UpdateCustomerRequestDto,
  UpdateProductRequestDto,
  UpdateProductVariantRequestDto
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

  createCustomer(customerRequest: CreateCustomerRequestDto): Promise<CustomerDto> {
    return this.request<CustomerDto>(
      firstValueFrom(this.http.post(`${this.salesBase()}/api/customers/`, customerRequest, this.textResponseOptions())),
      true);
  }

  updateCustomer(customerId: string, customerRequest: UpdateCustomerRequestDto): Promise<CustomerDto> {
    return this.request<CustomerDto>(
      firstValueFrom(this.http.put(`${this.salesBase()}/api/customers/${customerId}`, customerRequest, this.textResponseOptions())),
      true);
  }

  updateCustomerStatus(customerId: string, status: ECustomerStatus): Promise<CustomerDto> {
    return this.request<CustomerDto>(
      firstValueFrom(this.http.put(`${this.salesBase()}/api/customers/${customerId}/status`, { status }, this.textResponseOptions())),
      true);
  }

  deleteCustomer(customerId: string): Promise<void> {
    return this.request<void>(
      firstValueFrom(this.http.delete(`${this.salesBase()}/api/customers/${customerId}`, this.textResponseOptions())),
      false);
  }

  getCustomer(customerId: string): Promise<ApiResult<CustomerDto>> {
    return this.withEtag(firstValueFrom(this.http.get(`${this.salesBase()}/api/customers/${customerId}`, this.textResponseOptions())));
  }

  searchCustomers(filters: { name?: string; phone?: string; phoneMatch?: PhoneMatch; page?: number; pageSize?: number }): Promise<PagedResult<CustomerDto>> {
    const params = this.params({
      name: filters.name,
      phone: filters.phone,
      phoneMatch: PhoneMatchApiValue[filters.phoneMatch ?? PhoneMatch.Prefix],
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20
    });
    return this.requestPage<CustomerDto>(
      firstValueFrom(this.http.get(`${this.salesBase()}/api/customers/`, this.textResponseOptions(params))));
  }

  createCategory(categoryRequest: CreateCategoryRequestDto): Promise<CategoryDto> {
    return this.request<CategoryDto>(
      firstValueFrom(this.http.post(`${this.salesBase()}/api/categories/`, categoryRequest, this.textResponseOptions())),
      true);
  }

  updateCategory(categoryId: string, categoryRequest: UpdateCategoryRequestDto): Promise<CategoryDto> {
    return this.request<CategoryDto>(
      firstValueFrom(this.http.put(`${this.salesBase()}/api/categories/${categoryId}`, categoryRequest, this.textResponseOptions())),
      true);
  }

  createProduct(productRequest: CreateProductRequestDto): Promise<ProductDto> {
    return this.request<ProductDto>(
      firstValueFrom(this.http.post(`${this.salesBase()}/api/products/`, productRequest, this.textResponseOptions())),
      true);
  }

  updateProduct(productId: string, productRequest: UpdateProductRequestDto): Promise<ProductDto> {
    return this.request<ProductDto>(
      firstValueFrom(this.http.put(`${this.salesBase()}/api/products/${productId}`, productRequest, this.textResponseOptions())),
      true);
  }

  deleteProduct(productId: string): Promise<void> {
    return this.request<void>(
      firstValueFrom(this.http.delete(`${this.salesBase()}/api/products/${productId}`, this.textResponseOptions())),
      false);
  }

  getProduct(productId: string): Promise<ApiResult<ProductDto>> {
    return this.withEtag(firstValueFrom(this.http.get(`${this.salesBase()}/api/products/${productId}`, this.textResponseOptions())));
  }

  searchProducts(filters: {
    productCode?: string;
    name?: string;
    sku?: string;
    categoryId?: string;
    colorId?: string;
    sizeId?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PagedResult<ProductDto>> {
    const params = this.params({
      productCode: filters.productCode,
      name: filters.name,
      sku: filters.sku,
      categoryId: filters.categoryId,
      colorId: filters.colorId,
      sizeId: filters.sizeId,
      status: filters.status,
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20
    });
    return this.requestPage<ProductDto>(
      firstValueFrom(this.http.get(`${this.salesBase()}/api/products/`, this.textResponseOptions(params))));
  }

  addProductVariant(productId: string, productVariantRequest: CreateProductVariantRequestDto): Promise<ProductDto> {
    return this.request<ProductDto>(
      firstValueFrom(this.http.post(`${this.salesBase()}/api/products/${productId}/variants`, productVariantRequest, this.textResponseOptions())),
      true);
  }

  updateProductVariant(productId: string, productVariantId: string, productVariantRequest: UpdateProductVariantRequestDto): Promise<ProductDto> {
    return this.request<ProductDto>(
      firstValueFrom(this.http.put(`${this.salesBase()}/api/products/${productId}/variants/${productVariantId}`, productVariantRequest, this.textResponseOptions())),
      true);
  }

  discontinueProductVariant(productId: string, productVariantId: string): Promise<ProductDto> {
    return this.request<ProductDto>(
      firstValueFrom(this.http.post(`${this.salesBase()}/api/products/${productId}/variants/${productVariantId}/deactivate`, {}, this.textResponseOptions())),
      true);
  }

  async createOrder(customerId: string, lines: OrderLineInput[]): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.post(
      `${this.salesBase()}/api/orders/`,
      { customerId, lines },
      this.textResponseOptions()
    )));
  }

  async getOrder(orderId: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.get(
      `${this.salesBase()}/api/orders/${orderId}`,
      this.textResponseOptions()
    )));
  }

  searchOrders(filters: { from?: string; to?: string; customer?: string; page?: number; pageSize?: number }): Promise<PagedResult<OrderDto>> {
    const params = this.params({
      from: filters.from,
      to: filters.to,
      customer: filters.customer,
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20
    });
    return this.requestPage<OrderDto>(
      firstValueFrom(this.http.get(`${this.salesBase()}/api/orders/`, this.textResponseOptions(params))));
  }

  async replaceOrderLines(orderId: string, lines: OrderLineInput[], etag: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.put(
      `${this.salesBase()}/api/orders/${orderId}/lines`,
      lines,
      this.textResponseOptions(undefined, this.authHeaders().set('If-Match', etag))
    )));
  }

  async confirmOrder(orderId: string, etag: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.post(
      `${this.salesBase()}/api/orders/${orderId}/confirm`,
      {},
      this.textResponseOptions(undefined, this.authHeaders().set('If-Match', etag))
    )));
  }

  async cancelOrder(orderId: string, etag: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.post(
      `${this.salesBase()}/api/orders/${orderId}/cancel`,
      {},
      this.textResponseOptions(undefined, this.authHeaders().set('If-Match', etag))
    )));
  }

  async undoConfirmOrder(orderId: string, etag: string): Promise<ApiResult<OrderDto>> {
    return this.withEtag(firstValueFrom(this.http.post(
      `${this.salesBase()}/api/orders/${orderId}/undo-confirm`,
      {},
      this.textResponseOptions(undefined, this.authHeaders().set('If-Match', etag))
    )));
  }

  async adjustInventory(productVariantId: string, sku: string, quantityDelta: number): Promise<InventoryDto> {
    return this.request<InventoryDto>(firstValueFrom(this.http.post(
      `${this.inventoryBase()}/api/inventory/${productVariantId}/adjust`,
      { sku, quantityDelta },
      this.textResponseOptions()
    )), true);
  }

  async getInventory(productVariantId: string): Promise<InventoryDto | null> {
    try {
      return await this.request<InventoryDto>(firstValueFrom(this.http.get(
        `${this.inventoryBase()}/api/inventory/${productVariantId}`,
        this.textResponseOptions()
      )), true);
    } catch (error) {
      if (error instanceof ApiClientError && error.status === 404) {
        return null;
      }

      throw error;
    }
  }

  async getReservation(orderId: string): Promise<ReservationDto | null> {
    try {
      return await this.request<ReservationDto>(firstValueFrom(this.http.get(
        `${this.inventoryBase()}/api/inventory/reservations/${orderId}`,
        this.textResponseOptions()
      )), true);
    } catch (error) {
      if (error instanceof ApiClientError && error.status === 404) {
        return null;
      }

      throw error;
    }
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
      const apiResult = ApiResponseReader.readSuccess<T>(response);
      const responseBody = ApiResponseReader.ensureSuccess<T>(apiResult, true);
      this.recordSuccessMessage(apiResult);
      return {
        body: responseBody,
        etag: response.headers.get('ETag'),
        status: response.status,
        message: apiResult.message,
        correlationId: apiResult.correlationId
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
      const apiResult = ApiResponseReader.readSuccess<T>(response);
      this.recordSuccessMessage(apiResult);
      if (requireData) {
        return ApiResponseReader.ensureSuccess<T>(apiResult, true);
      }

      ApiResponseReader.ensureSuccess<T>(apiResult, false);
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

  private recordSuccessMessage<T>(apiResult: ApiClientResult<T>): void {
    if (apiResult.message && apiResult.message.trim() !== '') {
      this.lastSuccessMessage.set(apiResult.message);
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
    Object.entries(values).forEach(([key, parameterValue]) => {
      if (parameterValue !== undefined && parameterValue !== null && `${parameterValue}` !== '') {
        params = params.set(key, `${parameterValue}`);
      }
    });
    return params;
  }
}

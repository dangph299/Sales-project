import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { ApiResult } from '../../../core/api/api-result.model';
import { PagedResult } from '../../../core/api/paged-result.model';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { OrderLineRequest } from './requests/order-line.request';
import { OrderReservationResponse } from './responses/order-reservation.response';
import { OrderResponse } from './responses/order.response';

export interface SearchOrdersFilters {
  from?: string;
  to?: string;
  customer?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class OrderApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);

  private get baseUrl(): string {
    return this.endpoints.salesBase();
  }

  search(filters: SearchOrdersFilters = {}): Promise<PagedResult<OrderResponse>> {
    return this.client.getPage<OrderResponse>(this.baseUrl, '/api/orders/', {
      from: filters.from,
      to: filters.to,
      customer: filters.customer,
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20
    });
  }

  getById(orderId: string): Promise<ApiResult<OrderResponse>> {
    return this.client.getWithEtag<OrderResponse>(this.baseUrl, `/api/orders/${orderId}`);
  }

  create(customerId: string, lines: OrderLineRequest[]): Promise<ApiResult<OrderResponse>> {
    return this.client.postWithEtag<OrderResponse>(this.baseUrl, '/api/orders/', { customerId, lines });
  }

  replaceLines(orderId: string, lines: OrderLineRequest[], etag: string): Promise<ApiResult<OrderResponse>> {
    return this.client.putWithEtag<OrderResponse>(this.baseUrl, `/api/orders/${orderId}/lines`, lines, etag);
  }

  confirm(orderId: string, etag: string): Promise<ApiResult<OrderResponse>> {
    return this.client.postWithEtag<OrderResponse>(this.baseUrl, `/api/orders/${orderId}/confirm`, {}, etag);
  }

  cancel(orderId: string, etag: string): Promise<ApiResult<OrderResponse>> {
    return this.client.postWithEtag<OrderResponse>(this.baseUrl, `/api/orders/${orderId}/cancel`, {}, etag);
  }

  undoConfirm(orderId: string, etag: string): Promise<ApiResult<OrderResponse>> {
    return this.client.postWithEtag<OrderResponse>(this.baseUrl, `/api/orders/${orderId}/undo-confirm`, {}, etag);
  }

  /** Order-scoped read against the Inventory API. Null when no reservation exists. */
  getReservation(orderId: string): Promise<OrderReservationResponse | null> {
    return this.client.getOptional<OrderReservationResponse>(
      this.endpoints.inventoryBase(),
      `/api/inventory/reservations/${orderId}`);
  }
}

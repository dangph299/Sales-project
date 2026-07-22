import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { ApiResult } from '../../../core/api/api-result.model';
import { PagedResult } from '../../../core/api/paged-result.model';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { OrderCustomerPhoneMatchMode } from '../constants/order-customer-phone-match-mode';
import { OrderStatus } from '../constants/order-status';
import { OrderCustomerRequest } from './requests/order-customer.request';
import { OrderLineRequest } from './requests/order-line.request';
import { OrderReservationResponse } from './responses/order-reservation.response';
import { OrderResponse } from './responses/order.response';

/**
 * Order search filters, each naming exactly what it matches.
 *
 * Every one of these is applied by the database across the whole table, so a
 * match on another page is still found. `customerPhone` is sent as typed; the
 * backend normalizes it.
 */
export interface SearchOrdersFilters {
  orderNumber?: string;
  customerName?: string;
  customerPhone?: string;
  customerPhoneMatchMode?: OrderCustomerPhoneMatchMode;
  from?: string;
  to?: string;
  status?: OrderStatus | '' | null;
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
      orderNumber: filters.orderNumber,
      customerName: filters.customerName,
      customerPhone: filters.customerPhone,
      // Only meaningful alongside a phone term, and omitted otherwise so the
      // backend keeps its own default.
      customerPhoneMatchMode: filters.customerPhone ? filters.customerPhoneMatchMode : undefined,
      from: filters.from,
      to: filters.to,
      status: filters.status,
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20
    });
  }

  getById(orderId: string): Promise<ApiResult<OrderResponse>> {
    return this.client.getWithEtag<OrderResponse>(this.baseUrl, `/api/orders/${orderId}`);
  }

  /**
   * Creates an order from the customer details the user entered.
   *
   * The client never decides whether that customer exists and never creates one
   * first: the backend resolves the phone number or creates the customer, in the
   * same transaction as the order.
   */
  create(customer: OrderCustomerRequest, lines: OrderLineRequest[]): Promise<ApiResult<OrderResponse>> {
    return this.client.postWithEtag<OrderResponse>(this.baseUrl, '/api/orders/', { customer, lines });
  }

  /**
   * Replaces the customer details recorded on an order.
   *
   * Edits the order's snapshot only — the customer record is left alone.
   */
  updateCustomer(orderId: string, customer: OrderCustomerRequest, etag: string): Promise<ApiResult<OrderResponse>> {
    return this.client.putWithEtag<OrderResponse>(this.baseUrl, `/api/orders/${orderId}/customer`, customer, etag);
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

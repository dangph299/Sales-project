import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { ApiResult } from '../../../core/api/api-result.model';
import { PagedResult } from '../../../core/api/paged-result.model';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { CustomerStatus } from '../constants/customer-status';
import { SaveCustomerRequest } from './requests/save-customer.request';
import { CustomerResponse } from './responses/customer.response';

export interface SearchCustomersFilters {
  name?: string;
  phone?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class CustomerApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);

  private get baseUrl(): string {
    return this.endpoints.salesBase();
  }

  search(filters: SearchCustomersFilters = {}): Promise<PagedResult<CustomerResponse>> {
    return this.client.getPage<CustomerResponse>(this.baseUrl, '/api/customers/', {
      name: filters.name,
      phone: filters.phone,
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20
    });
  }

  getById(customerId: string): Promise<ApiResult<CustomerResponse>> {
    return this.client.getWithEtag<CustomerResponse>(this.baseUrl, `/api/customers/${customerId}`);
  }

  create(request: SaveCustomerRequest): Promise<CustomerResponse> {
    return this.client.post<CustomerResponse>(this.baseUrl, '/api/customers/', request);
  }

  update(customerId: string, request: SaveCustomerRequest): Promise<CustomerResponse> {
    return this.client.put<CustomerResponse>(this.baseUrl, `/api/customers/${customerId}`, request);
  }

  updateStatus(customerId: string, status: CustomerStatus): Promise<CustomerResponse> {
    return this.client.put<CustomerResponse>(this.baseUrl, `/api/customers/${customerId}/status`, { status });
  }

  delete(customerId: string): Promise<void> {
    return this.client.delete(this.baseUrl, `/api/customers/${customerId}`);
  }
}

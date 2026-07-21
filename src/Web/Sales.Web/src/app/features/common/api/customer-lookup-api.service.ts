import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { PagedResult } from '../../../core/api/paged-result.model';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { CustomerLookupResponse } from '../contracts/customer-lookup.response';

export interface CustomerLookupFilters {
  name?: string;
  phone?: string;
  page?: number;
  pageSize?: number;
}

/**
 * Read-only customer lookup for features that need to pick a customer without
 * depending on the Customers feature's internals.
 */
@Injectable({ providedIn: 'root' })
export class CustomerLookupApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);

  search(filters: CustomerLookupFilters = {}): Promise<PagedResult<CustomerLookupResponse>> {
    return this.client.getPage<CustomerLookupResponse>(
      this.endpoints.salesBase(),
      '/api/customers/',
      {
        name: filters.name,
        phone: filters.phone,
        page: filters.page ?? 1,
        pageSize: filters.pageSize ?? 20
      });
  }
}

import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { PagedResult } from '../../../core/api/paged-result.model';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import {
  CustomerLookupResponse,
  CustomerPhoneSuggestionResponse
} from '../contracts/customer-lookup.response';

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

  /**
   * Suggests customers whose phone number starts with what the user has typed.
   *
   * The phone goes up exactly as typed; the backend normalizes it and matches a
   * prefix against its indexed column. The client never sees, sends or reasons
   * about the normalized or reversed values.
   */
  suggestByPhone(customerPhoneSearchTerm: string, limit = 10): Promise<CustomerPhoneSuggestionResponse[]> {
    return this.client.get<CustomerPhoneSuggestionResponse[]>(
      this.endpoints.salesBase(),
      '/api/customers/lookup',
      { phone: customerPhoneSearchTerm, limit });
  }
}

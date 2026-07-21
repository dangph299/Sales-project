import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { PagedResult } from '../../../core/api/paged-result.model';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { ProductLookupResponse } from '../contracts/product-lookup.response';

export interface ProductLookupFilters {
  name?: string;
  sku?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

/**
 * Read-only product lookup for features that need to pick a product or variant
 * without depending on the Products feature's internals.
 */
@Injectable({ providedIn: 'root' })
export class ProductLookupApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);

  search(filters: ProductLookupFilters = {}): Promise<PagedResult<ProductLookupResponse>> {
    return this.client.getPage<ProductLookupResponse>(
      this.endpoints.salesBase(),
      '/api/products/',
      {
        name: filters.name,
        sku: filters.sku,
        status: filters.status,
        page: filters.page ?? 1,
        pageSize: filters.pageSize ?? 20
      });
  }
}

import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { PagedResult } from '../../../core/api/paged-result.model';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { ProductLookupResponse, ProductVariantPageResponse } from '../contracts/product-lookup.response';

export interface ProductLookupFilters {
  name?: string;
  sku?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

export interface ProductVariantLookupFilters {
  productCode?: string;
  productName?: string;
  sku?: string;
  variantStatus?: string;
  sortBy?: string;
  sortDirection?: string;
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

  searchVariants(filters: ProductVariantLookupFilters = {}): Promise<PagedResult<ProductVariantPageResponse>> {
    return this.client.getPage<ProductVariantPageResponse>(
      this.endpoints.salesBase(),
      '/api/products/variants',
      {
        productCode: filters.productCode,
        productName: filters.productName,
        sku: filters.sku,
        variantStatus: filters.variantStatus,
        sortBy: filters.sortBy,
        sortDirection: filters.sortDirection,
        page: filters.page ?? 1,
        pageSize: filters.pageSize ?? 20
      });
  }
}

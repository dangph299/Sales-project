import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { ApiResult } from '../../../core/api/api-result.model';
import { PagedResult } from '../../../core/api/paged-result.model';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { CreateProductRequest } from './requests/create-product.request';
import { SaveProductVariantRequest } from './requests/save-product-variant.request';
import { UpdateProductRequest } from './requests/update-product.request';
import { ProductResponse } from './responses/product.response';

export interface SearchProductsFilters {
  productCode?: string;
  name?: string;
  sku?: string;
  categoryId?: string;
  colorId?: string;
  sizeId?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class ProductApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);

  private get baseUrl(): string {
    return this.endpoints.salesBase();
  }

  search(filters: SearchProductsFilters = {}): Promise<PagedResult<ProductResponse>> {
    return this.client.getPage<ProductResponse>(this.baseUrl, '/api/products/', {
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
  }

  getById(productId: string): Promise<ApiResult<ProductResponse>> {
    return this.client.getWithEtag<ProductResponse>(this.baseUrl, `/api/products/${productId}`);
  }

  create(request: CreateProductRequest): Promise<ProductResponse> {
    return this.client.post<ProductResponse>(this.baseUrl, '/api/products/', request);
  }

  update(productId: string, request: UpdateProductRequest): Promise<ProductResponse> {
    return this.client.put<ProductResponse>(this.baseUrl, `/api/products/${productId}`, request);
  }

  delete(productId: string): Promise<void> {
    return this.client.delete(this.baseUrl, `/api/products/${productId}`);
  }

  addVariant(productId: string, request: SaveProductVariantRequest): Promise<ProductResponse> {
    return this.client.post<ProductResponse>(this.baseUrl, `/api/products/${productId}/variants`, request);
  }

  updateVariant(productId: string, variantId: string, request: SaveProductVariantRequest): Promise<ProductResponse> {
    return this.client.put<ProductResponse>(this.baseUrl, `/api/products/${productId}/variants/${variantId}`, request);
  }

  discontinueVariant(productId: string, variantId: string): Promise<ProductResponse> {
    return this.client.post<ProductResponse>(
      this.baseUrl,
      `/api/products/${productId}/variants/${variantId}/deactivate`);
  }

  deleteVariant(productId: string, variantId: string): Promise<void> {
    return this.client.delete(this.baseUrl, `/api/products/${productId}/variants/${variantId}`);
  }
}

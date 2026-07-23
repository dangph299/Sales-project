import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { AdjustInventoryRequest } from './requests/adjust-inventory.request';
import { GetInventoryByVariantIdsRequest } from './requests/get-inventory-by-variant-ids.request';
import { InventoryBatchResponse, InventoryResponse } from './responses/inventory.response';

@Injectable({ providedIn: 'root' })
export class InventoryApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);

  private get baseUrl(): string {
    return this.endpoints.inventoryBase();
  }

  /** Null when the variant has no inventory record yet. */
  getByVariant(productVariantId: string): Promise<InventoryResponse | null> {
    return this.client.getOptional<InventoryResponse>(this.baseUrl, `/api/inventory/${productVariantId}`);
  }

  getByVariants(productVariantIds: string[]): Promise<InventoryBatchResponse> {
    const request: GetInventoryByVariantIdsRequest = { productVariantIds };
    return this.client.post<InventoryBatchResponse>(this.baseUrl, '/api/inventory/by-variant-ids', request);
  }

  adjust(productVariantId: string, request: AdjustInventoryRequest): Promise<InventoryResponse> {
    return this.client.post<InventoryResponse>(this.baseUrl, `/api/inventory/${productVariantId}/adjust`, request);
  }
}

import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { AdjustInventoryRequest } from './requests/adjust-inventory.request';
import { InventoryResponse } from './responses/inventory.response';

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

  adjust(productVariantId: string, request: AdjustInventoryRequest): Promise<InventoryResponse> {
    return this.client.post<InventoryResponse>(this.baseUrl, `/api/inventory/${productVariantId}/adjust`, request);
  }
}

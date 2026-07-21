import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../api/api-client.service';
import { ApiEndpointConfigurationService } from '../config/api-endpoint-configuration.service';

export interface HealthStatus {
  sales: unknown;
  inventory: unknown;
}

@Injectable({ providedIn: 'root' })
export class HealthApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);

  async check(): Promise<HealthStatus> {
    const [sales, inventory] = await Promise.all([
      this.client.get<string>(this.endpoints.salesBase(), '/health'),
      this.client.get<string>(this.endpoints.inventoryBase(), '/health')
    ]);

    return { sales, inventory };
  }
}

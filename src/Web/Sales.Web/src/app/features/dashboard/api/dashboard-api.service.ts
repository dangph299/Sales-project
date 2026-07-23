import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { DashboardSnapshotResponse } from './responses/dashboard-snapshot.response';
import { toDashboardSnapshot } from '../mappers/dashboard.mapper';
import { DashboardSnapshot } from '../models/dashboard-snapshot.model';

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);

  async loadSnapshot(): Promise<DashboardSnapshot> {
    const response = await this.client.get<DashboardSnapshotResponse>(
      this.endpoints.dashboardBase(),
      '/api/dashboard');

    return toDashboardSnapshot(response);
  }
}

import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { PagedResult } from '../../../core/api/paged-result.model';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { DashboardMetrics } from '../models/dashboard-metrics.model';
import { RecentOrderRow, RecentProductRow } from '../models/dashboard-row.model';
import { toDashboardMetrics, toRecentOrderRows, toRecentProductRows } from '../mappers/dashboard.mapper';

/**
 * Raw shapes this feature reads from the list endpoints.
 *
 * Deliberately narrow and dashboard-owned: the dashboard summarises small pages
 * from several endpoints and must not depend on the Orders, Products or
 * Customers feature response models.
 */
export interface DashboardOrderPayload {
  id: string;
  customerName: string;
  status: string;
  total: number;
  createdAt: string;
}

export interface DashboardProductVariantPayload {
  status: string;
}

export interface DashboardProductPayload {
  id: string;
  sku: string;
  productCode?: string | null;
  name: string;
  status?: string | null;
  isActive: boolean;
  minPrice?: number | null;
  maxPrice?: number | null;
  variants?: DashboardProductVariantPayload[] | null;
}

export interface DashboardSnapshot {
  metrics: DashboardMetrics;
  recentOrders: RecentOrderRow[];
  recentProducts: RecentProductRow[];
}

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);

  async loadSnapshot(): Promise<DashboardSnapshot> {
    const salesBase = this.endpoints.salesBase();

    const [orders, products, customers] = await Promise.all([
      this.client.getPage<DashboardOrderPayload>(salesBase, '/api/orders/', { page: 1, pageSize: 5 }),
      this.client.getPage<DashboardProductPayload>(salesBase, '/api/products/', { page: 1, pageSize: 8 }),
      this.client.getPage<unknown>(salesBase, '/api/customers/', { page: 1, pageSize: 1 })
    ]);

    return {
      metrics: toDashboardMetrics(orders, products, customers as PagedResult<unknown>),
      recentOrders: toRecentOrderRows(orders.items),
      recentProducts: toRecentProductRows(products.items)
    };
  }
}

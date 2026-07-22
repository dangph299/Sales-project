import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { DashboardMetrics } from '../models/dashboard-metrics.model';
import { RecentOrderRow, RecentProductRow } from '../models/dashboard-row.model';
import { toDashboardMetrics, toRecentOrderRows, toRecentProductRows } from '../mappers/dashboard.mapper';

/**
 * Raw shapes this feature reads from the list endpoints.
 *
 * Deliberately narrow and dashboard-owned: the dashboard reads only the fields
 * its overview needs and does not depend on Orders, Products or Customers
 * feature response models.
 */
export interface DashboardOrderPayload {
  id: string;
  orderCode: string;
  customerName: string;
  status: string;
  totalQuantity: number;
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
  chartOrders: RecentOrderRow[];
}

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);

  async loadSnapshot(): Promise<DashboardSnapshot> {
    const salesBase = this.endpoints.salesBase();
    const now = new Date();
    const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const tomorrowStart = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1);
    const sevenDaysStart = new Date(now.getFullYear(), now.getMonth(), now.getDate() - 6);

    const [recentOrders, todayOrders, chartOrders, orderCount, pendingOrders, recentProducts, productCount, publishedProducts, customers] = await Promise.all([
      this.client.getPage<DashboardOrderPayload>(salesBase, '/api/orders/', { page: 1, pageSize: 5 }),
      this.client.getPage<DashboardOrderPayload>(salesBase, '/api/orders/', {
        from: todayStart.toISOString(),
        to: tomorrowStart.toISOString(),
        page: 1,
        pageSize: 100
      }),
      this.client.getPage<DashboardOrderPayload>(salesBase, '/api/orders/', {
        from: sevenDaysStart.toISOString(),
        to: tomorrowStart.toISOString(),
        page: 1,
        pageSize: 100
      }),
      this.client.getPage<DashboardOrderPayload>(salesBase, '/api/orders/', { page: 1, pageSize: 1 }),
      this.client.getPage<DashboardOrderPayload>(salesBase, '/api/orders/', { status: 'PendingInventory', page: 1, pageSize: 1 }),
      this.client.getPage<DashboardProductPayload>(salesBase, '/api/products/', { page: 1, pageSize: 5 }),
      this.client.getPage<DashboardProductPayload>(salesBase, '/api/products/', { page: 1, pageSize: 1 }),
      this.client.getPage<DashboardProductPayload>(salesBase, '/api/products/', { status: 'Published', page: 1, pageSize: 1 }),
      this.client.getPage<unknown>(salesBase, '/api/customers/', { page: 1, pageSize: 1 })
    ]);

    return {
      metrics: toDashboardMetrics({
        periodOrders: todayOrders,
        orderCount,
        pendingOrders,
        productCount,
        publishedProducts,
        customers
      }),
      recentOrders: toRecentOrderRows(recentOrders.items),
      recentProducts: toRecentProductRows(recentProducts.items),
      chartOrders: toRecentOrderRows(chartOrders.items)
    };
  }
}

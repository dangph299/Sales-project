import { PagedResult } from '../../../core/api/paged-result.model';
import type { DashboardOrderPayload, DashboardProductPayload } from '../api/dashboard-api.service';
import { dashboardOrderStatusDisplay, dashboardProductStatusDisplay } from '../constants/dashboard-status';
import { DashboardMetrics } from '../models/dashboard-metrics.model';
import { RecentOrderRow, RecentProductRow } from '../models/dashboard-row.model';

export interface DashboardMetricSources {
  periodOrders: PagedResult<DashboardOrderPayload>;
  orderCount: PagedResult<DashboardOrderPayload>;
  pendingOrders: PagedResult<DashboardOrderPayload>;
  productCount: PagedResult<DashboardProductPayload>;
  publishedProducts: PagedResult<DashboardProductPayload>;
  customers: PagedResult<unknown>;
}

export function toDashboardMetrics(sources: DashboardMetricSources): DashboardMetrics {
  return {
    orderTotal: sources.orderCount.total,
    customerTotal: sources.customers.total,
    pendingOrderCount: sources.pendingOrders.total,
    revenueToday: sources.periodOrders.items.reduce((total, order) => total + order.total, 0),
    productTotal: sources.productCount.total,
    publishedProductCount: sources.publishedProducts.total
  };
}

export function toRecentOrderRows(orders: DashboardOrderPayload[]): RecentOrderRow[] {
  return orders.map(order => ({
    id: order.id,
    orderCode: order.orderCode,
    customerName: order.customerName,
    status: dashboardOrderStatusDisplay(order.status),
    totalQuantity: order.totalQuantity,
    total: order.total,
    createdAt: order.createdAt
  }));
}

export function toRecentProductRows(products: DashboardProductPayload[]): RecentProductRow[] {
  return products.map(product => ({
    id: product.id,
    productCode: product.productCode || product.sku,
    productName: product.name,
    status: dashboardProductStatusDisplay(product.status || (product.isActive ? 'Published' : 'Draft')),
    minPrice: product.minPrice,
    maxPrice: product.maxPrice
  }));
}

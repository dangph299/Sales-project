import { PagedResult } from '../../../core/api/paged-result.model';
import { StatusDisplay, toStatusDisplay } from '../../../shared/models/status-display.model';
import type { DashboardOrderPayload, DashboardProductPayload } from '../api/dashboard-api.service';
import { DashboardMetrics } from '../models/dashboard-metrics.model';
import { RecentOrderRow, RecentProductRow } from '../models/dashboard-row.model';

const orderStatusDisplays: Readonly<Record<string, StatusDisplay>> = {
  Draft: { label: 'Draft', tone: 'neutral' },
  PendingInventory: { label: 'Pending inventory', tone: 'info' },
  Confirmed: { label: 'Confirmed', tone: 'success' },
  Cancelled: { label: 'Cancelled', tone: 'danger' },
  InventoryRejected: { label: 'Inventory rejected', tone: 'danger' }
};

const productStatusDisplays: Readonly<Record<string, StatusDisplay>> = {
  Draft: { label: 'Draft', tone: 'neutral' },
  Published: { label: 'Published', tone: 'success' },
  Discontinued: { label: 'Discontinued', tone: 'warning' }
};

export function toDashboardMetrics(
  orders: PagedResult<DashboardOrderPayload>,
  products: PagedResult<DashboardProductPayload>,
  customers: PagedResult<unknown>): DashboardMetrics {
  return {
    orderTotal: orders.total,
    customerTotal: customers.total,
    pendingOrderCount: orders.items.filter(order => order.status === 'PendingInventory').length,
    revenue: orders.items.reduce((total, order) => total + order.total, 0),
    publishedProductCount: products.items
      .filter(product => product.status === 'Published' || product.isActive)
      .length,
    publishedVariantCount: products.items
      .flatMap(product => product.variants ?? [])
      .filter(variant => variant.status === 'Published')
      .length
  };
}

export function toRecentOrderRows(orders: DashboardOrderPayload[]): RecentOrderRow[] {
  return orders.map(order => ({
    id: order.id,
    customerName: order.customerName,
    status: toStatusDisplay(order.status, orderStatusDisplays),
    total: order.total,
    createdAt: order.createdAt
  }));
}

export function toRecentProductRows(products: DashboardProductPayload[]): RecentProductRow[] {
  return products.map(product => ({
    id: product.id,
    label: `${product.productCode || product.sku} - ${product.name}`,
    status: toStatusDisplay(product.status || (product.isActive ? 'Published' : 'Draft'), productStatusDisplays),
    minPrice: product.minPrice,
    maxPrice: product.maxPrice
  }));
}

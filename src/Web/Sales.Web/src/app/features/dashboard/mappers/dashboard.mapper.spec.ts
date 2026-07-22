import { PagedResult } from '../../../core/api/paged-result.model';
import type { DashboardOrderPayload, DashboardProductPayload } from '../api/dashboard-api.service';
import { toDashboardMetrics, toRecentOrderRows, toRecentProductRows } from './dashboard.mapper';

function page<T>(items: T[], total = items.length): PagedResult<T> {
  return { items, page: 1, pageSize: 20, total };
}

const orders: DashboardOrderPayload[] = [
  { id: 'o1', orderCode: 'ORD-1', customerName: 'A', status: 'PendingInventory', totalQuantity: 2, total: 100, createdAt: '2026-01-01T00:00:00Z' },
  { id: 'o2', orderCode: 'ORD-2', customerName: 'B', status: 'Confirmed', totalQuantity: 5, total: 250, createdAt: '2026-01-02T00:00:00Z' },
  { id: 'o3', orderCode: 'ORD-3', customerName: 'C', status: 'PendingInventory', totalQuantity: 1, total: 50, createdAt: '2026-01-03T00:00:00Z' }
];

const products: DashboardProductPayload[] = [
  {
    id: 'p1', sku: 'SKU1', productCode: 'PRD1', name: 'Shirt', status: 'Published', isActive: true,
    minPrice: 100, maxPrice: 200,
    variants: [{ status: 'Published' }, { status: 'Draft' }]
  },
  {
    id: 'p2', sku: 'SKU2', productCode: null, name: 'Hat', status: 'Draft', isActive: false,
    variants: [{ status: 'Published' }]
  }
];

describe('dashboard mapper', () => {
  it('derives metrics from count pages and period order totals', () => {
    const metrics = toDashboardMetrics({
      periodOrders: page(orders, 42),
      orderCount: page([], 42),
      pendingOrders: page([], 12),
      productCount: page([], 18),
      publishedProducts: page([], 9),
      customers: page([], 7)
    });

    expect(metrics.orderTotal).toBe(42);
    expect(metrics.customerTotal).toBe(7);
    expect(metrics.pendingOrderCount).toBe(12);
    expect(metrics.revenueToday).toBe(400);
    expect(metrics.productTotal).toBe(18);
    expect(metrics.publishedProductCount).toBe(9);
  });

  it('maps order rows with a resolved status display', () => {
    const rows = toRecentOrderRows(orders);

    expect(rows[0].status).toEqual({ label: 'Pending inventory', tone: 'info' });
    expect(rows[1].status).toEqual({ label: 'Confirmed', tone: 'success' });
    expect(rows[0].orderCode).toBe('ORD-1');
    expect(rows[0].customerName).toBe('A');
    expect(rows[0].totalQuantity).toBe(2);
  });

  it('maps product code by code when present and falls back to SKU', () => {
    const rows = toRecentProductRows(products);

    expect(rows[0].productCode).toBe('PRD1');
    expect(rows[0].productName).toBe('Shirt');
    expect(rows[1].productCode).toBe('SKU2');
    expect(rows[1].productName).toBe('Hat');
  });

  it('treats an active product with no status as Published', () => {
    const rows = toRecentProductRows([
      { id: 'p3', sku: 'SKU3', name: 'Belt', status: null, isActive: true }
    ]);

    expect(rows[0].status).toEqual({ label: 'Published', tone: 'success' });
  });
});

import { PagedResult } from '../../../core/api/paged-result.model';
import type { DashboardOrderPayload, DashboardProductPayload } from '../api/dashboard-api.service';
import { toDashboardMetrics, toRecentOrderRows, toRecentProductRows } from './dashboard.mapper';

function page<T>(items: T[], total = items.length): PagedResult<T> {
  return { items, page: 1, pageSize: 20, total };
}

const orders: DashboardOrderPayload[] = [
  { id: 'o1', customerName: 'A', status: 'PendingInventory', total: 100, createdAt: '2026-01-01T00:00:00Z' },
  { id: 'o2', customerName: 'B', status: 'Confirmed', total: 250, createdAt: '2026-01-02T00:00:00Z' },
  { id: 'o3', customerName: 'C', status: 'PendingInventory', total: 50, createdAt: '2026-01-03T00:00:00Z' }
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
  it('derives metrics from the sampled pages', () => {
    const metrics = toDashboardMetrics(page(orders, 42), page(products), page([], 7));

    expect(metrics.orderTotal).toBe(42);
    expect(metrics.customerTotal).toBe(7);
    expect(metrics.pendingOrderCount).toBe(2);
    expect(metrics.revenue).toBe(400);
    expect(metrics.publishedProductCount).toBe(1);
    expect(metrics.publishedVariantCount).toBe(2);
  });

  it('maps order rows with a resolved status display', () => {
    const rows = toRecentOrderRows(orders);

    expect(rows[0].status).toEqual({ label: 'Pending inventory', tone: 'info' });
    expect(rows[1].status).toEqual({ label: 'Confirmed', tone: 'success' });
    expect(rows[0].customerName).toBe('A');
  });

  it('labels products by code when present and falls back to SKU', () => {
    const rows = toRecentProductRows(products);

    expect(rows[0].label).toBe('PRD1 - Shirt');
    expect(rows[1].label).toBe('SKU2 - Hat');
  });

  it('treats an active product with no status as Published', () => {
    const rows = toRecentProductRows([
      { id: 'p3', sku: 'SKU3', name: 'Belt', status: null, isActive: true }
    ]);

    expect(rows[0].status).toEqual({ label: 'Published', tone: 'success' });
  });
});

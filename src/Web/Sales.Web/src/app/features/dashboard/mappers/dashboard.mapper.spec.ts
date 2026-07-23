import { DashboardSnapshotResponse } from '../api/responses/dashboard-snapshot.response';
import { toDashboardSnapshot } from './dashboard.mapper';

const snapshot: DashboardSnapshotResponse = {
  metrics: {
    orderTotal: 42,
    pendingOrderCount: 3,
    revenueToday: 150.25,
    customerTotal: 8,
    productTotal: 20,
    publishedProductCount: 15
  },
  inventory: {
    totalItems: 10,
    totalQuantity: 500,
    inStock: 7,
    lowStock: 2,
    outOfStock: 1,
    lowStockThreshold: 5
  },
  recentOrders: [
    {
      id: 'o1',
      orderCode: 'ORD-1',
      customerName: 'A',
      status: 'PendingInventory',
      totalQuantity: 2,
      total: 100,
      createdAt: '2026-01-01T00:00:00Z'
    }
  ],
  orderChart: [
    {
      status: 'Confirmed',
      total: 250,
      createdAt: '2026-01-02T00:00:00Z'
    }
  ],
  refreshedAt: '2026-07-23T15:30:00Z'
};

describe('dashboard mapper', () => {
  it('maps the dashboard snapshot response into dashboard view models', () => {
    const result = toDashboardSnapshot(snapshot);

    expect(result.metrics.orderTotal).toBe(42);
    expect(result.inventory.totalQuantity).toBe(500);
    expect(result.refreshedAt).toBe('2026-07-23T15:30:00Z');
    expect(result.recentOrders[0].status).toEqual({ label: 'Pending inventory', tone: 'info' });
    expect(result.recentOrders[0].orderCode).toBe('ORD-1');
    expect(result.chartOrders[0].status).toEqual({ label: 'Confirmed', tone: 'success' });
    expect(result.chartOrders[0].total).toBe(250);
  });
});

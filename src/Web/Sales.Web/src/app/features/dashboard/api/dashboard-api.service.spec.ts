import { TestBed } from '@angular/core/testing';
import { ApiClientService } from '../../../core/api/api-client.service';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { DashboardSnapshotResponse } from './responses/dashboard-snapshot.response';
import { DashboardApiService } from './dashboard-api.service';

describe('DashboardApiService', () => {
  it('loads the complete dashboard snapshot with one BFF request', async () => {
    const client = new FakeApiClientService();
    await TestBed.configureTestingModule({
      providers: [
        DashboardApiService,
        { provide: ApiClientService, useValue: client },
        { provide: ApiEndpointConfigurationService, useValue: { dashboardBase: () => '/dashboard-api' } }
      ]
    });

    const service = TestBed.inject(DashboardApiService);

    const result = await service.loadSnapshot();

    expect(client.calls).toEqual([{ baseUrl: '/dashboard-api', path: '/api/dashboard' }]);
    expect(result.metrics.orderTotal).toBe(42);
    expect(result.inventory.lowStock).toBe(2);
    expect(result.recentOrders[0].orderCode).toBe('ORD-1');
  });
});

class FakeApiClientService {
  readonly calls: Array<{ baseUrl: string; path: string }> = [];

  get<T>(baseUrl: string, path: string): Promise<T> {
    this.calls.push({ baseUrl, path });
    return Promise.resolve(snapshot as T);
  }
}

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
  orderChart: [],
  refreshedAt: '2026-07-23T15:30:00Z'
};

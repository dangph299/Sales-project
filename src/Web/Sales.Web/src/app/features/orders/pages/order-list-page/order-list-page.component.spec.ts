import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ApiResult } from '../../../../core/api/api-result.model';
import { PagedResult } from '../../../../core/api/paged-result.model';
import { CustomerLookupApiService } from '../../../common/api/customer-lookup-api.service';
import { ProductLookupApiService } from '../../../common/api/product-lookup-api.service';
import { OrderApiService } from '../../api/order-api.service';
import { OrderResponse } from '../../api/responses/order.response';
import { OrderStatusChangedNotification } from '../../models/order-status-changed.model';
import { OrderRealtimeService } from '../../services/order-realtime.service';
import { OrderListPageComponent } from './order-list-page.component';

// Behavioural coverage carried over from the pre-refactor
// pages/orders-page.component.spec.ts. The fakes now stand in for
// OrderApiService / OrderRealtimeService instead of the former god ApiService.
describe('OrderListPageComponent realtime behavior', () => {
  let fixture: ComponentFixture<OrderListPageComponent>;
  let orderApi: FakeOrderApiService;
  let realtime: FakeOrderRealtimeService;

  beforeEach(async () => {
    orderApi = new FakeOrderApiService();
    realtime = new FakeOrderRealtimeService();

    await TestBed.configureTestingModule({
      imports: [OrderListPageComponent],
      providers: [
        provideNoopAnimations(),
        { provide: OrderApiService, useValue: orderApi },
        { provide: OrderRealtimeService, useValue: realtime },
        { provide: CustomerLookupApiService, useValue: new FakeLookupApiService() },
        { provide: ProductLookupApiService, useValue: new FakeLookupApiService() }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(OrderListPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => {
    fixture.destroy();
  });

  it('subscribes and unsubscribes the order list group with the component lifecycle', () => {
    expect(realtime.orderListSubscribeCount).toBe(1);

    fixture.destroy();

    expect(realtime.orderListUnsubscribeCount).toBe(1);
  });

  it('subscribes to the selected order detail group', async () => {
    await fixture.componentInstance.loadOrderDetail(orderApi.order.id);

    expect(realtime.subscribedOrderIds).toEqual([orderApi.order.id]);
  });

  it('does not refetch current detail for another order status event', async () => {
    await fixture.componentInstance.loadOrderDetail(orderApi.order.id);
    orderApi.getByIdCount = 0;

    realtime.emitStatusChanged({
      orderId: '11111111-1111-1111-1111-111111111111',
      previousStatus: 'PendingInventory',
      currentStatus: 'Confirmed',
      changedAt: new Date().toISOString(),
      version: 2
    });
    await delay(350);

    expect(orderApi.getByIdCount).toBe(0);
  });

  it('refetches current detail when its order status event arrives', async () => {
    await fixture.componentInstance.loadOrderDetail(orderApi.order.id);
    orderApi.getByIdCount = 0;

    realtime.emitStatusChanged({
      orderId: orderApi.order.id,
      previousStatus: 'PendingInventory',
      currentStatus: 'Confirmed',
      changedAt: new Date().toISOString(),
      version: 2
    });
    await delay(350);

    expect(orderApi.getByIdCount).toBe(1);
  });
});

class FakeOrderApiService {
  getByIdCount = 0;

  readonly order: OrderResponse = {
    id: '22222222-2222-2222-2222-222222222222',
    customerId: '33333333-3333-3333-3333-333333333333',
    customerName: 'Customer',
    customerPhone: '0901234567',
    createdAt: new Date().toISOString(),
    status: 'PendingInventory',
    totalQuantity: 1,
    total: 100,
    version: 1,
    updatedAt: new Date().toISOString(),
    rejectionReason: null,
    lines: []
  };

  search(): Promise<PagedResult<OrderResponse>> {
    return Promise.resolve(emptyPage<OrderResponse>());
  }

  getById(): Promise<ApiResult<OrderResponse>> {
    this.getByIdCount += 1;
    return Promise.resolve({ body: this.order, etag: 'etag-1', status: 200 });
  }

  getReservation(): Promise<null> {
    return Promise.resolve(null);
  }
}

class FakeLookupApiService {
  search(): Promise<PagedResult<never>> {
    return Promise.resolve(emptyPage<never>());
  }
}

class FakeOrderRealtimeService {
  readonly state = signal<'Disconnected' | 'Connecting' | 'Connected' | 'Reconnecting'>('Connected');

  orderListSubscribeCount = 0;
  orderListUnsubscribeCount = 0;
  readonly subscribedOrderIds: string[] = [];

  private statusChangedHandler: ((notification: OrderStatusChangedNotification) => void) | null = null;

  onStatusChanged(handler: (notification: OrderStatusChangedNotification) => void): () => void {
    this.statusChangedHandler = handler;
    return () => {
      this.statusChangedHandler = null;
    };
  }

  onReconnected(): () => void {
    return () => undefined;
  }

  subscribeToOrderList(): Promise<void> {
    this.orderListSubscribeCount += 1;
    return Promise.resolve();
  }

  unsubscribeFromOrderList(): Promise<void> {
    this.orderListUnsubscribeCount += 1;
    return Promise.resolve();
  }

  subscribeToOrder(orderId: string): Promise<void> {
    this.subscribedOrderIds.push(orderId);
    return Promise.resolve();
  }

  unsubscribeFromOrder(orderId: string): Promise<void> {
    const index = this.subscribedOrderIds.indexOf(orderId);
    if (index >= 0) {
      this.subscribedOrderIds.splice(index, 1);
    }

    return Promise.resolve();
  }

  emitStatusChanged(notification: OrderStatusChangedNotification): void {
    this.statusChangedHandler?.(notification);
  }
}

function emptyPage<T>(): PagedResult<T> {
  return { items: [], page: 1, pageSize: 20, total: 0 };
}

function delay(milliseconds: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, milliseconds));
}

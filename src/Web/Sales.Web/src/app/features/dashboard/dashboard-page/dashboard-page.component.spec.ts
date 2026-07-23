import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import {
  AppstoreOutline,
  CheckCircleOutline,
  CloseCircleOutline,
  DatabaseOutline,
  DollarOutline,
  InboxOutline,
  PlusOutline,
  ReloadOutline,
  ShoppingCartOutline,
  ShoppingOutline,
  TeamOutline,
  UserAddOutline,
  WarningOutline
} from '@ant-design/icons-angular/icons';
import { DashboardApiService } from '../api/dashboard-api.service';
import { DashboardSnapshot } from '../models/dashboard-snapshot.model';
import { DashboardPageComponent } from './dashboard-page.component';

describe('DashboardPageComponent', () => {
  let fixture: ComponentFixture<DashboardPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardPageComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([]),
        provideNzIcons([
          AppstoreOutline,
          CheckCircleOutline,
          CloseCircleOutline,
          DatabaseOutline,
          DollarOutline,
          InboxOutline,
          PlusOutline,
          ReloadOutline,
          ShoppingCartOutline,
          ShoppingOutline,
          TeamOutline,
          UserAddOutline,
          WarningOutline
        ]),
        { provide: DashboardApiService, useValue: new FakeDashboardApiService() }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
  });

  it('loads the inventory summary from the dashboard snapshot', () => {
    expect(fixture.componentInstance.inventory()).toEqual(snapshot.inventory);
  });

  it('renders the inventory overview widget and removes latest products', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Inventory Overview');
    expect(text).toContain('Total Inventory Items');
    expect(text).toContain('Total Stock Quantity');
    expect(text).toContain('In Stock');
    expect(text).toContain('Low Stock');
    expect(text).toContain('Out Of Stock');
    expect(text).not.toContain('Latest Products');
  });

  it('renders deep links for stock-state inventory filters', () => {
    const anchors = Array.from(fixture.nativeElement.querySelectorAll('a'))
      .map(anchor => (anchor as HTMLAnchorElement).getAttribute('href'));

    expect(anchors).toContain('/inventory?stock=in-stock');
    expect(anchors).toContain('/inventory?stock=low');
    expect(anchors).toContain('/inventory?stock=out');
  });
});

class FakeDashboardApiService {
  loadSnapshot(): Promise<DashboardSnapshot> {
    return Promise.resolve(snapshot);
  }
}

const snapshot: DashboardSnapshot = {
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
  recentOrders: [],
  chartOrders: [],
  refreshedAt: '2026-07-23T15:30:00Z'
};

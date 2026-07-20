import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiClientError, ApiResponseReader } from '../api-client-result';
import { ApiService } from '../api.service';
import { CustomerDto, OrderDto, ProductDto } from '../models';
import { formatDateTime, formatMoney } from '../shared/display-formatters';
import { MoneyDisplayComponent } from '../shared/money-display.component';
import { PageStateComponent } from '../shared/page-state.component';
import { StatusBadgeComponent } from '../shared/status-badge.component';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterLink, MoneyDisplayComponent, PageStateComponent, StatusBadgeComponent],
  template: `
    <section class="page-header">
      <div>
        <p class="eyebrow">Overview</p>
        <h1>Dashboard</h1>
        <p>Quan sat nhanh don hang, khach hang, san pham va canh bao ton kho dua tren API hien co.</p>
      </div>
      <button type="button" (click)="load()">Refresh</button>
    </section>

    <app-page-state [loading]="loading()" [errorMessage]="errorMessage()" (retry)="load()"></app-page-state>

    <section class="kpi-grid" *ngIf="!loading() && !errorMessage()">
      <article class="kpi-card">
        <span>Orders</span>
        <strong>{{ orderTotal() }}</strong>
        <small>Recent page total</small>
      </article>
      <article class="kpi-card">
        <span>Pending</span>
        <strong>{{ pendingOrderCount() }}</strong>
        <small>Pending inventory</small>
      </article>
      <article class="kpi-card">
        <span>Revenue</span>
        <strong>{{ formatMoney(revenue()) }}</strong>
        <small>Current page</small>
      </article>
      <article class="kpi-card">
        <span>Customers</span>
        <strong>{{ customerTotal() }}</strong>
        <small>Search API total</small>
      </article>
      <article class="kpi-card">
        <span>Published products</span>
        <strong>{{ publishedProductCount() }}</strong>
        <small>Current page</small>
      </article>
      <article class="kpi-card">
        <span>Published variants</span>
        <strong>{{ publishedVariantCount() }}</strong>
        <small>Current page</small>
      </article>
    </section>

    <section class="content-grid two" *ngIf="!loading() && !errorMessage()">
      <article class="panel-card">
        <div class="section-title">
          <h2>Recent Orders</h2>
          <a routerLink="/orders">View all</a>
        </div>
        <table class="data-table" *ngIf="orders().length > 0">
          <thead>
            <tr>
              <th>Customer</th>
              <th>Status</th>
              <th>Total</th>
              <th>Created</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let order of orders()">
              <td>{{ order.customerName }}</td>
              <td><app-status-badge [status]="order.status"></app-status-badge></td>
              <td><app-money-display [amount]="order.total"></app-money-display></td>
              <td>{{ formatDateTime(order.createdAt) }}</td>
            </tr>
          </tbody>
        </table>
        <app-page-state [empty]="orders().length === 0" emptyTitle="Chua co don hang" emptyText="Tao order dau tien tu man hinh Orders."></app-page-state>
      </article>

      <article class="panel-card">
        <div class="section-title">
          <h2>Recent Products</h2>
          <a routerLink="/products">View catalog</a>
        </div>
        <table class="data-table" *ngIf="products().length > 0">
          <thead>
            <tr>
              <th>Product</th>
              <th>Status</th>
              <th>Price range</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let product of products()">
              <td>{{ product.productCode || product.sku }} - {{ product.name }}</td>
              <td><app-status-badge [status]="product.status || (product.isActive ? 'Published' : 'Draft')"></app-status-badge></td>
              <td><app-money-display [minPrice]="product.minPrice" [maxPrice]="product.maxPrice"></app-money-display></td>
            </tr>
          </tbody>
        </table>
        <app-page-state [empty]="products().length === 0" emptyTitle="Chua co san pham" emptyText="Tao product va variant trong Catalog."></app-page-state>
      </article>
    </section>

    <section class="panel-card" *ngIf="!loading() && !errorMessage()">
      <div class="section-title">
        <h2>System Alerts</h2>
      </div>
      <ul class="plain-list">
        <li>Dashboard dang tong hop bang list endpoint page nho; nen bo sung backend dashboard aggregation API.</li>
        <li>Operations APIs cho outbox/inbox/dead letters chua duoc expose nen khong hien thi menu thao tac.</li>
      </ul>
    </section>
  `
})
export class DashboardPageComponent implements OnInit {
  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly orders = signal<OrderDto[]>([]);
  readonly products = signal<ProductDto[]>([]);
  readonly customers = signal<CustomerDto[]>([]);
  readonly orderTotal = signal(0);
  readonly customerTotal = signal(0);

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');
    try {
      const [ordersPage, productsPage, customersPage] = await Promise.all([
        this.api.searchOrders({ page: 1, pageSize: 5 }),
        this.api.searchProducts({ page: 1, pageSize: 8 }),
        this.api.searchCustomers({ page: 1, pageSize: 1 })
      ]);
      this.orders.set(ordersPage.items);
      this.products.set(productsPage.items);
      this.customers.set(customersPage.items);
      this.orderTotal.set(ordersPage.total);
      this.customerTotal.set(customersPage.total);
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  pendingOrderCount(): number {
    return this.orders().filter(order => order.status === 'PendingInventory').length;
  }

  revenue(): number {
    return this.orders().reduce((totalAmount, order) => totalAmount + order.total, 0);
  }

  publishedProductCount(): number {
    return this.products().filter(product => product.status === 'Published' || product.isActive).length;
  }

  publishedVariantCount(): number {
    return this.products()
      .flatMap(product => product.variants ?? [])
      .filter(productVariant => productVariant.status === 'Published')
      .length;
  }

  formatMoney(amount: number): string {
    return formatMoney(amount);
  }

  formatDateTime(text: string): string {
    return formatDateTime(text);
  }

  private describeError(error: unknown): string {
    if (error instanceof ApiClientError) {
      return ApiResponseReader.formatFailure(error.result);
    }

    return error instanceof Error ? error.message : 'Request failed.';
  }
}

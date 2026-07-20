import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClientError, ApiResponseReader } from '../api-client-result';
import { ApiService } from '../api.service';
import { CustomerDto, OrderDto, OrderLineInput, ProductDto, ProductVariantDto, ReservationDto } from '../models';
import { formatDateTime, formatMoney } from '../shared/display-formatters';
import { MoneyDisplayComponent } from '../shared/money-display.component';
import { PageStateComponent } from '../shared/page-state.component';
import { StatusBadgeComponent } from '../shared/status-badge.component';

interface CartLine {
  product: ProductDto;
  variant: ProductVariantDto;
  quantity: number;
  discountPercent: number;
}

@Component({
  selector: 'app-orders-page',
  standalone: true,
  imports: [CommonModule, FormsModule, MoneyDisplayComponent, PageStateComponent, StatusBadgeComponent],
  template: `
    <section class="page-header">
      <div>
        <p class="eyebrow">Sales</p>
        <h1>Orders</h1>
        <p>Tao order bang Customer Normal va ProductVariant Published. Gia hien thi lay tu ProductVariant.</p>
      </div>
      <button type="button" (click)="loadOrders()">Refresh orders</button>
    </section>

    <app-page-state [loading]="loading()" [errorMessage]="errorMessage()" (retry)="loadOrders()"></app-page-state>

    <section class="content-grid two" *ngIf="!loading()">
      <article class="panel-card">
        <div class="section-title">
          <h2>Customer Selection</h2>
          <button type="button" class="secondary" (click)="loadCustomers()">Search</button>
        </div>
        <section class="toolbar compact">
          <label>Name
            <input name="orderCustomerName" [(ngModel)]="customerSearch" (keyup.enter)="loadCustomers()">
          </label>
          <label>Phone
            <input name="orderCustomerPhone" [(ngModel)]="customerPhoneSearch" (keyup.enter)="loadCustomers()">
          </label>
        </section>
        <div class="list-panel">
          <button type="button" *ngFor="let customer of customers()" (click)="selectCustomer(customer)" [class.selected]="selectedCustomer()?.id === customer.id" [disabled]="customer.status !== 'Normal'">
            <span>{{ customer.customerCode || customer.id.slice(0, 8) }} - {{ customer.name }}</span>
            <app-status-badge [status]="customer.status || 'Normal'"></app-status-badge>
          </button>
        </div>
      </article>

      <article class="panel-card">
        <div class="section-title">
          <h2>Variant Selection</h2>
          <button type="button" class="secondary" (click)="loadProducts()">Search</button>
        </div>
        <section class="toolbar compact">
          <label>Product / SKU
            <input name="orderProductSearch" [(ngModel)]="productSearch" (keyup.enter)="loadProducts()">
          </label>
        </section>
        <div class="list-panel">
          <button type="button" *ngFor="let option of sellableVariants()" (click)="addCartLine(option.product, option.variant)">
            <span>{{ option.variant.sku }} - {{ option.product.name }}</span>
            <span>{{ formatMoney(option.variant.price) }}</span>
          </button>
        </div>
      </article>
    </section>

    <section class="content-grid two">
      <article class="panel-card wide">
        <div class="section-title">
          <h2>Cart</h2>
          <span>{{ cartLines().length }} lines</span>
        </div>
        <app-page-state [empty]="cartLines().length === 0" emptyTitle="Gio hang trong" emptyText="Chon Customer Normal va ProductVariant Published de tao order."></app-page-state>
        <div class="table-wrap" *ngIf="cartLines().length > 0">
          <table class="data-table">
            <thead>
              <tr>
                <th>Product</th>
                <th>SKU</th>
                <th>Color</th>
                <th>Size</th>
                <th>Unit price</th>
                <th>Quantity</th>
                <th>Discount %</th>
                <th>Line total</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let cartLine of cartLines()">
                <td>{{ cartLine.product.name }}</td>
                <td>{{ cartLine.variant.sku }}</td>
                <td>{{ cartLine.variant.color?.name || '-' }}</td>
                <td>{{ cartLine.variant.size?.code || '-' }}</td>
                <td><app-money-display [amount]="cartLine.variant.price"></app-money-display></td>
                <td><input class="table-input" type="number" min="1" [ngModel]="cartLine.quantity" (ngModelChange)="updateQuantity(cartLine.variant.id, +$event)"></td>
                <td><input class="table-input" type="number" min="0" max="100" [ngModel]="cartLine.discountPercent" (ngModelChange)="updateDiscount(cartLine.variant.id, +$event)"></td>
                <td>{{ formatMoney(lineTotal(cartLine)) }}</td>
                <td><button type="button" class="danger small" (click)="removeCartLine(cartLine.variant.id)">Remove</button></td>
              </tr>
            </tbody>
          </table>
        </div>
        <section class="summary-strip">
          <span>Subtotal: {{ formatMoney(subtotal()) }}</span>
          <strong>Grand total: {{ formatMoney(grandTotal()) }}</strong>
          <button type="button" (click)="createOrder()" [disabled]="saving() || !selectedCustomer() || cartLines().length === 0">Create Order</button>
        </section>
      </article>

      <article class="panel-card">
        <div class="section-title">
          <h2>Order List</h2>
          <button type="button" class="secondary" (click)="loadOrders()">Search</button>
        </div>
        <section class="toolbar compact">
          <label>Customer
            <input name="orderCustomerFilter" [(ngModel)]="orderCustomerFilter" (keyup.enter)="loadOrders()">
          </label>
        </section>
        <div class="list-panel">
          <button type="button" *ngFor="let order of orders()" (click)="loadOrderDetail(order.id)" [class.selected]="currentOrder()?.id === order.id">
            <span>{{ order.customerName }} - {{ formatMoney(order.total) }}</span>
            <app-status-badge [status]="order.status"></app-status-badge>
          </button>
        </div>
      </article>
    </section>

    <section class="panel-card" *ngIf="currentOrder() as order">
      <div class="section-title">
        <div>
          <h2>Order Detail</h2>
          <p>{{ order.id }} - ETag {{ currentEtag() || '-' }}</p>
        </div>
        <app-status-badge [status]="order.status"></app-status-badge>
      </div>
      <dl class="detail-grid">
        <dt>Customer</dt><dd>{{ order.customerName }} - {{ order.customerPhone }}</dd>
        <dt>Created</dt><dd>{{ formatDateTime(order.createdAt) }}</dd>
        <dt>Updated</dt><dd>{{ formatDateTime(order.updatedAt) }}</dd>
        <dt>Total</dt><dd><app-money-display [amount]="order.total"></app-money-display></dd>
      </dl>
      <div class="actions wrap">
        <button type="button" (click)="confirmOrder()" [disabled]="order.status !== 'Draft'">Confirm</button>
        <button type="button" class="warning" (click)="undoConfirmOrder()" [disabled]="order.status !== 'Confirmed'">Undo Confirm</button>
        <button type="button" class="danger" (click)="cancelOrder()" [disabled]="order.status === 'Cancelled'">Cancel</button>
        <button type="button" class="secondary" (click)="loadReservation(order.id)">Load Reservation</button>
      </div>
      <div class="table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th>ProductCode</th>
              <th>ProductName</th>
              <th>SKU</th>
              <th>Color</th>
              <th>Size</th>
              <th>UnitPrice</th>
              <th>Quantity</th>
              <th>Discount</th>
              <th>LineTotal</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let line of order.lines">
              <td>{{ line.productCode || '-' }}</td>
              <td>{{ line.productName }}</td>
              <td>{{ line.sku }}</td>
              <td>{{ line.colorName || line.colorCode || '-' }}</td>
              <td>{{ line.sizeCode || '-' }}</td>
              <td><app-money-display [amount]="line.unitPrice"></app-money-display></td>
              <td>{{ line.quantity }}</td>
              <td>{{ line.discountPercent }}%</td>
              <td><app-money-display [amount]="line.lineTotal"></app-money-display></td>
            </tr>
          </tbody>
        </table>
      </div>
      <pre *ngIf="reservationText()">{{ reservationText() }}</pre>
    </section>
  `
})
export class OrdersPageComponent implements OnInit {
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly customers = signal<CustomerDto[]>([]);
  readonly products = signal<ProductDto[]>([]);
  readonly orders = signal<OrderDto[]>([]);
  readonly cartLines = signal<CartLine[]>([]);
  readonly selectedCustomer = signal<CustomerDto | null>(null);
  readonly currentOrder = signal<OrderDto | null>(null);
  readonly currentEtag = signal<string | null>(null);
  readonly reservationText = signal('');
  customerSearch = '';
  customerPhoneSearch = '';
  productSearch = '';
  orderCustomerFilter = '';

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    void Promise.all([this.loadCustomers(), this.loadProducts(), this.loadOrders()]);
  }

  async loadCustomers(): Promise<void> {
    try {
      const customerPage = await this.api.searchCustomers({ name: this.customerSearch, phone: this.customerPhoneSearch, page: 1, pageSize: 10 });
      this.customers.set(customerPage.items);
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    }
  }

  async loadProducts(): Promise<void> {
    try {
      const productPage = await this.api.searchProducts({ name: this.productSearch, sku: this.productSearch, status: 'Published', page: 1, pageSize: 10 });
      this.products.set(productPage.items);
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    }
  }

  async loadOrders(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');
    try {
      const orderPage = await this.api.searchOrders({ customer: this.orderCustomerFilter, page: 1, pageSize: 20 });
      this.orders.set(orderPage.items);
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  selectCustomer(customer: CustomerDto): void {
    this.selectedCustomer.set(customer);
  }

  sellableVariants(): { product: ProductDto; variant: ProductVariantDto }[] {
    return this.products()
      .filter(product => product.status === 'Published' || product.isActive)
      .flatMap(product => (product.variants ?? [])
        .filter(variant => variant.status === 'Published')
        .map(variant => ({ product, variant })));
  }

  addCartLine(product: ProductDto, variant: ProductVariantDto): void {
    const existingLine = this.cartLines().find(cartLine => cartLine.variant.id === variant.id);
    if (existingLine) {
      this.updateQuantity(variant.id, existingLine.quantity + 1);
      return;
    }

    this.cartLines.set([...this.cartLines(), { product, variant, quantity: 1, discountPercent: 0 }]);
  }

  updateQuantity(productVariantId: string, quantity: number): void {
    this.cartLines.set(this.cartLines().map(cartLine =>
      cartLine.variant.id === productVariantId ? { ...cartLine, quantity: Math.max(1, quantity || 1) } : cartLine));
  }

  updateDiscount(productVariantId: string, discountPercent: number): void {
    const normalizedDiscount = Math.min(100, Math.max(0, discountPercent || 0));
    this.cartLines.set(this.cartLines().map(cartLine =>
      cartLine.variant.id === productVariantId ? { ...cartLine, discountPercent: normalizedDiscount } : cartLine));
  }

  removeCartLine(productVariantId: string): void {
    this.cartLines.set(this.cartLines().filter(cartLine => cartLine.variant.id !== productVariantId));
  }

  async createOrder(): Promise<void> {
    const customer = this.selectedCustomer();
    if (!customer || customer.status !== 'Normal') {
      this.errorMessage.set('Only Normal customers can create orders.');
      return;
    }

    this.saving.set(true);
    try {
      const orderLines: OrderLineInput[] = this.cartLines().map(cartLine => ({
        productVariantId: cartLine.variant.id,
        quantity: cartLine.quantity,
        discountPercent: cartLine.discountPercent
      }));
      const orderResult = await this.api.createOrder(customer.id, orderLines);
      this.currentOrder.set(orderResult.body);
      this.currentEtag.set(orderResult.etag ?? null);
      this.cartLines.set([]);
      await this.loadOrders();
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    } finally {
      this.saving.set(false);
    }
  }

  async loadOrderDetail(orderId: string): Promise<void> {
    try {
      const orderResult = await this.api.getOrder(orderId);
      this.currentOrder.set(orderResult.body);
      this.currentEtag.set(orderResult.etag ?? null);
      this.reservationText.set('');
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    }
  }

  async confirmOrder(): Promise<void> {
    await this.transitionOrder('confirm');
  }

  async undoConfirmOrder(): Promise<void> {
    await this.transitionOrder('undo');
  }

  async cancelOrder(): Promise<void> {
    if (!confirm('Cancel Order\n\nOrder se bi huy va giu lai lich su. Ban co chac chan tiep tuc?')) {
      return;
    }

    await this.transitionOrder('cancel');
  }

  async loadReservation(orderId: string): Promise<void> {
    try {
      const reservation = await this.api.getReservation(orderId);
      this.reservationText.set(reservation ? this.formatReservation(reservation) : 'No reservation found.');
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    }
  }

  lineTotal(cartLine: CartLine): number {
    return Math.round(cartLine.variant.price * cartLine.quantity * (1 - cartLine.discountPercent / 100));
  }

  subtotal(): number {
    return this.cartLines().reduce((subtotal, cartLine) => subtotal + cartLine.variant.price * cartLine.quantity, 0);
  }

  grandTotal(): number {
    return this.cartLines().reduce((totalAmount, cartLine) => totalAmount + this.lineTotal(cartLine), 0);
  }

  formatMoney(amount: number): string {
    return formatMoney(amount);
  }

  formatDateTime(text: string): string {
    return formatDateTime(text);
  }

  private async transitionOrder(action: 'confirm' | 'cancel' | 'undo'): Promise<void> {
    const order = this.currentOrder();
    const etag = this.currentEtag();
    if (!order || !etag) {
      return;
    }

    this.saving.set(true);
    try {
      const orderResult = action === 'confirm'
        ? await this.api.confirmOrder(order.id, etag)
        : action === 'cancel'
          ? await this.api.cancelOrder(order.id, etag)
          : await this.api.undoConfirmOrder(order.id, etag);
      this.currentOrder.set(orderResult.body);
      this.currentEtag.set(orderResult.etag ?? null);
      await this.loadOrders();
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    } finally {
      this.saving.set(false);
    }
  }

  private formatReservation(reservation: ReservationDto): string {
    return JSON.stringify(reservation, null, 2);
  }

  private describeError(error: unknown): string {
    if (error instanceof ApiClientError) {
      return ApiResponseReader.formatFailure(error.result);
    }

    return error instanceof Error ? error.message : 'Request failed.';
  }
}

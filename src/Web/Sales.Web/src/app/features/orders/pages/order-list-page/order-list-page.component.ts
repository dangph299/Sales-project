import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzNotificationService } from 'ng-zorro-antd/notification';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTableModule } from 'ng-zorro-antd/table';
import { PageStateComponent } from '../../../../shared/components/page-state/page-state.component';
import { StatusTagComponent } from '../../../../shared/components/status-tag/status-tag.component';
import { DateTimePipe } from '../../../../shared/pipes/date-time.pipe';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { confirmAction } from '../../../../shared/utilities/confirm-action';
import { describeApiError } from '../../../../shared/utilities/describe-api-error';
import { formatDateTime } from '../../../../shared/utilities/display-formatters';
import { CustomerLookupApiService } from '../../../common/api/customer-lookup-api.service';
import { ProductLookupApiService } from '../../../common/api/product-lookup-api.service';
import { CustomerLookupResponse } from '../../../common/contracts/customer-lookup.response';
import { customerLookupStatusDisplay } from '../../../common/constants/customer-lookup-status';
import {
  ProductLookupResponse,
  ProductVariantLookupResponse
} from '../../../common/contracts/product-lookup.response';
import { OrderApiService } from '../../api/order-api.service';
import { OrderLineRequest } from '../../api/requests/order-line.request';
import { OrderResponse } from '../../api/responses/order.response';
import { OrderLineEditorComponent } from '../../components/order-line-editor/order-line-editor.component';
import { OrderStatus, describeOrderStatusChange, orderStatusDisplay } from '../../constants/order-status';
import { CartLine, cartGrandTotal, normalizeQuantity } from '../../models/cart-line.model';
import { OrderStatusChangedNotification } from '../../models/order-status-changed.model';
import { OrderRealtimeService } from '../../services/order-realtime.service';

const orderListRefreshDelayMs = 500;
const orderDetailRefreshDelayMs = 300;
const pendingPollIntervalMs = 7_000;
const pendingPollWindowMs = 2 * 60_000;

type OrderSortKey = 'orderNumber' | 'customer' | 'phone' | 'createdAt' | 'total' | 'status' | 'updatedAt';
type SortDirection = 'asc' | 'desc';

@Component({
  selector: 'app-order-list-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageStateComponent,
    StatusTagComponent,
    OrderLineEditorComponent,
    DateTimePipe,
    MoneyPipe,
    NzButtonModule,
    NzCardModule,
    NzDescriptionsModule,
    NzDropDownModule,
    NzInputModule,
    NzMenuModule,
    NzModalModule,
    NzSelectModule,
    NzTableModule
  ],
  templateUrl: './order-list-page.component.html',
  styleUrl: './order-list-page.component.scss'
})
export class OrderListPageComponent implements OnInit, OnDestroy {
  private readonly orderApi = inject(OrderApiService);
  private readonly customerLookup = inject(CustomerLookupApiService);
  private readonly productLookup = inject(ProductLookupApiService);
  readonly orderRealtime = inject(OrderRealtimeService);
  private readonly modal = inject(NzModalService);
  private readonly notificationService = inject(NzNotificationService);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly realtimeMessage = signal('');
  readonly waitingSinceText = signal('');
  readonly customers = signal<CustomerLookupResponse[]>([]);
  readonly products = signal<ProductLookupResponse[]>([]);
  readonly orders = signal<OrderResponse[]>([]);
  readonly total = signal(0);
  readonly cartLines = signal<CartLine[]>([]);
  readonly selectedCustomer = signal<CustomerLookupResponse | null>(null);
  readonly currentOrder = signal<OrderResponse | null>(null);
  readonly currentEtag = signal<string | null>(null);
  readonly reservationText = signal('');
  readonly orderModalOpen = signal(false);
  readonly modalMode = signal<'create' | 'edit'>('create');
  readonly formDirty = signal(false);
  readonly formErrors = signal<{ customer?: string; lines?: string }>({});

  customerSearch = '';
  customerPhoneSearch = '';
  productSearch = '';
  orderSearch = '';
  /** `null` is what nzAllowClear writes back; the API client drops empty values from the query. */
  statusFilter: OrderStatus | '' | null = '';
  fromDate = '';
  toDate = '';
  pageIndex = 1;
  pageSize = 20;
  readonly sortKey = signal<OrderSortKey>('updatedAt');
  readonly sortDirection = signal<SortDirection>('desc');

  readonly statusDisplay = orderStatusDisplay;
  readonly customerDisplay = customerLookupStatusDisplay;
  readonly orderStatuses: { value: OrderStatus; label: string }[] = [
    { value: 'Draft', label: 'Draft' },
    { value: 'PendingInventory', label: 'Pending Inventory' },
    { value: 'Confirmed', label: 'Confirmed' },
    { value: 'Cancelled', label: 'Cancelled' },
    { value: 'InventoryRejected', label: 'Inventory Rejected' }
  ];
  readonly pageSizeOptions = [10, 20, 50];
  readonly modalTitle = computed(() => this.modalMode() === 'create' ? 'Create Order' : `Edit ${this.currentOrderNumber()}`);
  readonly modalGrandTotal = computed(() => cartGrandTotal(this.cartLines()));
  // Status and customer are filtered server-side, so they apply across every page and agree with
  // total(). Only an order-number term is matched here, because the API has no filter for it —
  // re-testing a customer term locally would discard server matches whose formatting differs
  // (the API strips non-digits from a phone, the stored value keeps none of the user's separators).
  readonly displayedOrders = computed(() => {
    const orderNumberTerm = this.clientOrderNumberFilter();
    const filtered = orderNumberTerm
      ? this.orders().filter(order => this.orderNumber(order).toLowerCase().includes(orderNumberTerm))
      : this.orders();

    return [...filtered].sort((left, right) => this.compareOrders(left, right));
  });

  private currentSubscribedOrderId: string | null = null;
  private removeStatusChangedHandler: (() => void) | null = null;
  private removeReconnectedHandler: (() => void) | null = null;
  private orderListRefreshTimer: ReturnType<typeof setTimeout> | null = null;
  private detailRefreshTimer: ReturnType<typeof setTimeout> | null = null;
  private pendingPollTimer: ReturnType<typeof setInterval> | null = null;
  private pendingPollDeadline = 0;
  private lastDetailNotificationKey = '';

  private readonly visibilityHandler = () => {
    if (document.visibilityState === 'visible') {
      void this.refreshCurrentOrder();
      void this.loadOrders();
    }
  };

  constructor() {
    effect(() => {
      const order = this.currentOrder();
      const realtimeState = this.orderRealtime.state();

      if (order?.status !== 'PendingInventory' || realtimeState === 'Connected') {
        this.stopPendingPolling();
        return;
      }

      this.ensurePendingPolling();
    });
  }

  ngOnInit(): void {
    void Promise.all([this.loadCustomers(), this.loadProducts(), this.loadOrders()]);
    this.removeStatusChangedHandler = this.orderRealtime.onStatusChanged(
      notification => this.handleOrderStatusChanged(notification));
    this.removeReconnectedHandler = this.orderRealtime.onReconnected(() => {
      void this.refreshCurrentOrder();
      void this.loadOrders();
    });
    document.addEventListener('visibilitychange', this.visibilityHandler);
    void this.orderRealtime.subscribeToOrderList();
  }

  ngOnDestroy(): void {
    if (this.currentSubscribedOrderId) {
      void this.orderRealtime.unsubscribeFromOrder(this.currentSubscribedOrderId);
    }

    void this.orderRealtime.unsubscribeFromOrderList();
    this.removeStatusChangedHandler?.();
    this.removeReconnectedHandler?.();
    document.removeEventListener('visibilitychange', this.visibilityHandler);
    this.clearRefreshTimers();
    this.stopPendingPolling();
  }

  async loadCustomers(): Promise<void> {
    try {
      const page = await this.customerLookup.search({
        name: this.customerSearch,
        phone: this.customerPhoneSearch,
        page: 1,
        pageSize: 10
      });
      this.customers.set(page.items);
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    }
  }

  async loadProducts(): Promise<void> {
    try {
      // The API ANDs its filters and matches `sku` exactly, so sending one term as both `name` and
      // `sku` can only ever match a product whose name contains its own full SKU. Route the term to
      // whichever filter it looks like instead. A SKU is PRODUCTCODE-COLOR-SIZE, so it needs both
      // separators and no whitespace — a single hyphen means a name like "T-Shirt", which must not
      // be sent to the exact-match sku filter.
      const term = this.productSearch.trim();
      const searchesBySku = /^[^\s-]+-[^\s-]+-[^\s-]+$/.test(term);
      const page = await this.productLookup.search({
        name: searchesBySku ? '' : term,
        sku: searchesBySku ? term : '',
        status: 'Published',
        page: 1,
        pageSize: 10
      });
      this.products.set(page.items);
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    }
  }

  async loadOrders(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');
    try {
      const page = await this.orderApi.search({
        customer: this.serverCustomerFilter(),
        status: this.statusFilter,
        from: this.fromDate || undefined,
        to: this.toDate || undefined,
        page: this.pageIndex,
        pageSize: this.pageSize
      });
      this.orders.set(page.items);
      this.total.set(page.total);
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.loading.set(false);
    }
  }

  selectCustomer(customer: CustomerLookupResponse): void {
    this.selectedCustomer.set(customer);
    this.formDirty.set(true);
    this.formErrors.set({ ...this.formErrors(), customer: undefined });
  }

  sellableVariants(): { product: ProductLookupResponse; variant: ProductVariantLookupResponse }[] {
    return this.products().flatMap(product => (product.variants ?? [])
      .filter(variant => variant.status === 'Published')
      .map(variant => ({ product, variant })));
  }

  addCartLine(product: ProductLookupResponse, variant: ProductVariantLookupResponse): void {
    const existing = this.cartLines().find(line => line.variant.id === variant.id);
    if (existing) {
      this.cartLines.set(this.cartLines().map(line =>
        line.variant.id === variant.id
          ? { ...line, quantity: normalizeQuantity(line.quantity + 1) }
          : line));
      this.formDirty.set(true);
      return;
    }

    this.cartLines.set([...this.cartLines(), { product, variant, quantity: 1, discountPercent: 0 }]);
    this.formDirty.set(true);
    this.formErrors.set({ ...this.formErrors(), lines: undefined });
  }

  async searchOrders(): Promise<void> {
    this.pageIndex = 1;
    await this.loadOrders();
  }

  /**
   * Applies the status filter explicitly rather than through `[(ngModel)]` plus a second
   * `(ngModelChange)` listener: those fire in template source order, so reordering the attributes
   * would silently reload using the previously selected status.
   */
  async changeStatusFilter(status: OrderStatus | '' | null): Promise<void> {
    this.statusFilter = status;
    await this.searchOrders();
  }

  async resetFilters(): Promise<void> {
    this.orderSearch = '';
    this.statusFilter = '';
    this.fromDate = '';
    this.toDate = '';
    this.pageIndex = 1;
    await this.loadOrders();
  }

  async changePage(pageIndex: number): Promise<void> {
    this.pageIndex = pageIndex;
    await this.loadOrders();
  }

  async changePageSize(pageSize: number): Promise<void> {
    this.pageSize = pageSize;
    this.pageIndex = 1;
    await this.loadOrders();
  }

  sortBy(key: OrderSortKey): void {
    if (this.sortKey() === key) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
      return;
    }

    this.sortKey.set(key);
    this.sortDirection.set('asc');
  }

  sortIndicator(key: OrderSortKey): string {
    if (this.sortKey() !== key) {
      return '';
    }

    return this.sortDirection() === 'asc' ? '↑' : '↓';
  }

  orderNumber(order: OrderResponse): string {
    return `ORD-${order.id.slice(0, 8).toUpperCase()}`;
  }

  currentOrderNumber(): string {
    const order = this.currentOrder();
    return order ? this.orderNumber(order) : 'Order';
  }

  openCreateOrder(): void {
    this.modalMode.set('create');
    this.currentOrder.set(null);
    this.currentEtag.set(null);
    this.selectedCustomer.set(null);
    this.cartLines.set([]);
    this.customerSearch = '';
    this.customerPhoneSearch = '';
    this.productSearch = '';
    this.formErrors.set({});
    this.formDirty.set(false);
    this.orderModalOpen.set(true);
  }

  async openEditOrder(order: OrderResponse): Promise<void> {
    await this.loadOrderDetail(order.id);
    const selected = this.currentOrder();
    if (!selected) {
      return;
    }

    this.modalMode.set('edit');
    this.selectedCustomer.set({
      id: selected.customerId,
      name: selected.customerName,
      phone: selected.customerPhone,
      status: 'Normal'
    });
    this.cartLines.set(this.toCartLines(selected));
    this.formErrors.set({});
    this.formDirty.set(false);
    this.orderModalOpen.set(true);
  }

  async closeOrderModal(): Promise<void> {
    if (this.formDirty() && !await confirmAction(
      this.modal,
      'Discard Order Changes',
      'Unsaved order changes will be lost.')) {
      return;
    }

    this.orderModalOpen.set(false);
    this.formErrors.set({});
    this.formDirty.set(false);
  }

  updateCartLines(lines: CartLine[]): void {
    this.cartLines.set(lines);
    this.formDirty.set(true);
    this.formErrors.set({ ...this.formErrors(), lines: lines.length === 0 ? 'Add at least one product.' : undefined });
  }

  async saveOrder(): Promise<void> {
    if (!this.validateOrderForm()) {
      return;
    }

    if (this.modalMode() === 'edit') {
      await this.replaceOrderLines();
      return;
    }

    await this.createOrder();
  }

  async deleteOrder(order: OrderResponse): Promise<void> {
    if (order.status === 'Cancelled') {
      return;
    }

    if (!await confirmAction(
      this.modal,
      'Delete Order',
      'This will cancel the order because the current API preserves order history.')) {
      return;
    }

    await this.transitionRowOrder(order, 'cancel');
  }

  async confirmRowOrder(order: OrderResponse): Promise<void> {
    if (order.status !== 'Draft') {
      return;
    }

    await this.transitionRowOrder(order, 'confirm');
  }

  async undoConfirmRowOrder(order: OrderResponse): Promise<void> {
    if (order.status !== 'Confirmed') {
      return;
    }

    await this.transitionRowOrder(order, 'undo');
  }

  async loadRowReservation(order: OrderResponse): Promise<void> {
    await this.loadOrderDetail(order.id);
    await this.loadReservation(order.id);
  }

  async createOrder(): Promise<void> {
    const customer = this.selectedCustomer();
    if (!customer || customer.status !== 'Normal') {
      this.formErrors.set({ ...this.formErrors(), customer: 'Select a Normal customer.' });
      return;
    }

    this.saving.set(true);
    try {
      const lines: OrderLineRequest[] = this.cartLines().map(line => ({
        productVariantId: line.variant.id,
        quantity: line.quantity,
        discountPercent: line.discountPercent
      }));
      const result = await this.orderApi.create(customer.id, lines);
      this.currentOrder.set(result.body);
      this.currentEtag.set(result.etag ?? null);
      await this.subscribeToOrder(result.body.id);
      this.updateWaitingState(result.body);
      this.cartLines.set([]);
      this.orderModalOpen.set(false);
      this.formDirty.set(false);
      await this.loadOrders();
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.saving.set(false);
    }
  }

  async replaceOrderLines(): Promise<void> {
    const order = this.currentOrder();
    const etag = this.currentEtag();
    if (!order || !etag) {
      this.errorMessage.set('Refresh the order and try again.');
      return;
    }

    this.saving.set(true);
    try {
      const lines: OrderLineRequest[] = this.cartLines().map(line => ({
        productVariantId: line.variant.id,
        quantity: line.quantity,
        discountPercent: line.discountPercent
      }));
      const result = await this.orderApi.replaceLines(order.id, lines, etag);
      this.currentOrder.set(result.body);
      this.currentEtag.set(result.etag ?? null);
      this.updateWaitingState(result.body);
      this.orderModalOpen.set(false);
      this.formDirty.set(false);
      await this.loadOrders();
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.saving.set(false);
    }
  }

  async loadOrderDetail(orderId: string): Promise<void> {
    try {
      const result = await this.orderApi.getById(orderId);
      this.currentOrder.set(result.body);
      this.currentEtag.set(result.etag ?? null);
      this.reservationText.set('');
      this.realtimeMessage.set('');
      await this.subscribeToOrder(orderId);
      this.updateWaitingState(result.body);
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    }
  }

  async refreshCurrentOrder(): Promise<void> {
    const order = this.currentOrder();
    if (!order) {
      return;
    }

    try {
      const result = await this.orderApi.getById(order.id);
      this.currentOrder.set(result.body);
      this.currentEtag.set(result.etag ?? null);
      this.updateWaitingState(result.body);
      if (result.body.status !== 'PendingInventory') {
        this.stopPendingPolling();
      }
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    }
  }

  confirmOrder(): Promise<void> {
    return this.transitionOrder('confirm');
  }

  undoConfirmOrder(): Promise<void> {
    return this.transitionOrder('undo');
  }

  async cancelOrder(): Promise<void> {
    if (!await confirmAction(
      this.modal,
      'Cancel Order',
      'The order will be cancelled and its history will be retained.')) {
      return;
    }

    await this.transitionOrder('cancel');
  }

  async loadReservation(orderId: string): Promise<void> {
    try {
      const reservation = await this.orderApi.getReservation(orderId);
      this.reservationText.set(reservation ? JSON.stringify(reservation, null, 2) : 'No reservation found.');
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    }
  }

  realtimeLabel(): string {
    const state = this.orderRealtime.state();
    if (state === 'Connected') {
      return 'Live';
    }

    return state === 'Reconnecting' || state === 'Connecting' ? 'Reconnecting' : 'Offline';
  }

  private async transitionOrder(action: 'confirm' | 'cancel' | 'undo'): Promise<void> {
    const order = this.currentOrder();
    const etag = this.currentEtag();
    if (!order || !etag) {
      return;
    }

    this.saving.set(true);
    try {
      const result = action === 'confirm'
        ? await this.orderApi.confirm(order.id, etag)
        : action === 'cancel'
          ? await this.orderApi.cancel(order.id, etag)
          : await this.orderApi.undoConfirm(order.id, etag);

      this.currentOrder.set(result.body);
      this.currentEtag.set(result.etag ?? null);
      this.updateWaitingState(result.body);
      await this.loadOrders();
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.saving.set(false);
    }
  }

  private async transitionRowOrder(order: OrderResponse, action: 'confirm' | 'cancel' | 'undo'): Promise<void> {
    await this.loadOrderDetail(order.id);
    await this.transitionOrder(action);
  }

  private validateOrderForm(): boolean {
    const errors: { customer?: string; lines?: string } = {};
    const customer = this.selectedCustomer();
    if (!customer || customer.status !== 'Normal') {
      errors.customer = 'Select a Normal customer before saving.';
    }

    if (this.cartLines().length === 0) {
      errors.lines = 'Add at least one product.';
    }

    this.formErrors.set(errors);
    return !errors.customer && !errors.lines;
  }

  private toCartLines(order: OrderResponse): CartLine[] {
    return order.lines.map(line => ({
      product: {
        id: line.productId ?? line.productVariantId,
        sku: line.sku,
        productCode: line.productCode,
        name: line.productName,
        status: 'Published',
        variants: []
      },
      variant: {
        id: line.productVariantId,
        sku: line.sku,
        color: line.colorCode || line.colorName
          ? { id: line.colorCode ?? line.productVariantId, code: line.colorCode ?? '', name: line.colorName ?? line.colorCode ?? '' }
          : null,
        size: line.sizeCode
          ? { id: line.sizeCode, code: line.sizeCode, name: line.sizeCode }
          : null,
        price: line.unitPrice,
        status: 'Published'
      },
      quantity: line.quantity,
      discountPercent: line.discountPercent
    }));
  }

  private compareOrders(left: OrderResponse, right: OrderResponse): number {
    const direction = this.sortDirection() === 'asc' ? 1 : -1;
    const leftValue = this.sortValue(left, this.sortKey());
    const rightValue = this.sortValue(right, this.sortKey());

    if (typeof leftValue === 'number' && typeof rightValue === 'number') {
      return (leftValue - rightValue) * direction;
    }

    return String(leftValue).localeCompare(String(rightValue)) * direction;
  }

  private sortValue(order: OrderResponse, key: OrderSortKey): string | number {
    switch (key) {
      case 'orderNumber':
        return this.orderNumber(order);
      case 'customer':
        return order.customerName;
      case 'phone':
        return order.customerPhone;
      case 'createdAt':
        return Date.parse(order.createdAt);
      case 'total':
        return order.total;
      case 'status':
        return this.statusDisplay(order.status).label;
      case 'updatedAt':
        return Date.parse(order.updatedAt);
    }
  }

  /** True when the search box holds an order number, which only the client can match. */
  private isOrderNumberSearch(term: string): boolean {
    return /^ord-/i.test(term);
  }

  private serverCustomerFilter(): string | undefined {
    // The API matches `customer` against name *or* phone, so phone-shaped terms belong on the server
    // too. Only the order number stays local, since the API has no equivalent filter.
    const term = this.orderSearch.trim();
    return !term || this.isOrderNumberSearch(term) ? undefined : term;
  }

  private clientOrderNumberFilter(): string {
    const term = this.orderSearch.trim();
    return this.isOrderNumberSearch(term) ? term.toLowerCase() : '';
  }

  private async subscribeToOrder(orderId: string): Promise<void> {
    if (this.currentSubscribedOrderId === orderId) {
      return;
    }

    if (this.currentSubscribedOrderId) {
      await this.orderRealtime.unsubscribeFromOrder(this.currentSubscribedOrderId);
    }

    this.currentSubscribedOrderId = orderId;
    await this.orderRealtime.subscribeToOrder(orderId);
  }

  private handleOrderStatusChanged(notification: OrderStatusChangedNotification): void {
    const currentOrder = this.currentOrder();
    this.scheduleOrderListRefresh();

    if (!currentOrder || currentOrder.id.toLowerCase() !== notification.orderId.toLowerCase()) {
      return;
    }

    const notificationKey = `${notification.orderId}:${notification.version}:${notification.currentStatus}`;
    if (this.lastDetailNotificationKey !== notificationKey) {
      this.lastDetailNotificationKey = notificationKey;
      const message = describeOrderStatusChange(notification.currentStatus);
      this.realtimeMessage.set(message);
      this.notificationService.info('Order Updated', message);
    }

    this.scheduleOrderDetailRefresh();
  }

  private scheduleOrderListRefresh(): void {
    if (this.orderListRefreshTimer) {
      clearTimeout(this.orderListRefreshTimer);
    }

    this.orderListRefreshTimer = setTimeout(() => {
      this.orderListRefreshTimer = null;
      void this.loadOrders();
    }, orderListRefreshDelayMs);
  }

  private scheduleOrderDetailRefresh(): void {
    if (this.detailRefreshTimer) {
      clearTimeout(this.detailRefreshTimer);
    }

    this.detailRefreshTimer = setTimeout(() => {
      this.detailRefreshTimer = null;
      void this.refreshCurrentOrder();
    }, orderDetailRefreshDelayMs);
  }

  private updateWaitingState(order: OrderResponse): void {
    if (order.status === 'PendingInventory') {
      if (!this.waitingSinceText()) {
        this.waitingSinceText.set(formatDateTime(new Date().toISOString()));
      }

      this.ensurePendingPolling();
      return;
    }

    this.waitingSinceText.set('');
    this.stopPendingPolling();
  }

  private ensurePendingPolling(): void {
    if (this.pendingPollTimer || this.orderRealtime.state() === 'Connected') {
      return;
    }

    this.pendingPollDeadline = Date.now() + pendingPollWindowMs;
    this.pendingPollTimer = setInterval(() => {
      if (this.orderRealtime.state() === 'Connected' || Date.now() > this.pendingPollDeadline) {
        this.stopPendingPolling();
        return;
      }

      void this.refreshCurrentOrder();
    }, pendingPollIntervalMs);
  }

  private stopPendingPolling(): void {
    if (this.pendingPollTimer) {
      clearInterval(this.pendingPollTimer);
      this.pendingPollTimer = null;
    }
  }

  private clearRefreshTimers(): void {
    if (this.orderListRefreshTimer) {
      clearTimeout(this.orderListRefreshTimer);
      this.orderListRefreshTimer = null;
    }

    if (this.detailRefreshTimer) {
      clearTimeout(this.detailRefreshTimer);
      this.detailRefreshTimer = null;
    }
  }
}

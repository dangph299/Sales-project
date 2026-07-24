import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzNotificationService } from 'ng-zorro-antd/notification';
import { NzTableModule } from 'ng-zorro-antd/table';
import { DataTableComponent } from '../../../../shared/components/data-table/data-table.component';
import { TableCellTemplateDirective } from '../../../../shared/components/data-table/table-cell-template.directive';
import { DropdownComponent } from '../../../../shared/components/dropdown/dropdown.component';
import { PageStateComponent } from '../../../../shared/components/page-state/page-state.component';
import { StatusTagComponent } from '../../../../shared/components/status-tag/status-tag.component';
import { FocusFirstRequiredDirective } from '../../../../shared/directives/focus-first-required.directive';
import { ApiNotifierService } from '../../../../shared/services/api-notifier.service';
import { TablePageChange, TableSort } from '../../../../shared/models/table.model';
import { DateTimePipe } from '../../../../shared/pipes/date-time.pipe';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { ListQueryController } from '../../../../shared/utilities/list-query-controller';
import { confirmAction } from '../../../../shared/utilities/confirm-action';
import { formatDateTime } from '../../../../shared/utilities/display-formatters';
import { ProductLookupApiService } from '../../../common/api/product-lookup-api.service';
import {
  ProductLookupResponse,
  ProductVariantLookupResponse
} from '../../../common/contracts/product-lookup.response';
import { OrderApiService } from '../../api/order-api.service';
import { OrderCustomerRequest } from '../../api/requests/order-customer.request';
import { OrderResponse } from '../../api/responses/order.response';
import { OrderLineEditorComponent } from '../../components/order-line-editor/order-line-editor.component';
import {
  OrderCustomerFormComponent,
  OrderCustomerFormErrors
} from '../../components/order-customer-form/order-customer-form.component';
import { OrderStatus, describeOrderStatusChange, orderStatusDisplay } from '../../constants/order-status';
import { CartLine, cartGrandTotal, normalizeQuantity } from '../../models/cart-line.model';
import { OrderStatusChangedNotification } from '../../models/order-status-changed.model';
import { OrderRealtimeService } from '../../services/order-realtime.service';
import { productLookupStatusDisplay } from '@features/common/constants/product-lookup-status';
import { OrderProductOption, orderListColumns, orderProductOptionColumns } from './order-list.columns';
import {
  emptyOrderCustomer,
  hasCustomerChanged,
  haveLinesChanged,
  toCartLines,
  toOrderLineRequests,
  validateOrderForm
} from './order-form.util';
import { DebouncedRefresh, PendingOrderPoller } from './order-refresh-scheduler';
import {
  OrderSortKey,
  OrderSortDirection,
  compareOrders,
  fromTableSortDirection,
  toTableSortDirection
} from './order-table-sort';

const orderListRefreshDelayMs = 500;
const orderDetailRefreshDelayMs = 300;
const pendingPollIntervalMs = 7_000;
const pendingPollWindowMs = 2 * 60_000;

@Component({
  selector: 'app-order-list-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DataTableComponent,
    TableCellTemplateDirective,
    DropdownComponent,
    PageStateComponent,
    StatusTagComponent,
    OrderLineEditorComponent,
    OrderCustomerFormComponent,
    DateTimePipe,
    MoneyPipe,
    FocusFirstRequiredDirective,
    NzButtonModule,
    NzCardModule,
    NzDescriptionsModule,
    NzDropDownModule,
    NzFormModule,
    NzInputModule,
    NzMenuModule,
    NzModalModule,
    NzTableModule
  ],
  templateUrl: './order-list-page.component.html',
  styleUrl: './order-list-page.component.scss'
})
export class OrderListPageComponent implements OnInit, OnDestroy {
  private readonly orderApi = inject(OrderApiService);
  private readonly productLookup = inject(ProductLookupApiService);
  readonly orderRealtime = inject(OrderRealtimeService);
  private readonly modal = inject(NzModalService);
  private readonly notificationService = inject(NzNotificationService);
  private readonly apiNotifier = inject(ApiNotifierService);

  private readonly query = new ListQueryController<{
    orderNumber: string;
    customerName: string;
    customerPhone: string;
    status: OrderStatus | '' | null;
    from: string;
    to: string;
  }>({
    pageIndex: 1,
    pageSize: 20,
    filters: { orderNumber: '', customerName: '', customerPhone: '', status: '', from: '', to: '' }
  });

  readonly loading = this.query.loading;
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly realtimeMessage = signal('');
  readonly waitingSinceText = signal('');
  readonly products = signal<ProductLookupResponse[]>([]);
  readonly orders = signal<OrderResponse[]>([]);
  readonly total = signal(0);
  readonly cartLines = signal<CartLine[]>([]);
  readonly orderCustomer = signal<OrderCustomerRequest>(emptyOrderCustomer());
  readonly currentOrder = signal<OrderResponse | null>(null);
  readonly currentEtag = signal<string | null>(null);
  readonly reservationText = signal('');
  readonly orderModalOpen = signal(false);
  readonly orderCreateFocusTrigger = signal(0);
  readonly modalMode = signal<'create' | 'edit' | 'view'>('create');
  readonly formDirty = signal(false);
  readonly customerFormErrors = signal<OrderCustomerFormErrors>({});
  readonly formErrors = signal<{ lines?: string }>({});
  readonly productStatusDisplay = productLookupStatusDisplay;

  productSearch = '';
  // Independent filters, all applied server-side. There is no combined term and
  // nothing is matched on the client, so a hit on another page is still found.
  orderNumberFilter = '';
  customerNameFilter = '';
  customerPhoneFilter = '';
  /** `null` is what nzAllowClear writes back; the API client drops empty values from the query. */
  statusFilter: OrderStatus | '' | null = '';
  fromDate = '';
  toDate = '';
  readonly sortKey = signal<OrderSortKey>('updatedAt');
  readonly sortDirection = signal<OrderSortDirection>('desc');
  readonly tableColumns = orderListColumns;
  readonly productOptionColumns = orderProductOptionColumns;

  readonly statusDisplay = orderStatusDisplay;
  readonly orderStatuses: { value: OrderStatus; label: string }[] = [
    { value: 'Draft', label: 'Draft' },
    { value: 'PendingInventory', label: 'Pending Inventory' },
    { value: 'Confirmed', label: 'Confirmed' },
    { value: 'Cancelled', label: 'Cancelled' },
    { value: 'InventoryRejected', label: 'Inventory Rejected' }
  ];
  readonly pageSizeOptions = [10, 20, 50];
  readonly modalTitle = computed(() => {
    const mode = this.modalMode();
    if (mode === 'create') {
      return 'Create Order';
    }

    return `${mode === 'view' ? 'View' : 'Edit'} ${this.currentOrderNumber()}`;
  });
  readonly modalGrandTotal = computed(() => cartGrandTotal(this.cartLines()));
  readonly orderModalReadonly = computed(() => this.modalMode() === 'view');
  readonly canSaveOrder = computed(() => this.modalMode() === 'create'
    || (this.modalMode() === 'edit' && this.currentOrder()?.status === 'Draft'));
  // Every filter is applied by the API, so this only orders the page the server
  // returned. Nothing is filtered out here — doing so would silently disagree
  // with total() and hide matches the server did find.
  readonly displayedOrders = computed(() =>
    [...this.orders()].sort((left, right) =>
      compareOrders(left, right, this.sortKey(), this.sortDirection(), this.statusDisplay)));
  readonly tableSort = computed<TableSort>(() => ({
    key: this.sortKey(),
    direction: toTableSortDirection(this.sortDirection())
  }));

  get pageIndex(): number {
    return this.query.state().pageIndex;
  }

  get pageSize(): number {
    return this.query.state().pageSize;
  }

  private currentSubscribedOrderId: string | null = null;
  private removeStatusChangedHandler: (() => void) | null = null;
  private removeReconnectedHandler: (() => void) | null = null;
  private lastDetailNotificationKey = '';

  private readonly orderListRefresh = new DebouncedRefresh(orderListRefreshDelayMs, () => void this.loadOrders());
  private readonly orderDetailRefresh = new DebouncedRefresh(
    orderDetailRefreshDelayMs, () => void this.refreshCurrentOrder());
  private readonly pendingPoller = new PendingOrderPoller(
    pendingPollIntervalMs,
    pendingPollWindowMs,
    () => this.orderRealtime.state() === 'Connected',
    () => void this.refreshCurrentOrder());

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
        this.pendingPoller.stop();
        return;
      }

      this.pendingPoller.ensureStarted();
    });
  }

  ngOnInit(): void {
    // No customer prefetch: the order form asks the server per keystroke instead
    // of pulling a page of customers up front and filtering it here.
    void Promise.all([this.loadProducts(), this.loadOrders()]);
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
    this.orderListRefresh.clear();
    this.orderDetailRefresh.clear();
    this.pendingPoller.stop();
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
        status: '',
        page: 1,
        pageSize: 10
      });
      this.products.set(page.items);
    } catch (error) {
      this.notifyError('Load Products Failed', error);
    }
  }

  async loadOrders(): Promise<void> {
    this.errorMessage.set('');
    try {
      const page = await this.query.run(state => this.orderApi.search({
        orderNumber: state.filters.orderNumber || undefined,
        customerName: state.filters.customerName || undefined,
        customerPhone: state.filters.customerPhone || undefined,
        status: state.filters.status,
        from: state.filters.from || undefined,
        to: state.filters.to || undefined,
        page: state.pageIndex,
        pageSize: state.pageSize
      }));
      if (!page) {
        return;
      }

      if (page.items.length === 0 && page.total > 0 && this.pageIndex > 1) {
        this.query.setPage(Math.max(1, Math.ceil(page.total / this.pageSize)));
        await this.loadOrders();
        return;
      }

      this.orders.set(page.items);
      this.total.set(page.total);
    } catch (error) {
      this.notifyError('Load Orders Failed', error, true);
    }
  }

  updateOrderCustomer(customer: OrderCustomerRequest): void {
    this.orderCustomer.set(customer);
    this.formDirty.set(true);
    this.customerFormErrors.set({});
  }

  sellableVariants(): OrderProductOption[] {
    return this.products().flatMap(product => (product.variants ?? [])
      .filter(variant => this.canOrderVariant(variant))
      .map(variant => ({ product, variant })));
  }

  addCartLine(product: ProductLookupResponse, variant: ProductVariantLookupResponse): void {
    if (this.orderModalReadonly()) {
      return;
    }

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
    this.query.setFilters({
      orderNumber: this.orderNumberFilter.trim(),
      customerName: this.customerNameFilter.trim(),
      customerPhone: this.customerPhoneFilter.trim(),
      status: this.statusFilter,
      from: this.fromDate,
      to: this.toDate
    });
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
    this.orderNumberFilter = '';
    this.customerNameFilter = '';
    this.customerPhoneFilter = '';
    this.statusFilter = '';
    this.fromDate = '';
    this.toDate = '';
    this.query.setFilters({ orderNumber: '', customerName: '', customerPhone: '', status: '', from: '', to: '' });
    await this.loadOrders();
  }

  async changeTablePage(page: TablePageChange): Promise<void> {
    this.query.setPage(page.pageIndex, page.pageSize);
    await this.loadOrders();
  }

  changeTableSort(sort: TableSort): void {
    this.sortKey.set(sort.key as OrderSortKey);
    this.sortDirection.set(fromTableSortDirection(sort.direction));
  }

  orderNumber(order: OrderResponse): string {
    return order.orderCode;
  }

  currentOrderNumber(): string {
    const order = this.currentOrder();
    return order ? this.orderNumber(order) : 'Order';
  }

  openCreateOrder(): void {
    if (this.saving()) {
      return;
    }

    this.modalMode.set('create');
    this.currentOrder.set(null);
    this.currentEtag.set(null);
    this.orderCustomer.set(emptyOrderCustomer());
    this.cartLines.set([]);
    this.productSearch = '';
    this.formErrors.set({});
    this.customerFormErrors.set({});
    this.formDirty.set(false);
    this.orderModalOpen.set(true);
  }

  async openEditOrder(order: OrderResponse): Promise<void> {
    if (this.saving()) {
      return;
    }

    await this.loadOrderDetail(order.id);
    const selected = this.currentOrder();
    if (!selected) {
      return;
    }

    this.modalMode.set(this.canEditOrder(selected) ? 'edit' : 'view');
    // Straight from the order's own snapshot. The customer record is never
    // re-fetched to overwrite it: the two are allowed to differ.
    this.orderCustomer.set({
      name: selected.customerName,
      phone: selected.customerPhone,
      email: selected.customerEmail ?? '',
      address: selected.customerAddress ?? ''
    });
    this.cartLines.set(toCartLines(selected));
    this.formErrors.set({});
    this.customerFormErrors.set({});
    this.formDirty.set(false);
    this.orderModalOpen.set(true);
  }

  triggerOrderCreateFocus(): void {
    if (this.orderModalOpen() && this.modalMode() === 'create') {
      this.orderCreateFocusTrigger.update(value => value + 1);
    }
  }

  async closeOrderModal(): Promise<void> {
    if (this.saving()) {
      return;
    }

    if (this.formDirty() && !await confirmAction(
      this.modal,
      'Discard Order Changes',
      'Unsaved order changes will be lost.')) {
      return;
    }

    this.orderModalOpen.set(false);
    this.formErrors.set({});
    this.customerFormErrors.set({});
    this.formDirty.set(false);
    this.orderCustomer.set(emptyOrderCustomer());
    this.cartLines.set([]);
  }

  updateCartLines(lines: CartLine[]): void {
    if (this.orderModalReadonly()) {
      return;
    }

    this.cartLines.set(lines);
    this.formDirty.set(true);
    this.formErrors.set({ ...this.formErrors(), lines: lines.length === 0 ? 'Add at least one product.' : undefined });
  }

  async saveOrder(): Promise<void> {
    if (this.saving()) {
      return;
    }

    if (!this.canSaveOrder()) {
      return;
    }

    if (!this.applyFormValidation()) {
      return;
    }

    if (this.modalMode() === 'edit') {
      await this.saveOrderEdits();
      return;
    }

    await this.createOrder();
  }

  async deleteOrder(order: OrderResponse): Promise<void> {
    if (this.saving()) {
      return;
    }

    if (!this.canCancelOrder(order)) {
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
    if (this.saving()) {
      return;
    }

    if (order.status !== 'Draft') {
      return;
    }

    await this.transitionRowOrder(order, 'confirm');
  }

  async undoConfirmRowOrder(order: OrderResponse): Promise<void> {
    if (this.saving()) {
      return;
    }

    if (order.status !== 'Confirmed') {
      return;
    }

    await this.loadOrderDetail(order.id);
    const selected = this.currentOrder();
    if (selected && !this.canUndoConfirmOrder(selected)) {
      this.notificationService.warning('Cannot Undo Confirm', 'This order used discontinued variants for sell-through and cannot be undone after confirmation.');
      return;
    }

    await this.transitionOrder('undo');
  }

  async loadRowReservation(order: OrderResponse): Promise<void> {
    await this.loadOrderDetail(order.id);
    await this.loadReservation(order.id);
  }

  async createOrder(): Promise<void> {
    if (this.saving()) {
      return;
    }

    this.saving.set(true);
    this.formErrors.set({});
    try {
      // Sent as typed. Whether this customer already exists is the backend's
      // question, answered in the same transaction that creates the order.
      const result = await this.orderApi.create(this.orderCustomer(), toOrderLineRequests(this.cartLines()));
      this.currentOrder.set(result.body);
      this.currentEtag.set(result.etag ?? null);
      await this.subscribeToOrder(result.body.id);
      this.updateWaitingState(result.body);
      this.cartLines.set([]);
      this.orderCustomer.set(emptyOrderCustomer());
      this.orderModalOpen.set(false);
      this.formDirty.set(false);
      await this.loadOrders();
    } catch (error) {
      this.notifyError('Create Order Failed', error);
    } finally {
      this.saving.set(false);
    }
  }

  /**
   * Saves an edited order.
   *
   * The customer snapshot and the lines are separate endpoints, so only what
   * actually changed is sent, and the ETag from the first call is threaded into
   * the second — the order's version moves after each one.
   */
  async saveOrderEdits(): Promise<void> {
    if (this.saving()) {
      return;
    }

    const order = this.currentOrder();
    let etag = this.currentEtag();
    if (!order || !etag) {
      this.notificationService.warning('Refresh Required', 'Refresh the order and try again.');
      return;
    }

    this.saving.set(true);
    this.formErrors.set({});
    try {
      let saved = order;
      if (hasCustomerChanged(this.orderCustomer(), order)) {
        const result = await this.orderApi.updateCustomer(order.id, this.orderCustomer(), etag);
        saved = result.body;
        etag = result.etag ?? etag;
      }

      if (haveLinesChanged(this.cartLines(), order)) {
        const result = await this.orderApi.replaceLines(order.id, toOrderLineRequests(this.cartLines()), etag);
        saved = result.body;
        etag = result.etag ?? etag;
      }

      this.currentOrder.set(saved);
      this.currentEtag.set(etag);
      this.updateWaitingState(saved);
      this.orderModalOpen.set(false);
      this.formDirty.set(false);
      await this.loadOrders();
    } catch (error) {
      this.notifyError('Update Order Failed', error);
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
      this.notifyError('Load Order Failed', error);
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
        this.pendingPoller.stop();
      }
    } catch (error) {
      this.notifyError('Refresh Order Failed', error);
    }
  }

  confirmOrder(): Promise<void> {
    if (this.saving()) {
      return Promise.resolve();
    }

    const order = this.currentOrder();
    if (!order || order.status !== 'Draft') {
      return Promise.resolve();
    }

    return this.transitionOrder('confirm');
  }

  undoConfirmOrder(): Promise<void> {
    if (this.saving()) {
      return Promise.resolve();
    }

    const order = this.currentOrder();
    if (order && !this.canUndoConfirmOrder(order)) {
      this.notificationService.warning('Cannot Undo Confirm', 'This order used discontinued variants for sell-through and cannot be undone after confirmation.');
      return Promise.resolve();
    }

    return this.transitionOrder('undo');
  }

  async cancelOrder(): Promise<void> {
    if (this.saving()) {
      return;
    }

    if (!await confirmAction(
      this.modal,
      'Cancel Order',
      'The order will be cancelled and its history will be retained.')) {
      return;
    }

    await this.transitionOrder('cancel');
  }

  canEditOrder(order: OrderResponse): boolean {
    return order.status === 'Draft';
  }

  canCancelOrder(order: OrderResponse): boolean {
    return order.status !== 'PendingInventory'
      && order.status !== 'Confirmed'
      && order.status !== 'Cancelled';
  }

  async loadReservation(orderId: string): Promise<void> {
    try {
      const reservation = await this.orderApi.getReservation(orderId);
      this.reservationText.set(reservation ? JSON.stringify(reservation, null, 2) : 'No reservation found.');
    } catch (error) {
      this.notifyError('Load Reservation Failed', error);
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
    if (this.saving()) {
      return;
    }

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
      this.notifyError(this.orderActionErrorTitle(action), error);
    } finally {
      this.saving.set(false);
    }
  }

  private async transitionRowOrder(order: OrderResponse, action: 'confirm' | 'cancel' | 'undo'): Promise<void> {
    if (this.saving()) {
      return;
    }

    await this.loadOrderDetail(order.id);
    await this.transitionOrder(action);
  }

  private orderActionErrorTitle(action: 'confirm' | 'cancel' | 'undo'): string {
    switch (action) {
      case 'confirm':
        return 'Confirm Order Failed';
      case 'cancel':
        return 'Cancel Order Failed';
      case 'undo':
        return 'Undo Confirm Failed';
    }
  }

  /**
   * Explains every problem rather than just disabling Save, so the user can see
   * what to fix instead of guessing why the button does nothing.
   */
  private applyFormValidation(): boolean {
    const result = validateOrderForm(this.orderCustomer(), this.cartLines());
    this.customerFormErrors.set(result.customerErrors);
    this.formErrors.set(result.formErrors);
    return result.isValid;
  }

  private canOrderVariant(variant: ProductVariantLookupResponse): boolean {
    return variant.status === 'Published' || variant.status === 'Discontinued';
  }

  canUndoConfirmOrder(order: OrderResponse): boolean {
    return order.status === 'Confirmed' && !order.lines.some(line => line.isSellThroughDiscontinued);
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
    this.orderListRefresh.schedule();

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

    this.orderDetailRefresh.schedule();
  }

  private updateWaitingState(order: OrderResponse): void {
    if (order.status === 'PendingInventory') {
      if (!this.waitingSinceText()) {
        this.waitingSinceText.set(formatDateTime(new Date().toISOString()));
      }

      this.pendingPoller.ensureStarted();
      return;
    }

    this.waitingSinceText.set('');
    this.pendingPoller.stop();
  }

  private notifyError(title: string, error: unknown, keepInline = false): void {
    const message = this.apiNotifier.error(title, error);
    if (keepInline) {
      this.errorMessage.set(message);
    }
  }
}

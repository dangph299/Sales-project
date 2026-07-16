import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClientError, ApiResponseReader } from './api-client-result';
import { ApiService } from './api.service';
import { CustomerDto, OrderDto, OrderLineInput, PhoneMatch, ProductDto, ValidationError } from './models';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  readonly userName = signal('admin');
  readonly password = signal('Admin123!');
  readonly busy = signal(false);
  readonly log = signal<string[]>([]);
  readonly formErrors = signal<Record<string, string[]>>({});
  readonly globalErrors = signal<string[]>([]);

  readonly productForm = signal({ sku: `SKU-${Date.now()}`, name: 'Demo product', price: 120000, isActive: true });
  readonly productSearch = signal('');
  readonly products = signal<ProductDto[]>([]);
  readonly selectedProduct = signal<ProductDto | null>(null);

  readonly customerForm = signal({ name: 'Nguyen Van A', phone: `090${Math.floor(1000000 + Math.random() * 8999999)}` });
  readonly customerSearch = signal({ name: '', phone: '', phoneMatch: PhoneMatch.Prefix });
  readonly customers = signal<CustomerDto[]>([]);
  readonly selectedCustomer = signal<CustomerDto | null>(null);

  readonly orderSearch = signal({ from: '', to: '', customer: '' });
  readonly orders = signal<OrderDto[]>([]);
  readonly orderQuantity = signal(2);
  readonly orderDiscount = signal(0);
  readonly currentOrder = signal<OrderDto | null>(null);
  readonly currentOrderEtag = signal('');
  readonly concurrencyResult = signal('');

  readonly inventoryDelta = signal(100);
  readonly inventoryText = signal('');

  readonly canCallApi = computed(() => !!this.api.accessToken());
  readonly selectedProductLabel = computed(() => {
    const product = this.selectedProduct();
    return product ? `${product.sku} - ${product.name}` : 'Chưa chọn sản phẩm';
  });
  readonly selectedCustomerLabel = computed(() => {
    const customer = this.selectedCustomer();
    return customer ? `${customer.name} - ${customer.phone}` : 'Chưa chọn khách hàng';
  });

  constructor(readonly api: ApiService) {}

  async run(label: string, action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    this.formErrors.set({});
    this.globalErrors.set([]);
    this.api.clearSuccessMessage();
    try {
      await action();
      this.push(`✅ ${this.api.consumeSuccessMessage() || label}`);
    } catch (error) {
      this.captureValidationErrors(error);
      this.push(`❌ ${label}: ${this.describeError(error)}`);
    } finally {
      this.busy.set(false);
    }
  }

  health(): Promise<void> {
    return this.run('Health check', async () => {
      const result = await this.api.health();
      this.pushJson(result);
    });
  }

  login(): Promise<void> {
    return this.run('Login', async () => {
      const token = await this.api.login(this.userName(), this.password());
      this.push(`Token expires in ${token.expiresIn}s`);
    });
  }

  logout(): void {
    this.api.logout();
    this.push('Logged out.');
  }

  createProduct(): Promise<void> {
    return this.run('Create product', async () => {
      const form = this.productForm();
      const created = await this.api.createProduct({ sku: form.sku, name: form.name, price: form.price });
      this.selectProduct(created);
      await this.searchProducts(created.name);
    });
  }

  updateProduct(): Promise<void> {
    return this.run('Update product', async () => {
      const selected = this.requireProduct();
      const form = this.productForm();
      const updated = await this.api.updateProduct(selected.id, { name: form.name, price: form.price, isActive: form.isActive });
      this.selectProduct(updated);
      await this.searchProducts(this.productSearch());
    });
  }

  deleteProduct(): Promise<void> {
    const selected = this.requireProduct();
    if (!confirm(`Delete product ${selected.sku} - ${selected.name}?`)) {
      return Promise.resolve();
    }

    return this.run('Delete product', async () => {
      await this.api.deleteProduct(selected.id);
      this.resetProductForm();
      await this.searchProducts(this.productSearch());
    });
  }

  resetProductForm(): void {
    this.selectedProduct.set(null);
    this.productForm.set({ sku: `SKU-${Date.now()}`, name: 'Demo product', price: 120000, isActive: true });
  }

  searchProducts(name = this.productSearch()): Promise<void> {
    return this.run('Search products', async () => {
      const result = await this.api.searchProducts(name);
      this.products.set(result.items);
      this.push(`Products: ${result.total}`);
    });
  }

  createCustomer(): Promise<void> {
    return this.run('Create customer', async () => {
      const created = await this.api.createCustomer(this.customerForm());
      this.selectCustomer(created);
      await this.searchCustomers(created.phone, PhoneMatch.Prefix);
    });
  }

  updateCustomer(): Promise<void> {
    return this.run('Update customer', async () => {
      const selected = this.requireCustomer();
      const updated = await this.api.updateCustomer(selected.id, this.customerForm());
      this.selectCustomer(updated);
      await this.searchCustomers(this.customerSearch().phone, this.customerSearch().phoneMatch);
    });
  }

  deleteCustomer(): Promise<void> {
    const selected = this.requireCustomer();
    if (!confirm(`Delete customer ${selected.name} - ${selected.phone}?`)) {
      return Promise.resolve();
    }

    return this.run('Delete customer', async () => {
      await this.api.deleteCustomer(selected.id);
      this.resetCustomerForm();
      await this.searchCustomers(this.customerSearch().phone, this.customerSearch().phoneMatch);
    });
  }

  resetCustomerForm(): void {
    this.selectedCustomer.set(null);
    this.customerForm.set({ name: 'Nguyen Van A', phone: `090${Math.floor(1000000 + Math.random() * 8999999)}` });
  }

  searchCustomers(phone = this.customerSearch().phone, phoneMatch = this.customerSearch().phoneMatch): Promise<void> {
    return this.run('Search customers', async () => {
      const criteria = this.customerSearch();
      const result = await this.api.searchCustomers(criteria.name, phone, phoneMatch);
      this.customers.set(result.items);
      this.push(`Customers: ${result.total}`);
    });
  }

  createOrder(): Promise<void> {
    return this.run('Create order', async () => {
      const customer = this.requireCustomer();
      const product = this.requireProduct();
      const response = await this.api.createOrder(customer.id, [this.line(product, this.orderQuantity(), this.orderDiscount())]);
      this.setCurrentOrder(response.body, response.etag);
      await this.searchOrders();
      this.push(`Order ${response.body.id}, ETag ${this.currentOrderEtag()}`);
    });
  }

  searchOrders(): Promise<void> {
    return this.run('Search orders', async () => {
      const result = await this.api.searchOrders(this.orderSearch());
      this.orders.set(result.items);
      this.push(`Orders: ${result.total}`);
    });
  }

  selectOrder(order: OrderDto): Promise<void> {
    return this.run('Load order detail', async () => {
      const response = await this.api.getOrder(order.id);
      this.setCurrentOrder(response.body, response.etag);
    });
  }

  reloadOrder(): Promise<void> {
    return this.run('Reload order', async () => {
      const order = this.requireOrder();
      const response = await this.api.getOrder(order.id);
      this.setCurrentOrder(response.body, response.etag);
    });
  }

  replaceLines(): Promise<void> {
    return this.run('Update order lines', async () => {
      const order = this.requireOrder();
      const product = this.requireProduct();
      const response = await this.api.replaceOrderLines(order.id, [this.line(product, this.orderQuantity(), this.orderDiscount())], this.currentOrderEtag());
      this.setCurrentOrder(response.body, response.etag);
      await this.searchOrders();
    });
  }

  testConcurrency(): Promise<void> {
    return this.run('Concurrency ETag test', async () => {
      const order = this.requireOrder();
      const product = this.requireProduct();
      const etag = this.currentOrderEtag();
      const first = this.api.replaceOrderLines(order.id, [this.line(product, this.orderQuantity() + 1, 0)], etag)
        .then(x => ({ status: x.status, etag: x.etag, totalQuantity: x.body.totalQuantity, version: x.body.version }))
        .catch(error => ({ status: error.status ?? 0, error: this.describeError(error) }));
      const second = this.api.replaceOrderLines(order.id, [this.line(product, this.orderQuantity() + 2, 0)], etag)
        .then(x => ({ status: x.status, etag: x.etag, totalQuantity: x.body.totalQuantity, version: x.body.version }))
        .catch(error => ({ status: error.status ?? 0, error: this.describeError(error) }));
      const result = await Promise.all([first, second]);
      this.concurrencyResult.set(JSON.stringify(result, null, 2));
      await this.reloadOrder();
    });
  }

  confirmOrder(): Promise<void> {
    return this.run('Confirm order / update status', async () => {
      const order = this.requireOrder();
      const response = await this.api.confirmOrder(order.id, this.currentOrderEtag());
      this.setCurrentOrder(response.body, response.etag);
      await this.searchOrders();
    });
  }

  testConfirmConcurrency(): Promise<void> {
    return this.run('Confirm same ETag test', async () => {
      const order = this.requireOrder();
      const etag = this.currentOrderEtag();
      const confirm = () => this.api.confirmOrder(order.id, etag)
        .then(x => ({ status: x.status, etag: x.etag, orderStatus: x.body.status, version: x.body.version }))
        .catch(error => ({ status: error.status ?? 0, error: this.describeError(error) }));
      const result = await Promise.all([confirm(), confirm()]);
      this.concurrencyResult.set(JSON.stringify(result, null, 2));
      await this.reloadOrder();
    });
  }

  cancelOrder(): Promise<void> {
    return this.run('Cancel order / update status', async () => {
      const order = this.requireOrder();
      const response = await this.api.cancelOrder(order.id, this.currentOrderEtag());
      this.setCurrentOrder(response.body, response.etag);
      await this.searchOrders();
    });
  }

  undoConfirmOrder(): Promise<void> {
    return this.run('Undo confirm order / update status', async () => {
      const order = this.requireOrder();
      const response = await this.api.undoConfirmOrder(order.id, this.currentOrderEtag());
      this.setCurrentOrder(response.body, response.etag);
      await this.searchOrders();
    });
  }

  adjustInventory(): Promise<void> {
    return this.run('Adjust inventory', async () => {
      const product = this.requireProduct();
      const item = await this.api.adjustInventory(product.id, product.sku, this.inventoryDelta());
      this.inventoryText.set(JSON.stringify(item, null, 2));
    });
  }

  getInventory(): Promise<void> {
    return this.run('Get inventory', async () => {
      const product = this.requireProduct();
      const item = await this.api.getInventory(product.id);
      this.inventoryText.set(JSON.stringify(item, null, 2));
    });
  }

  selectProduct(product: ProductDto): void {
    this.selectedProduct.set(product);
    this.productForm.set({ sku: product.sku, name: product.name, price: product.price, isActive: product.isActive });
  }

  selectCustomer(customer: CustomerDto): void {
    this.selectedCustomer.set(customer);
    this.customerForm.set({ name: customer.name, phone: customer.phone });
  }

  patchProductForm(patch: Partial<{ sku: string; name: string; price: number; isActive: boolean }>): void {
    this.productForm.update(value => ({ ...value, ...patch }));
    this.clearPatchedFields(patch);
  }

  patchCustomerForm(patch: Partial<{ name: string; phone: string }>): void {
    this.customerForm.update(value => ({ ...value, ...patch }));
    this.clearPatchedFields(patch);
  }

  patchCustomerSearch(patch: Partial<{ name: string; phone: string; phoneMatch: PhoneMatch }>): void {
    this.customerSearch.update(value => ({ ...value, ...patch }));
  }

  patchOrderSearch(patch: Partial<{ from: string; to: string; customer: string }>): void {
    this.orderSearch.update(value => ({ ...value, ...patch }));
  }

  statusClass(status: string): string {
    return `badge ${status.toLowerCase()}`;
  }

  fieldError(...fields: string[]): string {
    const errors = this.formErrors();
    const messages: string[] = [];

    for (const field of fields) {
      const normalized = this.normalizeField(field);
      const fieldMessages = errors[normalized] || [];
      for (const message of fieldMessages) {
        if (!messages.includes(message)) {
          messages.push(message);
        }
      }
    }

    return messages.join(' ');
  }

  private setCurrentOrder(order: OrderDto, etag?: string | null): void {
    this.currentOrder.set(order);
    this.currentOrderEtag.set(etag || `"${order.version}"`);
    if (order.lines.length > 0) {
      this.orderQuantity.set(order.totalQuantity);
      this.orderDiscount.set(order.lines[0].discountPercent);
      const matchingProduct = this.products().find(x => x.id === order.lines[0].productId);
      if (matchingProduct) {
        this.selectProduct(matchingProduct);
      }
    }
  }

  private line(product: ProductDto, quantity: number, discountPercent: number): OrderLineInput {
    return { productId: product.id, quantity: Number(quantity), discountPercent: Number(discountPercent) };
  }

  private requireProduct(): ProductDto {
    const product = this.selectedProduct();
    if (!product) {
      throw new Error('Select or create a product first.');
    }

    return product;
  }

  private requireCustomer(): CustomerDto {
    const customer = this.selectedCustomer();
    if (!customer) {
      throw new Error('Select or create a customer first.');
    }

    return customer;
  }

  private requireOrder(): OrderDto {
    const order = this.currentOrder();
    if (!order) {
      throw new Error('Create or load an order first.');
    }

    if (!this.currentOrderEtag()) {
      throw new Error('Missing ETag for current order.');
    }

    return order;
  }

  private push(message: string): void {
    this.log.update(items => [`${new Date().toLocaleTimeString()} ${message}`, ...items].slice(0, 80));
  }

  private pushJson(value: unknown): void {
    this.push(JSON.stringify(value, null, 2));
  }

  private describeError(error: unknown): string {
    if (error instanceof ApiClientError) {
      return ApiResponseReader.formatFailure(error.result);
    }

    if (error instanceof Error) {
      return error.message;
    }

    return String(error);
  }

  private captureValidationErrors(error: unknown): void {
    if (!(error instanceof ApiClientError)) {
      return;
    }

    const fieldErrors: Record<string, string[]> = {};
    const globalErrors: string[] = [];

    for (const validationError of error.result.validationErrors) {
      this.addValidationError(validationError, fieldErrors, globalErrors);
    }

    if (error.result.validationErrors.length === 0) {
      for (const message of ApiResponseReader.failureMessages(error.result)) {
        if (!globalErrors.includes(message)) {
          globalErrors.push(message);
        }
      }
    }

    this.formErrors.set(fieldErrors);
    this.globalErrors.set(globalErrors);
  }

  private addValidationError(
    validationError: ValidationError,
    fieldErrors: Record<string, string[]>,
    globalErrors: string[]): void {
    if (!validationError.field || validationError.field.trim() === '') {
      if (!globalErrors.includes(validationError.message)) {
        globalErrors.push(validationError.message);
      }

      return;
    }

    const field = this.normalizeField(validationError.field);
    fieldErrors[field] = fieldErrors[field] || [];

    if (!fieldErrors[field].includes(validationError.message)) {
      fieldErrors[field].push(validationError.message);
    }
  }

  private clearPatchedFields(patch: Record<string, unknown>): void {
    const fields = Object.keys(patch).map(field => this.normalizeField(field));
    if (fields.length === 0) {
      return;
    }

    this.formErrors.update(errors => {
      const next = { ...errors };
      for (const field of fields) {
        delete next[field];
      }

      return next;
    });
  }

  private normalizeField(field: string): string {
    return field.replace(/^.*\./, '').toLowerCase();
  }
}

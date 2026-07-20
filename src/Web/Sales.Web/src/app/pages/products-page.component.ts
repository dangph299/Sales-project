import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClientError, ApiResponseReader } from '../api-client-result';
import { ApiService } from '../api.service';
import { EProductStatus, EProductVariantStatus, ProductDto, ProductVariantDto, ValidationError } from '../models';
import { buildSkuPreview, seededColors, seededSizes, uncategorizedCategoryId } from '../reference-data/reference-data';
import { formatDateTime, formatPriceRange } from '../shared/display-formatters';
import { MoneyDisplayComponent } from '../shared/money-display.component';
import { PageStateComponent } from '../shared/page-state.component';
import { StatusBadgeComponent } from '../shared/status-badge.component';

interface ProductForm {
  productCode: string;
  name: string;
  description: string;
  categoryId: string;
  status: EProductStatus;
}

interface ProductVariantForm {
  colorId: string;
  sizeId: string;
  price: number;
  status: EProductVariantStatus;
}

@Component({
  selector: 'app-products-page',
  standalone: true,
  imports: [CommonModule, FormsModule, MoneyDisplayComponent, PageStateComponent, StatusBadgeComponent],
  template: `
    <section class="page-header">
      <div>
        <p class="eyebrow">Catalog</p>
        <h1>Products</h1>
        <p>Product giu thong tin chung. Gia, SKU ban hang, color va size nam tren ProductVariant.</p>
      </div>
      <button type="button" (click)="loadProducts()">Refresh</button>
    </section>

    <section class="toolbar">
      <label>Product code
        <input name="productCodeFilter" [(ngModel)]="productCodeFilter" (keyup.enter)="loadProducts()">
      </label>
      <label>Name
        <input name="productNameFilter" [(ngModel)]="nameFilter" (keyup.enter)="loadProducts()">
      </label>
      <label>SKU
        <input name="skuFilter" [(ngModel)]="skuFilter" (keyup.enter)="loadProducts()">
      </label>
      <label>Status
        <select name="productStatusFilter" [(ngModel)]="statusFilter">
          <option value="">All</option>
          <option value="Draft">Draft</option>
          <option value="Published">Published</option>
          <option value="Discontinued">Discontinued</option>
        </select>
      </label>
      <label>Color
        <select name="colorFilter" [(ngModel)]="colorFilter">
          <option value="">All</option>
          <option *ngFor="let color of colors" [value]="color.id">{{ color.code }}</option>
        </select>
      </label>
      <label>Size
        <select name="sizeFilter" [(ngModel)]="sizeFilter">
          <option value="">All</option>
          <option *ngFor="let size of sizes" [value]="size.id">{{ size.code }}</option>
        </select>
      </label>
      <button type="button" (click)="loadProducts()">Search</button>
    </section>

    <app-page-state [loading]="loading()" [errorMessage]="errorMessage()" [empty]="products().length === 0" emptyTitle="Chua co product" emptyText="Tao product draft, them variant, sau do publish." (retry)="loadProducts()"></app-page-state>

    <section class="content-grid two" *ngIf="!loading()">
      <article class="panel-card wide">
        <div class="section-title">
          <h2>Product List</h2>
          <span>{{ total() }} records</span>
        </div>
        <div class="table-wrap" *ngIf="products().length > 0">
          <table class="data-table">
            <thead>
              <tr>
                <th>Code</th>
                <th>Name</th>
                <th>Category</th>
                <th>Status</th>
                <th>Variants</th>
                <th>Price range</th>
                <th>Updated</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let product of products()" (click)="selectProduct(product)" [class.selected]="selectedProduct()?.id === product.id">
                <td>{{ product.productCode || product.sku }}</td>
                <td>{{ product.name }}</td>
                <td>{{ product.category?.name || product.categoryId || '-' }}</td>
                <td><app-status-badge [status]="product.status || (product.isActive ? 'Published' : 'Draft')"></app-status-badge></td>
                <td>{{ product.variants?.length || 0 }} / {{ publishedVariantCount(product) }} published</td>
                <td><app-money-display [minPrice]="product.minPrice" [maxPrice]="product.maxPrice"></app-money-display></td>
                <td>{{ formatDateTime(product.updatedAt) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>

      <article class="panel-card">
        <div class="section-title">
          <h2>{{ selectedProduct() ? 'Edit Product' : 'Create Product' }}</h2>
          <button type="button" class="secondary" (click)="resetProductForm()">New</button>
        </div>
        <div class="error-panel" *ngIf="errorMessage()">{{ errorMessage() }}</div>
        <div class="error-panel" *ngIf="formErrorSummary().length > 0">
          <p *ngFor="let message of formErrorSummary()">{{ message }}</p>
        </div>
        <form class="form-grid" (ngSubmit)="saveProduct()">
          <label>Product code
            <input name="productCode" [(ngModel)]="productForm.productCode" [disabled]="!!selectedProduct()" required>
          </label>
          <label>Name
            <input name="productName" [(ngModel)]="productForm.name" required>
          </label>
          <label>Description
            <textarea name="productDescription" [(ngModel)]="productForm.description" rows="3"></textarea>
          </label>
          <label>Category ID
            <input name="productCategoryId" [(ngModel)]="productForm.categoryId" required>
            <small>Default published category: {{ uncategorizedCategoryId }}</small>
          </label>
          <label>Status
            <select name="productStatus" [(ngModel)]="productForm.status" [disabled]="!selectedProduct()">
              <option value="Draft">Draft</option>
              <option value="Published">Published</option>
              <option value="Discontinued">Discontinued</option>
            </select>
          </label>
          <div class="form-actions">
            <button type="submit" [disabled]="saving()">{{ selectedProduct() ? 'Save product' : 'Create product' }}</button>
            <button type="button" class="secondary" (click)="resetProductForm()">Cancel</button>
          </div>
        </form>
        <div class="actions wrap" *ngIf="selectedProduct() as product">
          <button type="button" (click)="changeProductStatus('Published')" [disabled]="product.status === 'Published'">Publish Product</button>
          <button type="button" class="warning" (click)="changeProductStatus('Discontinued')" [disabled]="product.status === 'Discontinued'">Discontinue Product</button>
        </div>
      </article>
    </section>

    <section class="panel-card" *ngIf="selectedProduct() as product">
      <div class="section-title">
        <div>
          <h2>Product Variants</h2>
          <p>{{ product.productCode || product.sku }} - {{ product.name }}</p>
        </div>
        <span>{{ formatPriceRange(product.minPrice, product.maxPrice) }}</span>
      </div>
      <div class="table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th>SKU</th>
              <th>Color</th>
              <th>Size</th>
              <th>Price</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let variant of product.variants || []" [class.selected]="selectedVariant()?.id === variant.id">
              <td>{{ variant.sku }}</td>
              <td>{{ variant.color?.code || '-' }} {{ variant.color?.name || '' }}</td>
              <td>{{ variant.size?.code || '-' }}</td>
              <td><app-money-display [amount]="variant.price"></app-money-display></td>
              <td><app-status-badge [status]="variant.status"></app-status-badge></td>
              <td>
                <button type="button" class="secondary small" (click)="selectVariant(variant)">Edit</button>
                <button type="button" class="warning small" (click)="discontinueVariant(variant)" [disabled]="variant.status === 'Discontinued'">Discontinue</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <form class="variant-form" (ngSubmit)="saveVariant()">
        <h3>{{ selectedVariant() ? 'Edit Variant' : 'Add Variant' }}</h3>
        <label>Color
          <select name="variantColor" [(ngModel)]="variantForm.colorId" required>
            <option *ngFor="let color of colors" [value]="color.id">{{ color.code }} - {{ color.name }}</option>
          </select>
        </label>
        <label>Size
          <select name="variantSize" [(ngModel)]="variantForm.sizeId" required>
            <option *ngFor="let size of sizes" [value]="size.id">{{ size.code }} - {{ size.name }}</option>
          </select>
        </label>
        <label>Price
          <input name="variantPrice" type="number" min="0" [(ngModel)]="variantForm.price" required>
        </label>
        <label>Status
          <select name="variantStatus" [(ngModel)]="variantForm.status">
            <option value="Draft">Draft</option>
            <option value="Published">Published</option>
            <option value="Discontinued">Discontinued</option>
          </select>
        </label>
        <div class="sku-preview">SKU preview: {{ skuPreview() }}</div>
        <div class="form-actions">
          <button type="submit" [disabled]="saving() || product.status === 'Discontinued'">{{ selectedVariant() ? 'Save variant' : 'Add variant' }}</button>
          <button type="button" class="secondary" (click)="resetVariantForm()">Cancel</button>
        </div>
      </form>
    </section>
  `
})
export class ProductsPageComponent implements OnInit {
  readonly colors = seededColors;
  readonly sizes = seededSizes;
  readonly uncategorizedCategoryId = uncategorizedCategoryId;
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly products = signal<ProductDto[]>([]);
  readonly total = signal(0);
  readonly selectedProduct = signal<ProductDto | null>(null);
  readonly selectedVariant = signal<ProductVariantDto | null>(null);
  readonly validationErrors = signal<ValidationError[]>([]);
  productCodeFilter = '';
  nameFilter = '';
  skuFilter = '';
  statusFilter = '';
  colorFilter = '';
  sizeFilter = '';
  productForm: ProductForm = this.emptyProductForm();
  variantForm: ProductVariantForm = this.emptyVariantForm();

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    void this.loadProducts();
  }

  async loadProducts(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');
    try {
      const productPage = await this.api.searchProducts({
        productCode: this.productCodeFilter,
        name: this.nameFilter,
        sku: this.skuFilter,
        status: this.statusFilter,
        colorId: this.colorFilter,
        sizeId: this.sizeFilter,
        page: 1,
        pageSize: 20
      });
      this.products.set(productPage.items);
      this.total.set(productPage.total);
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  selectProduct(product: ProductDto): void {
    this.selectedProduct.set(product);
    this.productForm = {
      productCode: product.productCode || product.sku,
      name: product.name,
      description: product.description || '',
      categoryId: product.categoryId || product.category?.id || uncategorizedCategoryId,
      status: (product.status || (product.isActive ? 'Published' : 'Draft')) as EProductStatus
    };
    this.resetVariantForm();
  }

  selectVariant(variant: ProductVariantDto): void {
    this.selectedVariant.set(variant);
    this.variantForm = {
      colorId: variant.color?.id || this.colors[0].id,
      sizeId: variant.size?.id || this.sizes[3].id,
      price: variant.price,
      status: variant.status as EProductVariantStatus
    };
  }

  resetProductForm(): void {
    this.selectedProduct.set(null);
    this.productForm = this.emptyProductForm();
    this.resetVariantForm();
    this.validationErrors.set([]);
  }

  resetVariantForm(): void {
    this.selectedVariant.set(null);
    this.variantForm = this.emptyVariantForm();
  }

  async saveProduct(): Promise<void> {
    if (!this.productForm.productCode.trim() || !this.productForm.name.trim() || !this.productForm.categoryId.trim()) {
      this.validationErrors.set([{ field: 'Product', message: 'Product code, name and category are required.' }]);
      return;
    }

    this.saving.set(true);
    this.errorMessage.set('');
    this.validationErrors.set([]);
    try {
      const selectedProduct = this.selectedProduct();
      const savedProduct = selectedProduct
        ? await this.api.updateProduct(selectedProduct.id, {
            name: this.productForm.name.trim(),
            description: this.productForm.description.trim() || null,
            categoryId: this.productForm.categoryId,
            status: this.productForm.status
          })
        : await this.api.createProduct({
            productCode: this.productForm.productCode.trim(),
            name: this.productForm.name.trim(),
            description: this.productForm.description.trim() || null,
            categoryId: this.productForm.categoryId,
            variants: []
          });
      this.upsertProduct(savedProduct);
      this.selectProduct(savedProduct);
    } catch (error) {
      this.handleFormError(error);
    } finally {
      this.saving.set(false);
    }
  }

  async changeProductStatus(status: EProductStatus): Promise<void> {
    const selectedProduct = this.selectedProduct();
    if (!selectedProduct) {
      return;
    }

    if (status === 'Published' && (selectedProduct.variants?.length ?? 0) === 0) {
      this.errorMessage.set('Product can only be published after at least one variant exists.');
      return;
    }

    if (status === 'Discontinued' && !confirm('Discontinue Product\n\nSau khi discontinue, khong tao variant moi va khong nhap them ton kho. Du lieu lich su van duoc giu.\n\nBan co chac chan tiep tuc?')) {
      return;
    }

    this.productForm.status = status;
    await this.saveProduct();
  }

  async saveVariant(): Promise<void> {
    const selectedProduct = this.selectedProduct();
    if (!selectedProduct) {
      return;
    }

    if (this.variantForm.price < 0) {
      this.validationErrors.set([{ field: 'Price', message: 'Price must be greater than or equal to 0.' }]);
      return;
    }

    this.saving.set(true);
    this.errorMessage.set('');
    this.validationErrors.set([]);
    try {
      const selectedVariant = this.selectedVariant();
      const variantRequest = {
        colorId: this.variantForm.colorId,
        sizeId: this.variantForm.sizeId,
        price: this.variantForm.price,
        status: this.variantForm.status
      };
      const savedProduct = selectedVariant
        ? await this.api.updateProductVariant(selectedProduct.id, selectedVariant.id, variantRequest)
        : await this.api.addProductVariant(selectedProduct.id, variantRequest);
      this.upsertProduct(savedProduct);
      this.selectProduct(savedProduct);
    } catch (error) {
      this.handleFormError(error);
    } finally {
      this.saving.set(false);
    }
  }

  async discontinueVariant(variant: ProductVariantDto): Promise<void> {
    const selectedProduct = this.selectedProduct();
    if (!selectedProduct) {
      return;
    }

    if (!confirm(`Discontinue Variant\n\nVariant ${variant.sku} se khong duoc ban moi hoac nhap them ton kho.\n\nBan co chac chan tiep tuc?`)) {
      return;
    }

    this.saving.set(true);
    try {
      const savedProduct = await this.api.discontinueProductVariant(selectedProduct.id, variant.id);
      this.upsertProduct(savedProduct);
      this.selectProduct(savedProduct);
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    } finally {
      this.saving.set(false);
    }
  }

  publishedVariantCount(product: ProductDto): number {
    return (product.variants ?? []).filter(variant => variant.status === 'Published').length;
  }

  skuPreview(): string {
    return buildSkuPreview(this.productForm.productCode, this.variantForm.colorId, this.variantForm.sizeId);
  }

  formatPriceRange(minPrice: number | null | undefined, maxPrice: number | null | undefined): string {
    return formatPriceRange(minPrice, maxPrice);
  }

  formatDateTime(text: string | null | undefined): string {
    return formatDateTime(text);
  }

  formErrorSummary(): string[] {
    return this.validationErrors().map(error => `${error.field}: ${error.message}`);
  }

  private upsertProduct(product: ProductDto): void {
    const products = this.products().filter(existingProduct => existingProduct.id !== product.id);
    this.products.set([product, ...products]);
  }

  private emptyProductForm(): ProductForm {
    return {
      productCode: `PRD${Date.now().toString().slice(-6)}`,
      name: 'Basic T-Shirt',
      description: '',
      categoryId: uncategorizedCategoryId,
      status: 'Draft'
    };
  }

  private emptyVariantForm(): ProductVariantForm {
    return {
      colorId: this.colors[0].id,
      sizeId: this.sizes[3].id,
      price: 150000,
      status: 'Draft'
    };
  }

  private handleFormError(error: unknown): void {
    if (error instanceof ApiClientError) {
      this.validationErrors.set(error.result.validationErrors);
      this.errorMessage.set(ApiResponseReader.formatFailure(error.result));
      return;
    }

    this.errorMessage.set(this.describeError(error));
  }

  private describeError(error: unknown): string {
    if (error instanceof ApiClientError) {
      return ApiResponseReader.formatFailure(error.result);
    }

    return error instanceof Error ? error.message : 'Request failed.';
  }
}

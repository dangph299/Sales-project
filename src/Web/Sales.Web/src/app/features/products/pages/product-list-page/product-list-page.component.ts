import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTableModule } from 'ng-zorro-antd/table';
import { ApiClientError, ApiResponseReader } from '../../../../core/api/api-client-result';
import { ValidationError } from '../../../../core/api/api-error.model';
import { PageStateComponent } from '../../../../shared/components/page-state/page-state.component';
import { StatusTagComponent } from '../../../../shared/components/status-tag/status-tag.component';
import { DateTimePipe } from '../../../../shared/pipes/date-time.pipe';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { PriceRangePipe } from '../../../../shared/pipes/price-range.pipe';
import { confirmAction } from '../../../../shared/utilities/confirm-action';
import { describeApiError } from '../../../../shared/utilities/describe-api-error';
import { CommonStore } from '../../../common/services/common-store.service';
import { ProductApiService } from '../../api/product-api.service';
import { ProductResponse, ProductVariantResponse } from '../../api/responses/product.response';
import { ProductFormComponent } from '../../components/product-form/product-form.component';
import { ProductVariantFormComponent } from '../../components/product-variant-form/product-variant-form.component';
import { ProductStatus, productStatusDisplay } from '../../constants/product-status';
import { ProductVariantStatus, productVariantStatusDisplay } from '../../constants/product-variant-status';
import { ProductFormModel, emptyProductForm } from '../../models/product-form.model';
import { ProductVariantFormModel, emptyProductVariantForm } from '../../models/product-variant-form.model';

@Component({
  selector: 'app-product-list-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageStateComponent,
    StatusTagComponent,
    ProductFormComponent,
    ProductVariantFormComponent,
    DateTimePipe,
    MoneyPipe,
    PriceRangePipe,
    NzButtonModule,
    NzCardModule,
    NzEmptyModule,
    NzInputModule,
    NzModalModule,
    NzSelectModule,
    NzTableModule
  ],
  templateUrl: './product-list-page.component.html',
  styleUrl: './product-list-page.component.scss'
})
export class ProductListPageComponent implements OnInit {
  private readonly productApi = inject(ProductApiService);
  private readonly common = inject(CommonStore);
  private readonly modal = inject(NzModalService);

  readonly colors = this.common.colors;
  readonly sizes = this.common.sizes;

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly products = signal<ProductResponse[]>([]);
  readonly total = signal(0);
  readonly selectedProduct = signal<ProductResponse | null>(null);
  readonly selectedVariant = signal<ProductVariantResponse | null>(null);
  readonly validationErrors = signal<ValidationError[]>([]);

  /** The grid is read-only; editing happens in these modals, opened by an explicit action. */
  readonly productModalOpen = signal(false);
  readonly variantModalOpen = signal(false);

  productCodeFilter = '';
  nameFilter = '';
  skuFilter = '';
  statusFilter = '';
  colorFilter = '';
  sizeFilter = '';

  productForm: ProductFormModel = emptyProductForm();
  variantForm: ProductVariantFormModel = emptyProductVariantForm();

  readonly statusDisplay = productStatusDisplay;
  readonly variantStatusDisplay = productVariantStatusDisplay;
  readonly variantStatusOptions = signal<ProductVariantStatus[]>(['Draft', 'Published']);

  async ngOnInit(): Promise<void> {
    await this.common.ensureLoaded();
    await this.loadProducts();
  }

  async loadProducts(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');
    try {
      const page = await this.productApi.search({
        productCode: this.productCodeFilter,
        name: this.nameFilter,
        sku: this.skuFilter,
        status: this.statusFilter,
        colorId: this.colorFilter,
        sizeId: this.sizeFilter,
        page: 1,
        pageSize: 20
      });
      this.products.set(page.items);
      this.total.set(page.total);
      this.reselectAfterReload();
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.loading.set(false);
    }
  }

  /** Row click only changes which product's variants are shown — it never opens an editor. */
  viewProduct(product: ProductResponse): void {
    this.selectedProduct.set(product);
  }

  openCreateProduct(): void {
    this.selectedProduct.set(null);
    this.productForm = emptyProductForm(this.common.defaultCategoryId());
    this.validationErrors.set([]);
    this.errorMessage.set('');
    this.productModalOpen.set(true);
  }

  openEditProduct(product: ProductResponse): void {
    this.selectedProduct.set(product);
    this.productForm = {
      productCode: product.productCode || product.sku,
      name: product.name,
      description: product.description || '',
      categoryId: product.categoryId || product.category?.id || this.common.defaultCategoryId(),
      status: (product.status || (product.isActive ? 'Published' : 'Draft')) as ProductStatus
    };
    this.validationErrors.set([]);
    this.errorMessage.set('');
    this.productModalOpen.set(true);
  }

  closeProductModal(): void {
    this.productModalOpen.set(false);
    this.validationErrors.set([]);
  }

  openCreateVariant(): void {
    const selected = this.selectedProduct();
    if (!selected || selected.status !== 'Published') {
      this.errorMessage.set('Variants can only be added to Published products.');
      return;
    }

    this.selectedVariant.set(null);
    this.variantForm = emptyProductVariantForm(
      this.common.defaultColorId(),
      this.common.defaultSizeId());
    this.variantStatusOptions.set(['Draft', 'Published']);
    this.validationErrors.set([]);
    this.errorMessage.set('');
    this.variantModalOpen.set(true);
  }

  openEditVariant(variant: ProductVariantResponse): void {
    this.selectedVariant.set(variant);
    const statusOptions = this.allowedVariantStatuses(variant.status as ProductVariantStatus);
    this.variantForm = {
      colorId: variant.color?.id || this.common.defaultColorId(),
      sizeId: variant.size?.id || this.common.defaultSizeId(),
      price: variant.price,
      status: this.coerceVariantStatus(variant.status as ProductVariantStatus, statusOptions)
    };
    this.variantStatusOptions.set(statusOptions);
    this.validationErrors.set([]);
    this.errorMessage.set('');
    this.variantModalOpen.set(true);
  }

  closeVariantModal(): void {
    this.variantModalOpen.set(false);
    this.selectedVariant.set(null);
    this.validationErrors.set([]);
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
      const selected = this.selectedProduct();
      const saved = selected
        ? await this.productApi.update(selected.id, {
            name: this.productForm.name.trim(),
            description: this.productForm.description.trim() || null,
            categoryId: this.productForm.categoryId,
            status: this.productForm.status
          })
        : await this.productApi.create({
            productCode: this.productForm.productCode.trim(),
            name: this.productForm.name.trim(),
            description: this.productForm.description.trim() || null,
            categoryId: this.productForm.categoryId,
            variants: []
          });

      this.upsertProduct(saved);
      this.selectedProduct.set(saved);
      this.productModalOpen.set(false);
    } catch (error) {
      this.handleFormError(error);
    } finally {
      this.saving.set(false);
    }
  }

  async changeProductStatus(status: ProductStatus): Promise<void> {
    const selected = this.selectedProduct();
    if (!selected) {
      return;
    }

    if (status === 'Published' && (selected.variants?.length ?? 0) === 0) {
      this.errorMessage.set('Product can only be published after at least one variant exists.');
      return;
    }

    if (status === 'Discontinued' && !await confirmAction(
      this.modal,
      'Discontinue Product',
      'After discontinuation, new variants cannot be created and additional stock cannot be received. Historical data will be retained.')) {
      return;
    }

    this.saving.set(true);
    this.errorMessage.set('');
    try {
      const saved = await this.productApi.update(selected.id, {
        name: selected.name,
        description: selected.description || null,
        categoryId: selected.categoryId || selected.category?.id || this.common.defaultCategoryId(),
        status
      });
      this.upsertProduct(saved);
      this.selectedProduct.set(saved);
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.saving.set(false);
    }
  }

  async saveVariant(): Promise<void> {
    const selected = this.selectedProduct();
    if (!selected) {
      return;
    }

    if (selected.status !== 'Published') {
      this.validationErrors.set([{ field: 'Product', message: 'Variants can only be changed on Published products.' }]);
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
      const request = {
        colorId: this.variantForm.colorId,
        sizeId: this.variantForm.sizeId,
        price: this.variantForm.price,
        status: this.variantForm.status
      };
      const saved = selectedVariant
        ? await this.productApi.updateVariant(selected.id, selectedVariant.id, request)
        : await this.productApi.addVariant(selected.id, request);

      this.upsertProduct(saved);
      this.selectedProduct.set(saved);
      this.closeVariantModal();
    } catch (error) {
      this.handleFormError(error);
    } finally {
      this.saving.set(false);
    }
  }

  async discontinueVariant(variant: ProductVariantResponse): Promise<void> {
    const selected = this.selectedProduct();
    if (!selected) {
      return;
    }

    if (!await confirmAction(
      this.modal,
      'Discontinue Variant',
      `Variant ${variant.sku} will no longer be sold or receive additional stock.`)) {
      return;
    }

    this.saving.set(true);
    try {
      const saved = await this.productApi.discontinueVariant(selected.id, variant.id);
      this.upsertProduct(saved);
      this.selectedProduct.set(saved);
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.saving.set(false);
    }
  }

  publishedVariantCount(product: ProductResponse): number {
    return (product.variants ?? []).filter(variant => variant.status === 'Published').length;
  }

  private upsertProduct(product: ProductResponse): void {
    const products = this.products().filter(existing => existing.id !== product.id);
    this.products.set([product, ...products]);
  }

  /** Keeps the variants panel pointed at the same product after a reload replaces the list. */
  private reselectAfterReload(): void {
    const selectedId = this.selectedProduct()?.id;
    if (!selectedId) {
      return;
    }

    this.selectedProduct.set(this.products().find(product => product.id === selectedId) ?? null);
  }

  private allowedVariantStatuses(status: ProductVariantStatus): ProductVariantStatus[] {
    switch (status) {
      case 'Draft':
        return ['Draft', 'Published'];
      case 'Published':
        return ['Published', 'Discontinued'];
      case 'Discontinued':
        return ['Discontinued'];
      default:
        return ['Draft', 'Published'];
    }
  }

  private coerceVariantStatus(status: ProductVariantStatus, options: ProductVariantStatus[]): ProductVariantStatus {
    return options.includes(status) ? status : options[0];
  }

  private handleFormError(error: unknown): void {
    if (error instanceof ApiClientError) {
      this.validationErrors.set(error.result.validationErrors);
      this.errorMessage.set(ApiResponseReader.formatFailure(error.result));
      return;
    }

    this.errorMessage.set(describeApiError(error));
  }
}

import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzNotificationService } from 'ng-zorro-antd/notification';
import { ApiClientError } from '../../../../core/api/api-client-result';
import { ValidationError } from '../../../../core/api/api-error.model';
import { DataTableComponent } from '../../../../shared/components/data-table/data-table.component';
import { TableCellTemplateDirective } from '../../../../shared/components/data-table/table-cell-template.directive';
import { DropdownComponent } from '../../../../shared/components/dropdown/dropdown.component';
import { FormDialogComponent } from '../../../../shared/components/form-dialog/form-dialog.component';
import { PageStateComponent } from '../../../../shared/components/page-state/page-state.component';
import { StatusTagComponent } from '../../../../shared/components/status-tag/status-tag.component';
import { SelectOption } from '../../../../shared/models/select-option.model';
import { TablePageChange } from '../../../../shared/models/table.model';
import { FocusFirstRequiredDirective } from '../../../../shared/directives/focus-first-required.directive';
import { DateTimePipe } from '../../../../shared/pipes/date-time.pipe';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { PriceRangePipe } from '../../../../shared/pipes/price-range.pipe';
import { ApiNotifierService } from '../../../../shared/services/api-notifier.service';
import { ConfirmDeleteService } from '../../../../shared/services/confirm-delete.service';
import { confirmAction } from '../../../../shared/utilities/confirm-action';
import { ListQueryController } from '../../../../shared/utilities/list-query-controller';
import { InventoryApiService } from '../../../inventory/api/inventory-api.service';
import { CommonStore } from '../../../common/services/common-store.service';
import { ProductApiService } from '../../api/product-api.service';
import { ProductResponse, ProductVariantResponse } from '../../api/responses/product.response';
import { ProductFormComponent } from '../../components/product-form/product-form.component';
import { ProductVariantFormComponent } from '../../components/product-variant-form/product-variant-form.component';
import { ProductStatus } from '../../constants/product-status';
import {
  ProductVariantStatus,
  allowedProductVariantStatusTransitions,
  coerceProductVariantStatus,
  productVariantStatusDisplay
} from '../../constants/product-variant-status';
import { ProductFormModel, emptyProductForm } from '../../models/product-form.model';
import { ProductVariantFormModel, emptyProductVariantForm } from '../../models/product-variant-form.model';
import { productListColumns, productVariantListColumns } from './product-list.columns';

@Component({
  selector: 'app-product-list-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DataTableComponent,
    TableCellTemplateDirective,
    DropdownComponent,
    FormDialogComponent,
    PageStateComponent,
    StatusTagComponent,
    ProductFormComponent,
    ProductVariantFormComponent,
    DateTimePipe,
    MoneyPipe,
    PriceRangePipe,
    FocusFirstRequiredDirective,
    NzButtonModule,
    NzCardModule,
    NzDropDownModule,
    NzEmptyModule,
    NzInputModule,
    NzMenuModule,
    NzModalModule
  ],
  templateUrl: './product-list-page.component.html',
  styleUrl: './product-list-page.component.scss'
})
export class ProductListPageComponent implements OnInit {
  private readonly productApi = inject(ProductApiService);
  private readonly inventoryApi = inject(InventoryApiService);
  private readonly common = inject(CommonStore);
  private readonly modal = inject(NzModalService);
  private readonly notification = inject(NzNotificationService);
  private readonly apiNotifier = inject(ApiNotifierService);
  private readonly confirmDelete = inject(ConfirmDeleteService);

  readonly colors = this.common.colors;
  readonly sizes = this.common.sizes;
  readonly colorFilterOptions = computed<SelectOption<string>[]>(() => [
    { value: '', label: 'All' },
    ...this.colors().map(color => ({ value: color.id, label: color.code }))
  ]);
  readonly sizeFilterOptions = computed<SelectOption<string>[]>(() => [
    { value: '', label: 'All' },
    ...this.sizes().map(size => ({ value: size.id, label: size.code }))
  ]);

  private readonly query = new ListQueryController<{
    productCode: string;
    name: string;
    sku: string;
    colorId: string;
    sizeId: string;
  }>({
    pageIndex: 1,
    pageSize: 20,
    filters: { productCode: '', name: '', sku: '', colorId: '', sizeId: '' }
  });

  readonly loading = this.query.loading;
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly mutationErrorMessage = signal('');
  readonly products = signal<ProductResponse[]>([]);
  readonly total = signal(0);
  readonly selectedProduct = signal<ProductResponse | null>(null);
  readonly selectedVariant = signal<ProductVariantResponse | null>(null);
  readonly validationErrors = signal<ValidationError[]>([]);

  /** The grid is read-only; editing happens in these modals, opened by an explicit action. */
  readonly productModalOpen = signal(false);
  readonly variantModalOpen = signal(false);
  readonly productCreateFocusTrigger = signal(0);
  readonly variantCreateFocusTrigger = signal(0);

  productCodeFilter = '';
  nameFilter = '';
  skuFilter = '';
  colorFilter = '';
  sizeFilter = '';
  readonly pageSizeOptions = [10, 20, 50];

  get pageIndex(): number {
    return this.query.state().pageIndex;
  }

  get pageSize(): number {
    return this.query.state().pageSize;
  }

  productForm: ProductFormModel = emptyProductForm();
  variantForm: ProductVariantFormModel = emptyProductVariantForm();

  readonly variantStatusDisplay = productVariantStatusDisplay;
  readonly variantStatusOptions = signal<ProductVariantStatus[]>(['Draft', 'Published']);
  readonly tableColumns = productListColumns;
  readonly variantTableColumns = productVariantListColumns;

  async ngOnInit(): Promise<void> {
    await this.common.ensureLoaded();
    await this.loadProducts();
  }

  async loadProducts(): Promise<void> {
    this.errorMessage.set('');
    try {
      const page = await this.query.run(state => this.productApi.search({
        productCode: state.filters.productCode,
        name: state.filters.name,
        sku: state.filters.sku,
        status: '',
        colorId: state.filters.colorId,
        sizeId: state.filters.sizeId,
        page: state.pageIndex,
        pageSize: state.pageSize
      }));
      if (!page) {
        return;
      }

      if (page.items.length === 0 && page.total > 0 && this.pageIndex > 1) {
        this.query.setPage(Math.max(1, Math.ceil(page.total / this.pageSize)));
        await this.loadProducts();
        return;
      }

      this.products.set(page.items);
      this.total.set(page.total);
      this.reselectAfterReload();
    } catch (error) {
      this.notifyError('Load Products Failed', error, 'page');
    }
  }

  async searchProducts(): Promise<void> {
    this.query.setFilters({
      productCode: this.productCodeFilter,
      name: this.nameFilter,
      sku: this.skuFilter,
      colorId: this.colorFilter,
      sizeId: this.sizeFilter
    });
    await this.loadProducts();
  }

  async changeTablePage(page: TablePageChange): Promise<void> {
    this.query.setPage(page.pageIndex, page.pageSize);
    await this.loadProducts();
  }

  /** Row click only changes which product's variants are shown — it never opens an editor. */
  viewProduct(product: ProductResponse): void {
    this.selectedProduct.set(product);
  }

  openCreateProduct(): void {
    if (this.saving()) {
      return;
    }

    this.selectedProduct.set(null);
    this.productForm = emptyProductForm(this.common.defaultCategoryId());
    this.validationErrors.set([]);
    this.mutationErrorMessage.set('');
    this.productModalOpen.set(true);
  }

  openEditProduct(product: ProductResponse): void {
    if (this.saving()) {
      return;
    }

    this.selectedProduct.set(product);
    this.productForm = {
      name: product.name,
      description: product.description || '',
      categoryId: product.categoryId || product.category?.id || this.common.defaultCategoryId(),
      status: (product.status || (product.isActive ? 'Published' : 'Draft')) as ProductStatus
    };
    this.validationErrors.set([]);
    this.mutationErrorMessage.set('');
    this.productModalOpen.set(true);
  }

  closeProductModal(): void {
    if (this.saving()) {
      return;
    }

    this.productModalOpen.set(false);
    this.validationErrors.set([]);
    this.mutationErrorMessage.set('');
  }

  triggerProductCreateFocus(): void {
    if (this.productModalOpen() && !this.selectedProduct()) {
      this.productCreateFocusTrigger.update(value => value + 1);
    }
  }

  openCreateVariant(): void {
    if (this.saving()) {
      return;
    }

    const selected = this.selectedProduct();
    if (!selected) {
      this.notification.warning('Cannot Add Variant', 'Select a product before adding variants.');
      return;
    }

    this.selectedVariant.set(null);
    this.variantForm = emptyProductVariantForm(
      this.common.defaultColorId(),
      this.common.defaultSizeId());
    this.variantStatusOptions.set(['Draft', 'Published']);
    this.validationErrors.set([]);
    this.mutationErrorMessage.set('');
    this.variantModalOpen.set(true);
  }

  openEditVariant(variant: ProductVariantResponse): void {
    if (this.saving()) {
      return;
    }

    this.selectedVariant.set(variant);
    const statusOptions = allowedProductVariantStatusTransitions(variant.status as ProductVariantStatus);
    this.variantForm = {
      colorId: variant.color?.id || this.common.defaultColorId(),
      sizeId: variant.size?.id || this.common.defaultSizeId(),
      price: variant.price,
      status: coerceProductVariantStatus(variant.status as ProductVariantStatus, statusOptions)
    };
    this.variantStatusOptions.set(statusOptions);
    this.validationErrors.set([]);
    this.mutationErrorMessage.set('');
    this.variantModalOpen.set(true);
  }

  closeVariantModal(): void {
    if (this.saving()) {
      return;
    }

    this.variantModalOpen.set(false);
    this.selectedVariant.set(null);
    this.validationErrors.set([]);
    this.mutationErrorMessage.set('');
  }

  triggerVariantCreateFocus(): void {
    if (this.variantModalOpen() && !this.selectedVariant()) {
      this.variantCreateFocusTrigger.update(value => value + 1);
    }
  }

  async saveProduct(form = this.productForm): Promise<void> {
    if (this.saving()) {
      return;
    }

    this.productForm = { ...form };
    const description = form.description.trim();

    if (!form.name.trim() || !form.categoryId.trim()) {
      this.validationErrors.set([{ field: 'Product', message: 'Name and category are required.' }]);
      this.notification.warning('Check Product Form', 'Name and category are required.');
      return;
    }

    this.saving.set(true);
    this.mutationErrorMessage.set('');
    this.validationErrors.set([]);
    try {
      const selected = this.selectedProduct();
      const saved = selected
        ? await this.productApi.update(selected.id, {
            name: form.name.trim(),
            description: description || null,
            categoryId: form.categoryId,
            status: form.status
          })
        : await this.productApi.create({
            name: form.name.trim(),
            description: description || null,
            categoryId: form.categoryId,
            variants: []
          });

      this.upsertProduct(saved);
      this.selectedProduct.set(saved);
      this.productModalOpen.set(false);
      this.productForm = emptyProductForm(this.common.defaultCategoryId());
      this.validationErrors.set([]);
      this.mutationErrorMessage.set('');
      await this.loadProducts();
      this.apiNotifier.success(selected ? 'Product Updated' : 'Product Created', `${saved.productCode || saved.sku} - ${saved.name}`);
    } catch (error) {
      this.handleFormError(error);
    } finally {
      this.saving.set(false);
    }
  }

  async saveVariant(): Promise<void> {
    if (this.saving()) {
      return;
    }

    const selected = this.selectedProduct();
    if (!selected) {
      return;
    }

    if (this.variantForm.price < 0) {
      this.validationErrors.set([{ field: 'Price', message: 'Price must be greater than or equal to 0.' }]);
      this.notification.warning('Check Variant Form', 'Price must be greater than or equal to 0.');
      return;
    }

    this.saving.set(true);
    this.mutationErrorMessage.set('');
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
      this.variantModalOpen.set(false);
      this.selectedVariant.set(null);
      this.variantForm = emptyProductVariantForm(
        this.common.defaultColorId(),
        this.common.defaultSizeId());
      this.validationErrors.set([]);
      this.mutationErrorMessage.set('');
      await this.loadProducts();
      this.apiNotifier.success(selectedVariant ? 'Variant Updated' : 'Variant Added', saved.productCode || saved.sku);
    } catch (error) {
      this.handleFormError(error);
    } finally {
      this.saving.set(false);
    }
  }

  async discontinueVariant(variant: ProductVariantResponse): Promise<void> {
    if (this.saving()) {
      return;
    }

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
      this.apiNotifier.success('Variant Discontinued', variant.sku);
    } catch (error) {
      this.notifyError('Discontinue Variant Failed', error);
    } finally {
      this.saving.set(false);
    }
  }

  async changeVariantStatus(variant: ProductVariantResponse, status: ProductVariantStatus): Promise<void> {
    if (this.saving()) {
      return;
    }

    const selected = this.selectedProduct();
    if (!selected) {
      return;
    }

    if (variant.status === status) {
      return;
    }

    if (status === 'Discontinued') {
      await this.discontinueVariant(variant);
      return;
    }

    this.saving.set(true);
    this.mutationErrorMessage.set('');
    try {
      const saved = await this.productApi.updateVariant(selected.id, variant.id, {
        colorId: variant.color?.id || this.common.defaultColorId(),
        sizeId: variant.size?.id || this.common.defaultSizeId(),
        price: variant.price,
        status
      });
      this.upsertProduct(saved);
      this.selectedProduct.set(saved);
      this.apiNotifier.success('Variant Status Updated', `${variant.sku} is now ${status}.`);
    } catch (error) {
      this.notifyError('Update Variant Status Failed', error);
    } finally {
      this.saving.set(false);
    }
  }

  async deleteVariant(variant: ProductVariantResponse): Promise<void> {
    if (this.saving()) {
      return;
    }

    const selected = this.selectedProduct();
    if (!selected) {
      return;
    }

    if (!this.canDeleteVariant(variant)) {
      this.notification.warning('Cannot Delete Variant', 'Only draft or discontinued variants can be deleted.');
      return;
    }

    if (await this.hasVariantStock(variant)) {
      this.notification.error('Cannot Delete Variant', `${variant.sku} still has inventory. Discontinue it instead.`);
      return;
    }

    if (!await this.confirmDelete.open({
      title: 'Delete Variant',
      itemName: variant.sku,
      warningMessage: 'This removes the variant from active catalog management.'
    })) {
      return;
    }

    this.saving.set(true);
    this.mutationErrorMessage.set('');
    try {
      await this.productApi.deleteVariant(selected.id, variant.id);
      await this.loadProducts();
      this.apiNotifier.success('Variant Deleted', variant.sku);
    } catch (error) {
      this.notifyError('Delete Variant Failed', error);
    } finally {
      this.saving.set(false);
    }
  }

  async deleteProduct(product: ProductResponse): Promise<void> {
    if (this.saving()) {
      return;
    }

    if (!this.canDeleteProduct(product)) {
      this.notification.warning(
        'Cannot Delete Product',
        `${product.productCode || product.sku} must be draft or discontinued before it can be deleted.`);
      return;
    }

    if (await this.hasProductStock(product)) {
      this.notification.error(
        'Cannot Delete Product',
        `${product.productCode || product.sku} still has inventory. Discontinue it instead.`);
      return;
    }

    if (!await this.confirmDelete.open({
      title: 'Delete Product',
      itemName: `${product.productCode || product.sku} - ${product.name}`,
      warningMessage: 'This removes the product from active catalog management.'
    })) {
      return;
    }

    this.saving.set(true);
    this.mutationErrorMessage.set('');
    try {
      await this.productApi.delete(product.id);
      if (this.selectedProduct()?.id === product.id) {
        this.selectedProduct.set(null);
      }
      await this.loadProducts();
      this.apiNotifier.success('Product Deleted', `${product.productCode || product.sku} - ${product.name}`);
    } catch (error) {
      this.notifyError('Delete Product Failed', error);
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

  canDeleteProduct(product: ProductResponse): boolean {
    return !product.isDelete;
  }

  canDeleteVariant(variant: ProductVariantResponse): boolean {
    return variant.status === 'Draft' || variant.status === 'Discontinued';
  }

  private async hasProductStock(product: ProductResponse): Promise<boolean> {
    for (const variant of product.variants ?? []) {
      if (await this.hasVariantStock(variant)) {
        return true;
      }
    }

    return false;
  }

  private async hasVariantStock(variant: ProductVariantResponse): Promise<boolean> {
    try {
      const inventory = await this.inventoryApi.getByVariant(variant.id);
      return !!inventory && (inventory.available > 0 || inventory.reserved > 0);
    } catch (error) {
      this.notifyError('Inventory Check Failed', error);
      return true;
    }
  }

  private handleFormError(error: unknown): void {
    if (error instanceof ApiClientError) {
      this.validationErrors.set(error.result.validationErrors);
      this.notifyError('Save Product Failed', error, 'mutation');
      return;
    }

    this.notifyError('Save Product Failed', error, 'mutation');
  }

  private notifyError(title: string, error: unknown, target: 'page' | 'mutation' = 'mutation'): void {
    const message = this.apiNotifier.error(title, error);
    if (target === 'page') {
      this.errorMessage.set(message);
    } else {
      this.mutationErrorMessage.set(message);
    }
  }
}

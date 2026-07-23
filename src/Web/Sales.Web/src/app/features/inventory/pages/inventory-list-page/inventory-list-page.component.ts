import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzNotificationService } from 'ng-zorro-antd/notification';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { PageStateComponent } from '../../../../shared/components/page-state/page-state.component';
import { StatusTagComponent } from '../../../../shared/components/status-tag/status-tag.component';
import { confirmAction } from '../../../../shared/utilities/confirm-action';
import { describeApiError } from '../../../../shared/utilities/describe-api-error';
import { ProductLookupApiService } from '../../../common/api/product-lookup-api.service';
import { productLookupStatusDisplay } from '../../../common/constants/product-lookup-status';
import { ProductLookupResponse, ProductVariantLookupResponse } from '../../../common/contracts/product-lookup.response';
import { InventoryApiService } from '../../api/inventory-api.service';
import { InventoryResponse } from '../../api/responses/inventory.response';
import { StockAdjustmentFormComponent } from '../../components/stock-adjustment-form/stock-adjustment-form.component';
import { StockState, lowStockThreshold, stockStateLabels, stockStateOf } from '../../constants/stock-state';
import { StockAdjustmentFormModel, emptyStockAdjustmentForm } from '../../models/stock-adjustment-form.model';
import { StockRow, totalQuantity } from '../../models/stock-row.model';

type StockSortKey = 'sku' | 'product' | 'color' | 'size' | 'status';
type SortDirection = 'ascend' | 'descend';

@Component({
  selector: 'app-inventory-list-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageStateComponent,
    StatusTagComponent,
    StockAdjustmentFormComponent,
    NzButtonModule,
    NzCardModule,
    NzInputModule,
    NzModalModule,
    NzSelectModule,
    NzTableModule,
    NzTagModule
  ],
  templateUrl: './inventory-list-page.component.html',
  styleUrl: './inventory-list-page.component.scss'
})
export class InventoryListPageComponent implements OnInit {
  private readonly inventoryApi = inject(InventoryApiService);
  private readonly productLookup = inject(ProductLookupApiService);
  private readonly modal = inject(NzModalService);
  private readonly notification = inject(NzNotificationService);

  readonly lowStockThreshold = lowStockThreshold;
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly mutationErrorMessage = signal('');
  readonly stockRows = signal<StockRow[]>([]);
  /** Number of variants matching the filters, which is what the pager walks through. */
  readonly total = signal(0);
  readonly selectedRow = signal<StockRow | null>(null);
  readonly adjustmentModalOpen = signal(false);

  skuFilter = '';
  nameFilter = '';
  stockStateFilter: StockState | '' = '';
  pageIndex = 1;
  pageSize = 20;
  readonly pageSizeOptions = [10, 20, 50];
  adjustmentForm: StockAdjustmentFormModel = emptyStockAdjustmentForm();

  readonly sortKey = signal<StockSortKey>('sku');
  readonly sortDirection = signal<SortDirection>('ascend');

  readonly statusDisplay = productLookupStatusDisplay;
  readonly totalQuantity = totalQuantity;

  ngOnInit(): void {
    void this.loadStockRows();
  }

  async loadStockRows(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');
    try {
      const page = await this.productLookup.searchVariants({
        sku: this.skuFilter,
        productName: this.nameFilter,
        sortBy: this.sortKey(),
        sortDirection: this.sortDirection(),
        page: this.pageIndex,
        pageSize: this.pageSize
      });

      // Deleting the last products on the final page leaves the pager past the end, which would
      // otherwise show an empty grid with no way back.
      if (page.items.length === 0 && page.total > 0 && this.pageIndex > 1) {
        this.pageIndex = Math.max(1, Math.ceil(page.total / this.pageSize));
        await this.loadStockRows();
        return;
      }

      const variantIds = page.items.map(item => item.productVariantId);
      const inventoryBatch = await this.inventoryApi.getByVariants(variantIds);
      const inventoryByVariantId = new Map(inventoryBatch.items.map(item => [item.productId, item]));
      const rows: StockRow[] = page.items.map(item => ({
        product: this.toProduct(item),
        variant: this.toVariant(item),
        inventory: inventoryByVariantId.get(item.productVariantId) ?? this.emptyInventory(item.productVariantId, item.sku)
      }));

      this.stockRows.set(rows);
      this.total.set(page.total);
      this.selectedRow.set(rows[0] ?? null);
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.loading.set(false);
    }
  }

  search(): void {
    this.pageIndex = 1;
    void this.loadStockRows();
  }

  changePage(pageIndex: number): void {
    this.pageIndex = pageIndex;
    void this.loadStockRows();
  }

  changePageSize(pageSize: number): void {
    this.pageSize = pageSize;
    this.pageIndex = 1;
    void this.loadStockRows();
  }

  filteredRows(): StockRow[] {
    return this.stockStateFilter === ''
      ? this.stockRows()
      : this.stockRows().filter(row => this.stockState(row) === this.stockStateFilter);
  }

  sortBy(key: StockSortKey): void {
    if (this.sortKey() !== key) {
      this.sortKey.set(key);
      this.sortDirection.set('ascend');
      this.pageIndex = 1;
      void this.loadStockRows();
      return;
    }

    this.sortDirection.set(this.sortDirection() === 'ascend' ? 'descend' : 'ascend');
    this.pageIndex = 1;
    void this.loadStockRows();
  }

  sortIndicator(key: StockSortKey): string {
    if (this.sortKey() !== key) {
      return '';
    }

    return this.sortDirection() === 'ascend' ? '\u2191' : '\u2193';
  }

  selectRow(row: StockRow): void {
    this.selectedRow.set(row);
  }

  openAdjustment(row: StockRow): void {
    if (this.saving()) {
      return;
    }

    this.selectedRow.set(row);
    this.adjustmentForm = emptyStockAdjustmentForm();
    this.mutationErrorMessage.set('');
    this.adjustmentModalOpen.set(true);
  }

  closeAdjustment(): void {
    if (this.saving()) {
      return;
    }

    this.adjustmentModalOpen.set(false);
    this.adjustmentForm = emptyStockAdjustmentForm();
    this.mutationErrorMessage.set('');
  }

  stockState(row: StockRow): StockState {
    return stockStateOf(row.inventory?.available, this.lowStockThreshold);
  }

  stockStateText(row: StockRow): string {
    return stockStateLabels[this.stockState(row)];
  }

  async adjustInventory(): Promise<void> {
    if (this.saving()) {
      return;
    }

    const row = this.selectedRow();
    if (!row) {
      return;
    }

    if (!await confirmAction(
      this.modal,
      'Adjust Inventory',
      `SKU ${row.variant.sku} will be adjusted by ${this.adjustmentForm.quantityDelta} units.`)) {
      return;
    }

    this.saving.set(true);
    this.mutationErrorMessage.set('');
    try {
      const inventory = await this.inventoryApi.adjust(row.variant.id, {
        sku: row.variant.sku,
        quantityDelta: this.adjustmentForm.quantityDelta
      });

      const updatedRows = this.stockRows().map(existing =>
        existing.variant.id === row.variant.id ? { ...existing, inventory } : existing);
      this.stockRows.set(updatedRows);
      this.selectedRow.set(updatedRows.find(existing => existing.variant.id === row.variant.id) ?? null);
      this.adjustmentModalOpen.set(false);
      this.adjustmentForm = emptyStockAdjustmentForm();
      this.mutationErrorMessage.set('');
    } catch (error) {
      const message = describeApiError(error);
      this.mutationErrorMessage.set(message);
      this.notification.error('Adjust Inventory Failed', message);
    } finally {
      this.saving.set(false);
    }
  }

  private toProduct(item: {
    productId: string;
    productCode: string;
    productName: string;
    productStatus: string;
  }): ProductLookupResponse {
    return {
      id: item.productId,
      sku: item.productCode,
      productCode: item.productCode,
      name: item.productName,
      status: item.productStatus,
      variants: []
    };
  }

  private toVariant(item: {
    productVariantId: string;
    sku: string;
    color?: ProductVariantLookupResponse['color'];
    size?: ProductVariantLookupResponse['size'];
    price: number;
    variantStatus: string;
  }): ProductVariantLookupResponse {
    return {
      id: item.productVariantId,
      sku: item.sku,
      color: item.color,
      size: item.size,
      price: item.price,
      status: item.variantStatus
    };
  }

  private emptyInventory(productVariantId: string, sku: string): InventoryResponse {
    return {
      productId: productVariantId,
      sku,
      available: 0,
      reserved: 0,
      version: 0
    };
  }
}

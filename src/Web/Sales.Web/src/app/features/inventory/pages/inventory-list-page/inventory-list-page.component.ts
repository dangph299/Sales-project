import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { DataTableComponent } from '../../../../shared/components/data-table/data-table.component';
import { TableCellTemplateDirective } from '../../../../shared/components/data-table/table-cell-template.directive';
import { DropdownComponent } from '../../../../shared/components/dropdown/dropdown.component';
import { FormDialogComponent } from '../../../../shared/components/form-dialog/form-dialog.component';
import { PageStateComponent } from '../../../../shared/components/page-state/page-state.component';
import { StatusTagComponent } from '../../../../shared/components/status-tag/status-tag.component';
import { ApiNotifierService } from '../../../../shared/services/api-notifier.service';
import { TablePageChange, TableSort } from '../../../../shared/models/table.model';
import { ListQueryController } from '../../../../shared/utilities/list-query-controller';
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
import { StockRow, canAdjustStock, totalQuantity } from '../../models/stock-row.model';
import { inventoryListColumns } from './inventory-list.columns';

type StockSortKey = 'sku' | 'product' | 'color' | 'size' | 'status';
type SortDirection = 'ascend' | 'descend';

@Component({
  selector: 'app-inventory-list-page',
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
    StockAdjustmentFormComponent,
    NzButtonModule,
    NzCardModule,
    NzInputModule,
    NzModalModule,
    NzTagModule
  ],
  templateUrl: './inventory-list-page.component.html',
  styleUrl: './inventory-list-page.component.scss'
})
export class InventoryListPageComponent implements OnInit {
  private readonly inventoryApi = inject(InventoryApiService);
  private readonly productLookup = inject(ProductLookupApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly modal = inject(NzModalService);
  private readonly apiNotifier = inject(ApiNotifierService);

  private readonly query = new ListQueryController<{ sku: string; name: string }>({
    pageIndex: 1,
    pageSize: 20,
    filters: { sku: '', name: '' }
  });

  readonly lowStockThreshold = lowStockThreshold;
  readonly loading = this.query.loading;
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
  readonly pageSizeOptions = [10, 20, 50];
  readonly stockStateOptions: { value: StockState | ''; label: string }[] = [
    { value: '', label: 'All' },
    { value: 'available', label: 'Available' },
    { value: 'low', label: 'Low stock' },
    { value: 'out', label: 'Out of stock' }
  ];
  adjustmentForm: StockAdjustmentFormModel = emptyStockAdjustmentForm();

  readonly sortKey = signal<StockSortKey>('sku');
  readonly sortDirection = signal<SortDirection>('ascend');
  readonly tableColumns = inventoryListColumns;

  readonly statusDisplay = productLookupStatusDisplay;
  readonly totalQuantity = totalQuantity;
  readonly tableSort = computed<TableSort>(() => ({ key: this.sortKey(), direction: this.sortDirection() }));
  readonly rowIdentity = (row: StockRow): string => row.variant.id;

  get pageIndex(): number {
    return this.query.state().pageIndex;
  }

  get pageSize(): number {
    return this.query.state().pageSize;
  }

  ngOnInit(): void {
    this.applyStockFilterFromRoute();
    void this.loadStockRows();
  }

  async loadStockRows(): Promise<void> {
    this.errorMessage.set('');
    try {
      const page = await this.query.run(state => this.productLookup.searchVariants({
        sku: state.filters.sku,
        productName: state.filters.name,
        sortBy: this.sortKey(),
        sortDirection: this.sortDirection(),
        page: state.pageIndex,
        pageSize: state.pageSize
      }));
      if (!page) {
        return;
      }

      // Deleting the last products on the final page leaves the pager past the end, which would
      // otherwise show an empty grid with no way back.
      if (page.items.length === 0 && page.total > 0 && this.pageIndex > 1) {
        this.query.setPage(Math.max(1, Math.ceil(page.total / this.pageSize)));
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
    }
  }

  search(): void {
    this.query.setFilters({ sku: this.skuFilter, name: this.nameFilter });
    void this.loadStockRows();
  }

  async changeTablePage(page: TablePageChange): Promise<void> {
    this.query.setPage(page.pageIndex, page.pageSize);
    await this.loadStockRows();
  }

  filteredRows(): StockRow[] {
    return this.stockStateFilter === ''
      ? this.stockRows()
      : this.stockRows().filter(row => this.stockState(row) === this.stockStateFilter);
  }

  changeTableSort(sort: TableSort): void {
    if (!sort.direction) {
      this.sortKey.set('sku');
      this.sortDirection.set('ascend');
    } else {
      this.sortKey.set(sort.key as StockSortKey);
      this.sortDirection.set(sort.direction);
    }

    this.query.setPage(1);
    void this.loadStockRows();
  }

  selectRow(row: StockRow): void {
    this.selectedRow.set(row);
  }

  canAdjustSelected(): boolean {
    const row = this.selectedRow();
    return !!row && canAdjustStock(row);
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

  private applyStockFilterFromRoute(): void {
    const stock = this.route.snapshot.queryParamMap.get('stock');
    switch (stock) {
      case 'in-stock':
        this.stockStateFilter = 'available';
        break;
      case 'low':
        this.stockStateFilter = 'low';
        break;
      case 'out':
        this.stockStateFilter = 'out';
        break;
    }
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
      this.mutationErrorMessage.set(this.apiNotifier.error('Adjust Inventory Failed', error));
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

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
import { InventoryApiService } from '../../api/inventory-api.service';
import { StockAdjustmentFormComponent } from '../../components/stock-adjustment-form/stock-adjustment-form.component';
import { StockState, lowStockThreshold, stockStateLabels, stockStateOf } from '../../constants/stock-state';
import { StockAdjustmentFormModel, emptyStockAdjustmentForm } from '../../models/stock-adjustment-form.model';
import { StockRow, totalQuantity } from '../../models/stock-row.model';

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
  readonly selectedRow = signal<StockRow | null>(null);
  readonly adjustmentModalOpen = signal(false);

  skuFilter = '';
  nameFilter = '';
  stockStateFilter: StockState | '' = '';
  adjustmentForm: StockAdjustmentFormModel = emptyStockAdjustmentForm();

  readonly statusDisplay = productLookupStatusDisplay;
  readonly totalQuantity = totalQuantity;

  ngOnInit(): void {
    void this.loadStockRows();
  }

  async loadStockRows(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');
    try {
      // Every variant, whatever its product's lifecycle state: stock is physical, and a product is
      // normally received into the warehouse before it is published. Filtering by product status
      // left new variants with no row, and so no way to adjust them.
      const page = await this.productLookup.search({
        sku: this.skuFilter,
        name: this.nameFilter,
        status: '',
        page: 1,
        pageSize: 20
      });

      // One inventory read per variant, issued together: awaiting them in the loop serialised the
      // whole grid behind up to (products x variants) sequential round trips.
      const pairs = page.items.flatMap(product =>
        (product.variants ?? []).map(variant => ({ product, variant })));
      const inventories = await Promise.all(
        pairs.map(pair => this.inventoryApi.getByVariant(pair.variant.id)));
      const rows: StockRow[] = pairs.map((pair, index) => ({
        product: pair.product,
        variant: pair.variant,
        inventory: inventories[index]
      }));

      this.stockRows.set(rows);
      this.selectedRow.set(rows[0] ?? null);
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.loading.set(false);
    }
  }

  filteredRows(): StockRow[] {
    if (this.stockStateFilter === '') {
      return this.stockRows();
    }

    return this.stockRows().filter(row => this.stockState(row) === this.stockStateFilter);
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
}

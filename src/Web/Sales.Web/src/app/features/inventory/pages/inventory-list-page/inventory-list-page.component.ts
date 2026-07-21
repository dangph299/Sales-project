import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
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

  readonly lowStockThreshold = lowStockThreshold;
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
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
      const page = await this.productLookup.search({
        sku: this.skuFilter,
        name: this.nameFilter,
        status: 'Published',
        page: 1,
        pageSize: 20
      });

      const rows: StockRow[] = [];
      for (const product of page.items) {
        for (const variant of product.variants ?? []) {
          const inventory = await this.inventoryApi.getByVariant(variant.id);
          rows.push({ product, variant, inventory });
        }
      }

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
    this.selectedRow.set(row);
    this.adjustmentForm = emptyStockAdjustmentForm();
    this.errorMessage.set('');
    this.adjustmentModalOpen.set(true);
  }

  closeAdjustment(): void {
    this.adjustmentModalOpen.set(false);
    this.adjustmentForm = emptyStockAdjustmentForm();
  }

  stockState(row: StockRow): StockState {
    return stockStateOf(row.inventory?.available, this.lowStockThreshold);
  }

  stockStateText(row: StockRow): string {
    return stockStateLabels[this.stockState(row)];
  }

  async adjustInventory(): Promise<void> {
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
    try {
      const inventory = await this.inventoryApi.adjust(row.variant.id, {
        sku: row.variant.sku,
        quantityDelta: this.adjustmentForm.quantityDelta
      });

      const updatedRows = this.stockRows().map(existing =>
        existing.variant.id === row.variant.id ? { ...existing, inventory } : existing);
      this.stockRows.set(updatedRows);
      this.selectedRow.set(updatedRows.find(existing => existing.variant.id === row.variant.id) ?? null);
      this.closeAdjustment();
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.saving.set(false);
    }
  }
}

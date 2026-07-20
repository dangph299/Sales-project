import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClientError, ApiResponseReader } from '../api-client-result';
import { ApiService } from '../api.service';
import { InventoryDto, ProductDto, ProductVariantDto } from '../models';
import { MoneyDisplayComponent } from '../shared/money-display.component';
import { PageStateComponent } from '../shared/page-state.component';
import { StatusBadgeComponent } from '../shared/status-badge.component';

interface StockRow {
  product: ProductDto;
  variant: ProductVariantDto;
  inventory: InventoryDto | null;
}

@Component({
  selector: 'app-inventory-page',
  standalone: true,
  imports: [CommonModule, FormsModule, MoneyDisplayComponent, PageStateComponent, StatusBadgeComponent],
  template: `
    <section class="page-header">
      <div>
        <p class="eyebrow">Inventory</p>
        <h1>Stock Overview</h1>
        <p>Ton kho duoc quan ly theo ProductVariantId/SKU, khong theo ProductId chung.</p>
      </div>
      <button type="button" (click)="loadStockRows()">Refresh stock</button>
    </section>

    <section class="toolbar">
      <label>SKU
        <input name="inventorySku" [(ngModel)]="skuFilter" (keyup.enter)="loadStockRows()">
      </label>
      <label>Product name
        <input name="inventoryProductName" [(ngModel)]="nameFilter" (keyup.enter)="loadStockRows()">
      </label>
      <label>Stock state
        <select name="stockState" [(ngModel)]="stockStateFilter">
          <option value="">All</option>
          <option value="available">Available</option>
          <option value="low">Low stock</option>
          <option value="out">Out of stock</option>
        </select>
      </label>
      <button type="button" (click)="loadStockRows()">Search</button>
    </section>

    <app-page-state [loading]="loading()" [errorMessage]="errorMessage()" [empty]="filteredRows().length === 0" emptyTitle="Chua co variant" emptyText="Tim product published co variant de xem ton kho." (retry)="loadStockRows()"></app-page-state>

    <section class="content-grid two" *ngIf="!loading()">
      <article class="panel-card wide">
        <div class="section-title">
          <h2>Stock Overview</h2>
          <span>Low threshold: {{ lowStockThreshold }}</span>
        </div>
        <div class="table-wrap" *ngIf="filteredRows().length > 0">
          <table class="data-table">
            <thead>
              <tr>
                <th>SKU</th>
                <th>Product</th>
                <th>Color</th>
                <th>Size</th>
                <th>Status</th>
                <th>Available</th>
                <th>Reserved</th>
                <th>Total</th>
                <th>State</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let stockRow of filteredRows()" (click)="selectRow(stockRow)" [class.selected]="selectedRow()?.variant?.id === stockRow.variant.id">
                <td>{{ stockRow.variant.sku }}</td>
                <td>{{ stockRow.product.name }}</td>
                <td>{{ stockRow.variant.color?.code || '-' }}</td>
                <td>{{ stockRow.variant.size?.code || '-' }}</td>
                <td>
                  <app-status-badge [status]="stockRow.product.status"></app-status-badge>
                  <app-status-badge [status]="stockRow.variant.status"></app-status-badge>
                </td>
                <td>{{ stockRow.inventory?.available ?? 0 }}</td>
                <td>{{ stockRow.inventory?.reserved ?? 0 }}</td>
                <td>{{ totalQuantity(stockRow) }}</td>
                <td><span class="stock-chip" [class]="stockState(stockRow)">{{ stockStateText(stockRow) }}</span></td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>

      <article class="panel-card">
        <div class="section-title">
          <h2>Receive / Adjust Stock</h2>
        </div>
        <section class="detail-panel" *ngIf="selectedRow() as stockRow; else noSelection">
          <dl>
            <dt>SKU</dt><dd>{{ stockRow.variant.sku }}</dd>
            <dt>Product</dt><dd>{{ stockRow.product.name }}</dd>
            <dt>Unit price</dt><dd><app-money-display [amount]="stockRow.variant.price"></app-money-display></dd>
            <dt>Current available</dt><dd>{{ stockRow.inventory?.available ?? 0 }}</dd>
          </dl>
          <form (ngSubmit)="adjustInventory()" class="form-grid">
            <label>Operation
              <select name="adjustmentOperation" [(ngModel)]="operation">
                <option value="receive">Receive Stock</option>
                <option value="adjust">Adjust Stock</option>
              </select>
            </label>
            <label>Adjustment quantity
              <input name="quantityDelta" type="number" [(ngModel)]="quantityDelta" required>
            </label>
            <label>Reason
              <input name="adjustmentReason" [(ngModel)]="reason" placeholder="Purchase receipt, cycle count...">
            </label>
            <div class="notice-panel" *ngIf="!canAdjust(stockRow)">
              Chi co Product Published va Variant Published moi duoc nhap ton kho.
            </div>
            <div class="form-actions">
              <button type="submit" [disabled]="saving() || !canAdjust(stockRow)">Apply adjustment</button>
            </div>
          </form>
        </section>
        <ng-template #noSelection>
          <app-page-state [empty]="true" emptyTitle="Chua chon variant" emptyText="Chon mot SKU trong bang de xem va dieu chinh ton kho."></app-page-state>
        </ng-template>
      </article>
    </section>
  `
})
export class InventoryPageComponent implements OnInit {
  readonly lowStockThreshold = 5;
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly stockRows = signal<StockRow[]>([]);
  readonly selectedRow = signal<StockRow | null>(null);
  skuFilter = '';
  nameFilter = '';
  stockStateFilter = '';
  operation: 'receive' | 'adjust' = 'receive';
  quantityDelta = 10;
  reason = '';

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    void this.loadStockRows();
  }

  async loadStockRows(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');
    try {
      const productPage = await this.api.searchProducts({
        sku: this.skuFilter,
        name: this.nameFilter,
        status: 'Published',
        page: 1,
        pageSize: 20
      });
      const stockRows: StockRow[] = [];
      for (const product of productPage.items) {
        for (const variant of product.variants ?? []) {
          const inventory = await this.api.getInventory(variant.id);
          stockRows.push({ product, variant, inventory });
        }
      }
      this.stockRows.set(stockRows);
      this.selectedRow.set(stockRows[0] ?? null);
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  filteredRows(): StockRow[] {
    return this.stockRows().filter(stockRow => {
      if (this.stockStateFilter === '') {
        return true;
      }

      return this.stockState(stockRow) === this.stockStateFilter;
    });
  }

  selectRow(stockRow: StockRow): void {
    this.selectedRow.set(stockRow);
  }

  async adjustInventory(): Promise<void> {
    const stockRow = this.selectedRow();
    if (!stockRow) {
      return;
    }

    if (!confirm(`Adjust Inventory\n\nSKU ${stockRow.variant.sku} se duoc dieu chinh ${this.quantityDelta} don vi.\n\nBan co chac chan tiep tuc?`)) {
      return;
    }

    this.saving.set(true);
    try {
      const inventory = await this.api.adjustInventory(stockRow.variant.id, stockRow.variant.sku, this.quantityDelta);
      const updatedRows = this.stockRows().map(existingRow =>
        existingRow.variant.id === stockRow.variant.id ? { ...existingRow, inventory } : existingRow);
      this.stockRows.set(updatedRows);
      this.selectedRow.set(updatedRows.find(existingRow => existingRow.variant.id === stockRow.variant.id) ?? null);
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    } finally {
      this.saving.set(false);
    }
  }

  canAdjust(stockRow: StockRow): boolean {
    return stockRow.product.status === 'Published' && stockRow.variant.status === 'Published';
  }

  totalQuantity(stockRow: StockRow): number {
    return (stockRow.inventory?.available ?? 0) + (stockRow.inventory?.reserved ?? 0);
  }

  stockState(stockRow: StockRow): 'available' | 'low' | 'out' {
    const available = stockRow.inventory?.available ?? 0;
    if (available <= 0) {
      return 'out';
    }

    if (available <= this.lowStockThreshold) {
      return 'low';
    }

    return 'available';
  }

  stockStateText(stockRow: StockRow): string {
    const state = this.stockState(stockRow);
    if (state === 'out') {
      return 'Out of stock';
    }

    if (state === 'low') {
      return 'Low stock';
    }

    return 'Available';
  }

  private describeError(error: unknown): string {
    if (error instanceof ApiClientError) {
      return ApiResponseReader.formatFailure(error.result);
    }

    return error instanceof Error ? error.message : 'Request failed.';
  }
}

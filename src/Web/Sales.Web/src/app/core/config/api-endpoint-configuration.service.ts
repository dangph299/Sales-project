import { Injectable, signal } from '@angular/core';

const salesBaseStorageKey = 'salesBase';
const inventoryBaseStorageKey = 'inventoryBase';

const defaultSalesBase = '/sales-api';
const defaultInventoryBase = '/inventory-api';

/** Owns the configurable base URLs of the two backend APIs. */
@Injectable({ providedIn: 'root' })
export class ApiEndpointConfigurationService {
  readonly salesBase = signal(localStorage.getItem(salesBaseStorageKey) || defaultSalesBase);
  readonly inventoryBase = signal(localStorage.getItem(inventoryBaseStorageKey) || defaultInventoryBase);

  setBaseUrls(salesBase: string, inventoryBase: string): void {
    this.salesBase.set(salesBase.trim() || defaultSalesBase);
    this.inventoryBase.set(inventoryBase.trim() || defaultInventoryBase);
    localStorage.setItem(salesBaseStorageKey, this.salesBase());
    localStorage.setItem(inventoryBaseStorageKey, this.inventoryBase());
  }
}

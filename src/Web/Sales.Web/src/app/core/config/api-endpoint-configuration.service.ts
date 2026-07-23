import { Injectable, signal } from '@angular/core';

const salesBaseStorageKey = 'salesBase';
const inventoryBaseStorageKey = 'inventoryBase';
const dashboardBaseStorageKey = 'dashboardBase';

const defaultSalesBase = '/sales-api';
const defaultInventoryBase = '/inventory-api';
const defaultDashboardBase = '/dashboard-api';

/** Owns the configurable base URLs of the backend APIs. */
@Injectable({ providedIn: 'root' })
export class ApiEndpointConfigurationService {
  readonly salesBase = signal(localStorage.getItem(salesBaseStorageKey) || defaultSalesBase);
  readonly inventoryBase = signal(localStorage.getItem(inventoryBaseStorageKey) || defaultInventoryBase);
  readonly dashboardBase = signal(localStorage.getItem(dashboardBaseStorageKey) || defaultDashboardBase);

  setBaseUrls(salesBase: string, inventoryBase: string, dashboardBase = this.dashboardBase()): void {
    this.salesBase.set(salesBase.trim() || defaultSalesBase);
    this.inventoryBase.set(inventoryBase.trim() || defaultInventoryBase);
    this.dashboardBase.set(dashboardBase.trim() || defaultDashboardBase);
    localStorage.setItem(salesBaseStorageKey, this.salesBase());
    localStorage.setItem(inventoryBaseStorageKey, this.inventoryBase());
    localStorage.setItem(dashboardBaseStorageKey, this.dashboardBase());
  }
}

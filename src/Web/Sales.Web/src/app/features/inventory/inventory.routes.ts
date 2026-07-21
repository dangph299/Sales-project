import { Routes } from '@angular/router';

export const inventoryRoutes: Routes = [
  {
    path: '',
    data: { breadcrumb: 'Stock Overview', title: 'Stock Overview' },
    loadComponent: () => import('./pages/inventory-list-page/inventory-list-page.component')
      .then(component => component.InventoryListPageComponent)
  }
];

import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard'
  },
  {
    path: 'dashboard',
    data: { breadcrumb: 'Dashboard', title: 'Dashboard' },
    loadComponent: () => import('./pages/dashboard-page.component').then(component => component.DashboardPageComponent)
  },
  {
    path: 'customers',
    data: { breadcrumb: 'Customers', title: 'Customers' },
    loadComponent: () => import('./pages/customers-page.component').then(component => component.CustomersPageComponent)
  },
  {
    path: 'categories',
    data: { breadcrumb: 'Categories', title: 'Categories' },
    loadComponent: () => import('./pages/categories-page.component').then(component => component.CategoriesPageComponent)
  },
  {
    path: 'products',
    data: { breadcrumb: 'Products', title: 'Products' },
    loadComponent: () => import('./pages/products-page.component').then(component => component.ProductsPageComponent)
  },
  {
    path: 'inventory',
    data: { breadcrumb: 'Stock Overview', title: 'Stock Overview' },
    loadComponent: () => import('./pages/inventory-page.component').then(component => component.InventoryPageComponent)
  },
  {
    path: 'orders',
    data: { breadcrumb: 'Orders', title: 'Orders' },
    loadComponent: () => import('./pages/orders-page.component').then(component => component.OrdersPageComponent)
  },
  {
    path: 'reference-data',
    data: { breadcrumb: 'Reference Data', title: 'Colors and Sizes' },
    loadComponent: () => import('./pages/reference-data-page.component').then(component => component.ReferenceDataPageComponent)
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];

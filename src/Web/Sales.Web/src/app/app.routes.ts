import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard'
  },
  {
    path: 'dashboard',
    loadChildren: () => import('./features/dashboard/dashboard.routes').then(routes => routes.dashboardRoutes)
  },
  {
    path: 'customers',
    loadChildren: () => import('./features/customers/customers.routes').then(routes => routes.customersRoutes)
  },
  {
    path: 'categories',
    loadChildren: () => import('./features/categories/categories.routes').then(routes => routes.categoriesRoutes)
  },
  {
    path: 'products',
    loadChildren: () => import('./features/products/products.routes').then(routes => routes.productsRoutes)
  },
  {
    path: 'inventory',
    loadChildren: () => import('./features/inventory/inventory.routes').then(routes => routes.inventoryRoutes)
  },
  {
    path: 'orders',
    loadChildren: () => import('./features/orders/orders.routes').then(routes => routes.ordersRoutes)
  },
  {
    path: 'common',
    loadChildren: () => import('./features/common/common.routes').then(routes => routes.commonRoutes)
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];

import { Routes } from '@angular/router';

export const ordersRoutes: Routes = [
  {
    path: '',
    data: { breadcrumb: 'Orders', title: 'Orders' },
    loadComponent: () => import('./pages/order-list-page/order-list-page.component')
      .then(component => component.OrderListPageComponent)
  }
];

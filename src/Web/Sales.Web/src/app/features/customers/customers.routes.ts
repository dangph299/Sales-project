import { Routes } from '@angular/router';

export const customersRoutes: Routes = [
  {
    path: '',
    data: { breadcrumb: 'Customers', title: 'Customers' },
    loadComponent: () => import('./pages/customer-list-page/customer-list-page.component')
      .then(component => component.CustomerListPageComponent)
  }
];

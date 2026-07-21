import { Routes } from '@angular/router';

export const productsRoutes: Routes = [
  {
    path: '',
    data: { breadcrumb: 'Products', title: 'Products' },
    loadComponent: () => import('./pages/product-list-page/product-list-page.component')
      .then(component => component.ProductListPageComponent)
  }
];

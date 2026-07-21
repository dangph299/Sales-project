import { Routes } from '@angular/router';

export const commonRoutes: Routes = [
  {
    path: '',
    data: { breadcrumb: 'Attribute', title: 'Attribute' },
    loadComponent: () => import('./pages/common-page/common-page.component')
      .then(component => component.CommonPageComponent)
  }
];

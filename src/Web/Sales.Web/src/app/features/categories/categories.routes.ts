import { Routes } from '@angular/router';

export const categoriesRoutes: Routes = [
  {
    path: '',
    data: { breadcrumb: 'Categories', title: 'Categories' },
    loadComponent: () => import('./pages/category-hierarchy-page/category-hierarchy-page.component')
      .then(component => component.CategoryHierarchyPageComponent)
  }
];

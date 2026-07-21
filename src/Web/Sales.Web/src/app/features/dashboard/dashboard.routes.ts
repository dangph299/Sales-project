import { Routes } from '@angular/router';

export const dashboardRoutes: Routes = [
  {
    path: '',
    data: { breadcrumb: 'Dashboard', title: 'Dashboard' },
    loadComponent: () => import('./dashboard-page/dashboard-page.component')
      .then(component => component.DashboardPageComponent)
  }
];

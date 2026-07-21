import { Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';

/**
 * Constructs every routed page so that provider mistakes surface in CI.
 *
 * The Angular compiler cannot see them: injecting a service whose provider
 * lives on an NG-ZORRO module (NzModalService on NzModalModule, for example)
 * builds cleanly and then throws NullInjectorError at runtime.
 */
describe('routed pages construct with their declared providers', () => {
  const pages: [string, () => Promise<Type<unknown>>][] = [
    ['dashboard', () => import('./features/dashboard/dashboard-page/dashboard-page.component')
      .then(m => m.DashboardPageComponent)],
    ['customers', () => import('./features/customers/pages/customer-list-page/customer-list-page.component')
      .then(m => m.CustomerListPageComponent)],
    ['categories', () => import('./features/categories/pages/category-hierarchy-page/category-hierarchy-page.component')
      .then(m => m.CategoryHierarchyPageComponent)],
    ['products', () => import('./features/products/pages/product-list-page/product-list-page.component')
      .then(m => m.ProductListPageComponent)],
    ['orders', () => import('./features/orders/pages/order-list-page/order-list-page.component')
      .then(m => m.OrderListPageComponent)],
    ['inventory', () => import('./features/inventory/pages/inventory-list-page/inventory-list-page.component')
      .then(m => m.InventoryListPageComponent)],
    ['common', () => import('./features/common/pages/common-page/common-page.component')
      .then(m => m.CommonPageComponent)]
  ];

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter(routes)
      ]
    });
  });

  pages.forEach(([name, load]) => {
    it(`creates and destroys the ${name} page`, async () => {
      const component = await load();
      const fixture = TestBed.createComponent(component);

      expect(() => fixture.detectChanges()).not.toThrow();
      expect(() => fixture.destroy()).not.toThrow();
    });
  });

  it('declares a lazy child route for every navigable destination', () => {
    const paths = routes.map(route => route.path);

    ['dashboard', 'customers', 'categories', 'products', 'inventory', 'orders', 'common']
      .forEach(path => expect(paths).toContain(path));
  });
});

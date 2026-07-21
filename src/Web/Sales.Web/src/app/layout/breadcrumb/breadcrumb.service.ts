import { Injectable, inject, signal } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

export interface Breadcrumb {
  label: string;
  url: string;
}

/** Derives breadcrumb trail and page title from `data` on the active route chain. */
@Injectable({ providedIn: 'root' })
export class BreadcrumbService {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly breadcrumbs = signal<Breadcrumb[]>([]);
  readonly pageTitle = signal('Dashboard');

  constructor() {
    this.router.events
      .pipe(filter(routerEvent => routerEvent instanceof NavigationEnd))
      .subscribe(() => this.refresh());

    this.refresh();
  }

  private refresh(): void {
    const breadcrumbs: Breadcrumb[] = [];
    let currentRoute: ActivatedRoute | null = this.route.root;
    let url = '';

    while (currentRoute) {
      const childRoute: ActivatedRoute | null = currentRoute.firstChild;
      if (!childRoute) {
        break;
      }

      const routePath = childRoute.snapshot.url.map(segment => segment.path).join('/');
      if (routePath) {
        url += `/${routePath}`;
      }

      const label = childRoute.snapshot.data['breadcrumb'];
      if (typeof label === 'string') {
        breadcrumbs.push({ label, url });
      }

      const title = childRoute.snapshot.data['title'];
      if (typeof title === 'string') {
        this.pageTitle.set(title);
      }

      currentRoute = childRoute;
    }

    this.breadcrumbs.set(breadcrumbs);
  }
}

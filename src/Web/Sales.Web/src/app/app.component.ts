import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { ApiClientError, ApiResponseReader } from './api-client-result';
import { ApiService } from './api.service';

interface Breadcrumb {
  label: string;
  url: string;
}

interface NavigationLink {
  label: string;
  route: string;
}

interface NavigationGroup {
  label: string;
  links: NavigationLink[];
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  readonly userName = signal('admin');
  readonly password = signal('Admin123!');
  readonly sidebarOpen = signal(false);
  readonly authenticating = signal(false);
  readonly healthText = signal('Chua kiem tra');
  readonly errorMessage = signal('');
  readonly breadcrumbs = signal<Breadcrumb[]>([]);
  readonly pageTitle = signal('Dashboard');

  readonly navigationGroups: NavigationGroup[] = [
    {
      label: 'Sales',
      links: [
        { label: 'Orders', route: '/orders' },
        { label: 'Customers', route: '/customers' }
      ]
    },
    {
      label: 'Catalog',
      links: [
        { label: 'Products', route: '/products' },
        { label: 'Categories', route: '/categories' },
        { label: 'Colors & Sizes', route: '/reference-data' }
      ]
    },
    {
      label: 'Inventory',
      links: [
        { label: 'Stock Overview', route: '/inventory' }
      ]
    }
  ];

  readonly isAuthenticated = computed(() => this.api.accessToken().trim().length > 0);

  constructor(readonly api: ApiService, private readonly router: Router, private readonly route: ActivatedRoute) {
    this.router.events
      .pipe(filter(routerEvent => routerEvent instanceof NavigationEnd))
      .subscribe(() => this.updateRouteMetadata());
    this.updateRouteMetadata();
  }

  async login(): Promise<void> {
    this.authenticating.set(true);
    this.errorMessage.set('');
    try {
      await this.api.login(this.userName(), this.password());
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    } finally {
      this.authenticating.set(false);
    }
  }

  logout(): void {
    this.api.logout();
  }

  async checkHealth(): Promise<void> {
    this.healthText.set('Dang kiem tra...');
    try {
      await this.api.health();
      this.healthText.set('Sales API va Inventory API san sang');
    } catch (error) {
      this.healthText.set(this.describeError(error));
    }
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  toggleSidebar(): void {
    this.sidebarOpen.update(isOpen => !isOpen);
  }

  private updateRouteMetadata(): void {
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

  private describeError(error: unknown): string {
    if (error instanceof ApiClientError) {
      return ApiResponseReader.formatFailure(error.result);
    }

    if (error instanceof Error) {
      return error.message;
    }

    return 'Request failed.';
  }
}

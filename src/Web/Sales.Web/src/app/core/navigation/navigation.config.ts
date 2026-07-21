import { NavigationItem } from './navigation-item.model';

export const navigationItems: NavigationItem[] = [
  {
    label: 'Dashboard',
    icon: 'dashboard',
    route: '/dashboard'
  },
  {
    label: 'Sales',
    icon: 'shopping-cart',
    links: [
      { label: 'Orders', route: '/orders' },
      { label: 'Customers', route: '/customers' }
    ]
  },
  {
    label: 'Catalog',
    icon: 'appstore',
    links: [
      { label: 'Products', route: '/products' },
      { label: 'Categories', route: '/categories' },
      { label: 'Attribute', route: '/common' }
    ]
  },
  {
    label: 'Inventory',
    icon: 'database',
    route: '/inventory'
  }
];

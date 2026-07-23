import { ApiEndpointConfigurationService } from './api-endpoint-configuration.service';

describe('ApiEndpointConfigurationService', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('defaults the dashboard base to the dashboard api proxy path', () => {
    const service = new ApiEndpointConfigurationService();

    expect(service.dashboardBase()).toBe('/dashboard-api');
  });

  it('persists the dashboard base with the other backend base urls', () => {
    const service = new ApiEndpointConfigurationService();

    service.setBaseUrls('/sales', '/inventory', '/dashboard');

    expect(service.salesBase()).toBe('/sales');
    expect(service.inventoryBase()).toBe('/inventory');
    expect(service.dashboardBase()).toBe('/dashboard');
    expect(localStorage.getItem('dashboardBase')).toBe('/dashboard');
  });

  it('falls back to defaults for blank base urls', () => {
    const service = new ApiEndpointConfigurationService();

    service.setBaseUrls(' ', ' ', ' ');

    expect(service.salesBase()).toBe('/sales-api');
    expect(service.inventoryBase()).toBe('/inventory-api');
    expect(service.dashboardBase()).toBe('/dashboard-api');
  });
});

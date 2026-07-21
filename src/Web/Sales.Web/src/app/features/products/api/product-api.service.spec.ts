import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SessionService } from '../../../core/auth/session.service';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { ProductApiService } from './product-api.service';
import { ProductResponse } from './responses/product.response';

describe('ProductApiService description payloads', () => {
  let service: ProductApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: SessionService, useValue: { accessToken: signal('token') } },
        { provide: ApiEndpointConfigurationService, useValue: { salesBase: () => '/sales-api' } }
      ]
    });

    service = TestBed.inject(ProductApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('sends description in the create HTTP request payload', async () => {
    const result = service.create({
      name: 'Product two',
      description: 'Create description',
      categoryId: 'category-1',
      variants: []
    });

    const request = http.expectOne('/sales-api/api/products/');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      name: 'Product two',
      description: 'Create description',
      categoryId: 'category-1',
      variants: []
    });
    request.flush(success(product('product-2', 'Product two', 'Create description')));

    expect((await result).description).toBe('Create description');
  });

  it('sends description in the update HTTP request payload', async () => {
    const result = service.update('product-1', {
      name: 'Product one',
      description: 'Update description',
      categoryId: 'category-1',
      status: 'Draft'
    });

    const request = http.expectOne('/sales-api/api/products/product-1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      name: 'Product one',
      description: 'Update description',
      categoryId: 'category-1',
      status: 'Draft'
    });
    request.flush(success(product('product-1', 'Product one', 'Update description')));

    expect((await result).description).toBe('Update description');
  });
});

function success<T>(data: T): { success: true; data: T } {
  return { success: true, data };
}

function product(id: string, name: string, description: string): ProductResponse {
  return {
    id,
    sku: 'PRD-1',
    productCode: 'PRD-1',
    name,
    description,
    categoryId: 'category-1',
    category: { id: 'category-1', categoryCode: 'CAT-1', name: 'Category' },
    status: 'Draft',
    minPrice: null,
    maxPrice: null,
    variants: [],
    isActive: true,
    version: 1,
    updatedAt: '2026-07-21T00:00:00Z',
    isDelete: false
  };
}

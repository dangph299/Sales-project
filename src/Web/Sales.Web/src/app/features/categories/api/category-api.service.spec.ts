import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SessionService } from '../../../core/auth/session.service';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { CategoryApiService } from './category-api.service';
import { CategoryResponse } from './responses/category.response';

describe('CategoryApiService description payloads', () => {
  let service: CategoryApiService;
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

    service = TestBed.inject(CategoryApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('sends description in the create HTTP request payload', async () => {
    const result = service.create({
      name: 'Category two',
      description: 'Create description',
      parentCategoryId: null,
      sortOrder: 2
    });

    const request = http.expectOne('/sales-api/api/categories/');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      name: 'Category two',
      description: 'Create description',
      parentCategoryId: null,
      sortOrder: 2
    });
    request.flush(success(category('category-2', 'Category two', 'Create description')));

    expect((await result).description).toBe('Create description');
  });

  it('sends description in the update HTTP request payload', async () => {
    const result = service.update('category-1', {
      name: 'Category one',
      description: 'Update description',
      parentCategoryId: null,
      sortOrder: 1,
      status: 'Draft'
    });

    const request = http.expectOne('/sales-api/api/categories/category-1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      name: 'Category one',
      description: 'Update description',
      parentCategoryId: null,
      sortOrder: 1,
      status: 'Draft'
    });
    request.flush(success(category('category-1', 'Category one', 'Update description')));

    expect((await result).description).toBe('Update description');
  });
});

function success<T>(data: T): { success: true; data: T } {
  return { success: true, data };
}

function category(id: string, name: string, description: string): CategoryResponse {
  return {
    id,
    categoryCode: 'CAT-1',
    name,
    description,
    parentCategoryId: null,
    sortOrder: 1,
    status: 'Draft'
  };
}

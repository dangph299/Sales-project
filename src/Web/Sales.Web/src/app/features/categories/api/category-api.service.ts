import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { CreateCategoryRequest } from './requests/create-category.request';
import { UpdateCategoryRequest } from './requests/update-category.request';
import { CategoryResponse } from './responses/category.response';

@Injectable({ providedIn: 'root' })
export class CategoryApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);

  private get baseUrl(): string {
    return this.endpoints.salesBase();
  }

  list(): Promise<CategoryResponse[]> {
    return this.client.get<CategoryResponse[]>(this.baseUrl, '/api/categories');
  }

  create(request: CreateCategoryRequest): Promise<CategoryResponse> {
    return this.client.post<CategoryResponse>(this.baseUrl, '/api/categories/', request);
  }

  update(categoryId: string, request: UpdateCategoryRequest): Promise<CategoryResponse> {
    return this.client.put<CategoryResponse>(this.baseUrl, `/api/categories/${categoryId}`, request);
  }

  delete(categoryId: string): Promise<void> {
    return this.client.delete(this.baseUrl, `/api/categories/${categoryId}`);
  }
}

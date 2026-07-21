import { Injectable, inject } from '@angular/core';
import { ApiClientService } from '../../../core/api/api-client.service';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { CategoryLookupResponse } from '../contracts/category-lookup.response';
import { ColorResponse } from '../contracts/color.response';
import { SizeLookupResponse } from '../contracts/size-lookup.response';

/**
 * Read-only access to seeded common lookup data. Every item carries both the stable business `code`
 * and the persistence `id`, so no seeded identifier has to be hardcoded in the frontend.
 */
@Injectable({ providedIn: 'root' })
export class CommonApiService {
  private readonly client = inject(ApiClientService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);

  listColors(): Promise<ColorResponse[]> {
    return this.client.get<ColorResponse[]>(this.endpoints.salesBase(), '/api/common/colors');
  }

  listSizes(): Promise<SizeLookupResponse[]> {
    return this.client.get<SizeLookupResponse[]>(this.endpoints.salesBase(), '/api/common/sizes');
  }

  listCategories(): Promise<CategoryLookupResponse[]> {
    return this.client.get<CategoryLookupResponse[]>(this.endpoints.salesBase(), '/api/categories');
  }
}

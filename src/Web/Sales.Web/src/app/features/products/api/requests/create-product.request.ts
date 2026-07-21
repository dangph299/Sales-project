import { SaveProductVariantRequest } from './save-product-variant.request';

export interface CreateProductRequest {
  name: string;
  description?: string | null;
  categoryId: string;
  variants?: SaveProductVariantRequest[];
}

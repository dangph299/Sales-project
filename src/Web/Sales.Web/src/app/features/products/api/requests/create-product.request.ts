import { SaveProductVariantRequest } from './save-product-variant.request';

export interface CreateProductRequest {
  productCode: string;
  name: string;
  description?: string | null;
  categoryId: string;
  variants?: SaveProductVariantRequest[];
}

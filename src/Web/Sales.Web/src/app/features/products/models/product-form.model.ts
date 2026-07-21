import { ProductStatus } from '../constants/product-status';

export interface ProductFormModel {
  productCode: string;
  name: string;
  description: string;
  categoryId: string;
  status: ProductStatus;
}

/**
 * Creates a blank product form. The default category is supplied by the caller from loaded
 * common lookup data, so the form carries no seeded identifiers of its own.
 */
export function emptyProductForm(categoryId = ''): ProductFormModel {
  return {
    productCode: `PRD${Date.now().toString().slice(-6)}`,
    name: '',
    description: '',
    categoryId,
    status: 'Draft'
  };
}

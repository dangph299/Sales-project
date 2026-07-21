import { ProductStatus } from '../constants/product-status';

export interface ProductFormModel {
  name: string;
  description: string;
  categoryId: string;
  status: ProductStatus;
}

/**
 * Creates a blank product form. The default category is supplied by the caller from loaded
 * common lookup data, so the form carries no seeded identifiers of its own. The product code is
 * allocated by the backend on create and is never chosen here.
 */
export function emptyProductForm(categoryId = ''): ProductFormModel {
  return {
    name: '',
    description: '',
    categoryId,
    status: 'Draft'
  };
}

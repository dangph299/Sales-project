import { ProductVariantStatus } from '../constants/product-variant-status';

export interface ProductVariantFormModel {
  colorId: string;
  sizeId: string;
  price: number;
  status: ProductVariantStatus;
}

/**
 * Creates a blank variant form. Color and size defaults are supplied by the caller from loaded
 * common lookup data, so the form carries no seeded identifiers of its own.
 */
export function emptyProductVariantForm(colorId = '', sizeId = ''): ProductVariantFormModel {
  return {
    colorId,
    sizeId,
    price: 150000,
    status: 'Draft'
  };
}

import { ProductVariantStatus } from '../../constants/product-variant-status';

/** Create and update share the same variant payload shape. */
export interface SaveProductVariantRequest {
  colorId: string;
  sizeId: string;
  price: number;
  status: ProductVariantStatus;
}

import { ColorResponse } from './color.response';
import { SizeResponse } from './size.response';

/**
 * The narrow product projection features other than Products may consume.
 *
 * Deliberately smaller than the Products feature's own ProductResponse: this is
 * what Orders, Inventory and Dashboard actually read when picking a variant.
 */
export interface ProductLookupResponse {
  id: string;
  sku: string;
  productCode?: string | null;
  name: string;
  status?: string | null;
  minPrice?: number | null;
  maxPrice?: number | null;
  variants?: ProductVariantLookupResponse[] | null;
}

export interface ProductVariantLookupResponse {
  id: string;
  sku: string;
  color?: ColorResponse | null;
  size?: SizeResponse | null;
  price: number;
  status: string;
}

import { ColorResponse } from '../../../common/contracts/color.response';
import { SizeResponse } from '../../../common/contracts/size.response';
import { ProductStatus } from '../../constants/product-status';
import { ProductVariantStatus } from '../../constants/product-variant-status';

export interface ProductCategoryResponse {
  id: string;
  categoryCode: string;
  name: string;
}

export interface ProductVariantResponse {
  id: string;
  sku: string;
  color?: ColorResponse | null;
  size?: SizeResponse | null;
  price: number;
  status: ProductVariantStatus | string;
  createdAt?: string | null;
  updatedAt?: string | null;
}

export interface ProductResponse {
  id: string;
  sku: string;
  productCode?: string | null;
  name: string;
  description?: string | null;
  categoryId?: string | null;
  category?: ProductCategoryResponse | null;
  status?: ProductStatus | string | null;
  minPrice?: number | null;
  maxPrice?: number | null;
  variants?: ProductVariantResponse[] | null;
  isActive: boolean;
  version: number;
  createdAt?: string | null;
  updatedAt: string;
  isDelete: boolean;
  deleteByUser?: string | null;
  deletedAt?: string | null;
}

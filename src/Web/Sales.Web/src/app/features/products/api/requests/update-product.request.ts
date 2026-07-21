import { ProductStatus } from '../../constants/product-status';

export interface UpdateProductRequest {
  name: string;
  description?: string | null;
  categoryId: string;
  status: ProductStatus;
}

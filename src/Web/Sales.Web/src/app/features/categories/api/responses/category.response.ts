import { CategoryStatus } from '../../constants/category-status';

export interface CategoryResponse {
  id: string;
  categoryCode: string;
  name: string;
  description?: string | null;
  parentCategoryId?: string | null;
  parentCategoryName?: string | null;
  sortOrder: number;
  status: CategoryStatus | string;
  productCount?: number;
  createdAt?: string | null;
  updatedAt?: string | null;
}

import { CategoryStatus } from '../../constants/category-status';

export interface UpdateCategoryRequest {
  name: string;
  description?: string | null;
  parentCategoryId?: string | null;
  sortOrder: number;
  status: CategoryStatus;
}

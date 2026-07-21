import { CategoryResponse } from '../api/responses/category.response';
import { CategoryStatus } from '../constants/category-status';

export interface CategoryTreeNode {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  parentCategoryId?: string | null;
  parentCategoryName?: string | null;
  status: CategoryStatus | string;
  sortOrder: number;
  productCount?: number;
  updatedAt?: string | null;
  level: number;
  hasChildren: boolean;
  isExpanded: boolean;
  children: CategoryTreeNode[];
  path: string;
  source: CategoryResponse;
  isInvalid: boolean;
  invalidReason?: string;
  isContextOnly?: boolean;
}

export interface CategoryTreeBuildResult {
  roots: CategoryTreeNode[];
  diagnostics: string[];
}


import { TableColumn } from '@shared/models/table.model';
import { CategoryTreeNode } from '../../models/category-tree-node.model';

export const categoryHierarchyColumns: TableColumn<CategoryTreeNode>[] = [
  { key: 'category', header: 'Category', type: 'custom', width: '250px', cssClass: 'category-column' },
  { key: 'code', header: 'Code', type: 'custom', width: '120px', cssClass: 'code-column' },
  { key: 'parentCategoryName', header: 'Parent Category', type: 'custom', width: '150px', cssClass: 'parent-column' },
  { key: 'status', header: 'Status', type: 'custom', width: '120px', cssClass: 'status-column' },
  { key: 'sortOrder', header: 'Sort', align: 'right', width: '90px' },
  { key: 'productCount', header: 'Products', align: 'right', width: '100px', valueAccessor: row => row.productCount ?? 0 },
  { key: 'updatedAt', header: 'Updated', type: 'dateTime', width: '150px', cssClass: 'date-column' },
  { key: 'actions', header: 'Actions', type: 'custom', align: 'center', width: '150px' }
];

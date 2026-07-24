import { TableColumn } from '@shared/models/table.model';
import { ProductResponse, ProductVariantResponse } from '../../api/responses/product.response';

export const productListColumns: TableColumn<ProductResponse>[] = [
  { key: 'productCode', header: 'Code', valueFormatter: row => row.productCode || row.sku },
  { key: 'name', header: 'Name' },
  { key: 'category', header: 'Category', valueAccessor: row => row.category?.name || '-' },
  { key: 'variants', header: 'Variants (Published/Total)', type: 'custom' },
  { key: 'priceRange', header: 'Price range', type: 'custom' },
  { key: 'updatedAt', header: 'Updated', type: 'dateTime' },
  { key: 'actions', header: 'Actions', type: 'custom', align: 'center' }
];

export const productVariantListColumns: TableColumn<ProductVariantResponse>[] = [
  { key: 'sku', header: 'SKU', cssClass: 'mono' },
  { key: 'color', header: 'Color', type: 'custom' },
  { key: 'size', header: 'Size', valueAccessor: row => row.size?.code || '-' },
  { key: 'price', header: 'Price', type: 'currency', align: 'right' },
  { key: 'status', header: 'Status', type: 'custom' },
  { key: 'actions', header: 'Actions', type: 'custom', align: 'center' }
];

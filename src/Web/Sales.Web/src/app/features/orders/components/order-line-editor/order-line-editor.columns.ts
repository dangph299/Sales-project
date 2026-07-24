import { TableColumn } from '@shared/models/table.model';
import { CartLine } from '../../models/cart-line.model';

export const orderLineEditorColumns: TableColumn<CartLine>[] = [
  { key: 'product', header: 'Product', valueAccessor: row => row.product.name },
  { key: 'sku', header: 'SKU', valueAccessor: row => row.variant.sku },
  { key: 'color', header: 'Color', valueAccessor: row => row.variant.color?.name || '-' },
  { key: 'size', header: 'Size', valueAccessor: row => row.variant.size?.code || '-' },
  { key: 'unitPrice', header: 'Unit price', type: 'currency', align: 'right', valueAccessor: row => row.variant.price },
  { key: 'quantity', header: 'Quantity', type: 'custom', align: 'right' },
  { key: 'discountPercent', header: 'Discount %', type: 'custom', align: 'right' },
  { key: 'lineTotal', header: 'Line total', type: 'custom', align: 'right' },
  { key: 'actions', header: '', type: 'custom', align: 'center' }
];

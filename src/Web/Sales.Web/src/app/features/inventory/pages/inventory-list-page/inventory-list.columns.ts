import { TableColumn } from '@shared/models/table.model';
import { StockRow } from '../../models/stock-row.model';

export const inventoryListColumns: TableColumn<StockRow>[] = [
  { key: 'sku', header: 'SKU', sortable: true, valueAccessor: row => row.variant.sku },
  { key: 'product', header: 'Product', sortable: true, valueAccessor: row => row.product.name },
  { key: 'color', header: 'Color', sortable: true, valueAccessor: row => row.variant.color?.code || '-' },
  { key: 'size', header: 'Size', sortable: true, valueAccessor: row => row.variant.size?.code || '-' },
  { key: 'status', header: 'Variant Status', sortable: true, type: 'custom' },
  { key: 'available', header: 'Available', align: 'right', valueAccessor: row => row.inventory?.available ?? 0 },
  { key: 'reserved', header: 'Reserved', align: 'right', valueAccessor: row => row.inventory?.reserved ?? 0 },
  { key: 'total', header: 'Total', align: 'right', type: 'custom' },
  { key: 'state', header: 'State', type: 'custom' },
  { key: 'actions', header: 'Actions', type: 'custom', align: 'center' }
];

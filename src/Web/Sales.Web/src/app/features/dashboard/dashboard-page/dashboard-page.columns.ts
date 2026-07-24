import { TableColumn } from '@shared/models/table.model';
import { RecentOrderRow } from '../models/dashboard-row.model';

export const recentOrderColumns: TableColumn<RecentOrderRow>[] = [
  { key: 'orderCode', header: 'Order Number', type: 'custom' },
  { key: 'customerName', header: 'Customer' },
  { key: 'status', header: 'Status', type: 'custom' },
  { key: 'totalQuantity', header: 'Total Qty', align: 'right' },
  { key: 'total', header: 'Total Amount', type: 'currency', align: 'right' },
  { key: 'createdAt', header: 'Created', type: 'dateTime' }
];

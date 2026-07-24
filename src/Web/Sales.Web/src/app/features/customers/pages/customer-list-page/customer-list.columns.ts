import { TableColumn } from '@shared/models/table.model';
import { CustomerResponse } from '../../api/responses/customer.response';

export const customerListColumns: TableColumn<CustomerResponse>[] = [
  { key: 'customerCode', header: 'Code', sortable: true },
  { key: 'name', header: 'Name', sortable: true },
  { key: 'phone', header: 'Phone', sortable: true },
  { key: 'status', header: 'Status', sortable: true, type: 'custom' },
  { key: 'address', header: 'Address', sortable: true, type: 'custom' },
  { key: 'updatedAt', header: 'Updated', sortable: true, type: 'dateTime' },
  { key: 'actions', header: 'Actions', type: 'custom', align: 'center', width: '120px' }
];

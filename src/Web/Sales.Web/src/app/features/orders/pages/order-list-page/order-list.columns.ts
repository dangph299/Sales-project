import { TableColumn } from '@shared/models/table.model';
import { ProductLookupResponse, ProductVariantLookupResponse } from '../../../common/contracts/product-lookup.response';
import { OrderResponse } from '../../api/responses/order.response';

export interface OrderProductOption {
  product: ProductLookupResponse;
  variant: ProductVariantLookupResponse;
}

export const orderListColumns: TableColumn<OrderResponse>[] = [
  { key: 'orderNumber', header: 'Order Number', sortable: true, type: 'custom', width: '130px' },
  { key: 'customer', header: 'Customer', sortable: true, valueAccessor: row => row.customerName, width: '180px', cssClass: 'truncate-cell' },
  { key: 'phone', header: 'Phone', sortable: true, valueAccessor: row => row.customerPhone, width: '130px', cssClass: 'nowrap-cell' },
  { key: 'createdAt', header: 'Order Date', sortable: true, valueAccessor: row => row.createdAt, type: 'dateTime', width: '145px', cssClass: 'nowrap-cell' },
  { key: 'total', header: 'Total Amount', sortable: true, valueAccessor: row => row.total, type: 'currency', align: 'right', width: '125px' },
  { key: 'status', header: 'Status', sortable: true, type: 'custom', width: '140px', cssClass: 'nowrap-cell' },
  { key: 'updatedAt', header: 'Last Updated', sortable: true, valueAccessor: row => row.updatedAt, type: 'dateTime', width: '145px', cssClass: 'nowrap-cell' },
  { key: 'actions', header: 'Actions', type: 'custom', align: 'center', width: '180px' }
];

export const orderProductOptionColumns: TableColumn<OrderProductOption>[] = [
  { key: 'sku', header: 'SKU', valueAccessor: row => row.variant.sku },
  { key: 'product', header: 'Product', valueAccessor: row => row.product.name },
  { key: 'status', header: 'Status', type: 'custom' },
  { key: 'price', header: 'Price', type: 'currency', align: 'right', valueAccessor: row => row.variant.price },
  { key: 'actions', header: '', type: 'custom', align: 'center' }
];

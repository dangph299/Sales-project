import { OrderStatus } from '../../constants/order-status';

export interface OrderLineResponse {
  productVariantId: string;
  quantity: number;
  discountPercent: number;
  productId?: string | null;
  productCode?: string | null;
  sku: string;
  productName: string;
  colorCode?: string | null;
  colorName?: string | null;
  sizeCode?: string | null;
  isSellThroughDiscontinued?: boolean;
  unitPrice: number;
  lineTotal: number;
}

/**
 * An order as the API returns it.
 *
 * Every customer field is the order's own snapshot. The normalized and reversed
 * phone columns are intentionally absent: they exist only so the database can
 * index phone searches.
 */
export interface OrderResponse {
  id: string;
  orderCode: string;
  customerId: string;
  customerName: string;
  customerPhone: string;
  customerEmail?: string | null;
  customerAddress?: string | null;
  createdAt: string;
  status: OrderStatus | string;
  totalQuantity: number;
  total: number;
  version: number;
  updatedAt: string;
  rejectionReason?: string | null;
  lines: OrderLineResponse[];
}

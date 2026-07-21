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
  unitPrice: number;
  lineTotal: number;
}

export interface OrderResponse {
  id: string;
  customerId: string;
  customerName: string;
  customerPhone: string;
  createdAt: string;
  status: OrderStatus | string;
  totalQuantity: number;
  total: number;
  version: number;
  updatedAt: string;
  rejectionReason?: string | null;
  lines: OrderLineResponse[];
}

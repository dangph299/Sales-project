export interface RecentOrderResponse {
  id: string;
  orderCode: string;
  customerName: string;
  status: string;
  totalQuantity: number;
  total: number;
  createdAt: string;
}

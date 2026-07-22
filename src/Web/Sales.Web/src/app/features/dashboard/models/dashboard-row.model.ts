import { StatusDisplay } from '../../../shared/models/status-display.model';

/** Dashboard-owned projection of a recent order. */
export interface RecentOrderRow {
  id: string;
  orderCode: string;
  customerName: string;
  status: StatusDisplay;
  totalQuantity: number;
  total: number;
  createdAt: string;
}

/** Dashboard-owned projection of a recent product. */
export interface RecentProductRow {
  id: string;
  productCode: string;
  productName: string;
  status: StatusDisplay;
  minPrice?: number | null;
  maxPrice?: number | null;
}

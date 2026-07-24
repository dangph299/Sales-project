import { TableSortDirection } from '../../../../shared/models/table.model';
import { StatusDisplay } from '../../../../shared/models/status-display.model';
import { OrderResponse } from '../../api/responses/order.response';

export type OrderSortKey = 'orderNumber' | 'customer' | 'phone' | 'createdAt' | 'total' | 'status' | 'updatedAt';
export type OrderSortDirection = 'asc' | 'desc';

export function toTableSortDirection(direction: OrderSortDirection): TableSortDirection {
  return direction === 'asc' ? 'ascend' : 'descend';
}

export function fromTableSortDirection(direction: TableSortDirection): OrderSortDirection {
  return direction === 'ascend' ? 'asc' : 'desc';
}

export function orderSortValue(
  order: OrderResponse,
  key: OrderSortKey,
  statusDisplay: (status: string) => StatusDisplay
): string | number {
  switch (key) {
    case 'orderNumber':
      return order.orderCode;
    case 'customer':
      return order.customerName;
    case 'phone':
      return order.customerPhone;
    case 'createdAt':
      return Date.parse(order.createdAt);
    case 'total':
      return order.total;
    case 'status':
      return statusDisplay(order.status).label;
    case 'updatedAt':
      return Date.parse(order.updatedAt);
  }
}

export function compareOrders(
  left: OrderResponse,
  right: OrderResponse,
  key: OrderSortKey,
  direction: OrderSortDirection,
  statusDisplay: (status: string) => StatusDisplay
): number {
  const sign = direction === 'asc' ? 1 : -1;
  const leftValue = orderSortValue(left, key, statusDisplay);
  const rightValue = orderSortValue(right, key, statusDisplay);

  if (typeof leftValue === 'number' && typeof rightValue === 'number') {
    return (leftValue - rightValue) * sign;
  }

  return String(leftValue).localeCompare(String(rightValue)) * sign;
}

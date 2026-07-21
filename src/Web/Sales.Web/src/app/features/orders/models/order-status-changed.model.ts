import { OrderStatus } from '../constants/order-status';

/** Payload of the OrderStatusChanged hub event. */
export interface OrderStatusChangedNotification {
  orderId: string;
  previousStatus: OrderStatus | string;
  currentStatus: OrderStatus | string;
  changedAt: string;
  version: number;
}

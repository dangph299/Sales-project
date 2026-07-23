import { DashboardSnapshotResponse } from '../api/responses/dashboard-snapshot.response';
import { OrderChartPointResponse } from '../api/responses/order-chart-point.response';
import { RecentOrderResponse } from '../api/responses/recent-order.response';
import { dashboardOrderStatusDisplay } from '../constants/dashboard-status';
import { DashboardSnapshot } from '../models/dashboard-snapshot.model';
import { OrderChartRow, RecentOrderRow } from '../models/dashboard-row.model';

export function toDashboardSnapshot(response: DashboardSnapshotResponse): DashboardSnapshot {
  return {
    metrics: response.metrics,
    inventory: response.inventory,
    recentOrders: toRecentOrderRows(response.recentOrders),
    chartOrders: toOrderChartRows(response.orderChart),
    refreshedAt: response.refreshedAt
  };
}

export function toRecentOrderRows(orders: RecentOrderResponse[]): RecentOrderRow[] {
  return orders.map(order => ({
    id: order.id,
    orderCode: order.orderCode,
    customerName: order.customerName,
    status: dashboardOrderStatusDisplay(order.status),
    totalQuantity: order.totalQuantity,
    total: order.total,
    createdAt: order.createdAt
  }));
}

export function toOrderChartRows(points: OrderChartPointResponse[]): OrderChartRow[] {
  return points.map(point => ({
    createdAt: point.createdAt,
    total: point.total,
    status: dashboardOrderStatusDisplay(point.status)
  }));
}

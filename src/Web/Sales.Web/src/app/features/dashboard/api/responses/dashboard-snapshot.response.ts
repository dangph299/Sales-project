import { DashboardMetricsResponse } from './dashboard-metrics.response';
import { InventorySummaryResponse } from './inventory-summary.response';
import { OrderChartPointResponse } from './order-chart-point.response';
import { RecentOrderResponse } from './recent-order.response';

export interface DashboardSnapshotResponse {
  metrics: DashboardMetricsResponse;
  inventory: InventorySummaryResponse;
  recentOrders: RecentOrderResponse[];
  orderChart: OrderChartPointResponse[];
  refreshedAt: string;
}

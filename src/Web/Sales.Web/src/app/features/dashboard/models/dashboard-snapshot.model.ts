import { DashboardMetrics } from './dashboard-metrics.model';
import { InventorySummary } from './inventory-summary.model';
import { OrderChartRow, RecentOrderRow } from './dashboard-row.model';

export interface DashboardSnapshot {
  metrics: DashboardMetrics;
  inventory: InventorySummary;
  recentOrders: RecentOrderRow[];
  chartOrders: OrderChartRow[];
  refreshedAt: string;
}

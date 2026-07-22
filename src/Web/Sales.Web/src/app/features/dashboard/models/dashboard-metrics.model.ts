export interface DashboardMetrics {
  orderTotal: number;
  pendingOrderCount: number;
  revenueToday: number;
  customerTotal: number;
  productTotal: number;
  publishedProductCount: number;
}

export const emptyDashboardMetrics: DashboardMetrics = {
  orderTotal: 0,
  pendingOrderCount: 0,
  revenueToday: 0,
  customerTotal: 0,
  productTotal: 0,
  publishedProductCount: 0
};

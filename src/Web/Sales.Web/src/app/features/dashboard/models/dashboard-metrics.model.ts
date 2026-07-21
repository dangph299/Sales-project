export interface DashboardMetrics {
  orderTotal: number;
  pendingOrderCount: number;
  revenue: number;
  customerTotal: number;
  publishedProductCount: number;
  publishedVariantCount: number;
}

export const emptyDashboardMetrics: DashboardMetrics = {
  orderTotal: 0,
  pendingOrderCount: 0,
  revenue: 0,
  customerTotal: 0,
  publishedProductCount: 0,
  publishedVariantCount: 0
};

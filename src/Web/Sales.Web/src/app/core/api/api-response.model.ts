/** Envelope returned by every Sales/Inventory API endpoint. */
export interface ApiResponse<T> {
  success: boolean;
  succeeded?: boolean;
  message?: string | null;
  correlationId?: string | null;
  data?: T | null;
}

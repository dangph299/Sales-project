/** A successful response plus the transport metadata callers need (ETag for concurrency). */
export interface ApiResult<T> {
  body: T;
  etag?: string | null;
  status: number;
  message?: string | null;
  correlationId?: string | null;
}

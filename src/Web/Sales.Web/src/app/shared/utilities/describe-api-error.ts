import { ApiClientError, ApiResponseReader } from '../../core/api/api-client-result';

/** Turns any thrown value into a message safe to show the user. */
export function describeApiError(error: unknown): string {
  if (error instanceof ApiClientError) {
    return ApiResponseReader.formatFailure(error.result);
  }

  return error instanceof Error ? error.message : 'Request failed.';
}

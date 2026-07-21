import { SizeResponse } from './size.response';

/**
 * Size as published by the common lookup endpoint. Adds the seeded sort order that the
 * variant projection embedded in product responses does not carry.
 */
export interface SizeLookupResponse extends SizeResponse {
  sortOrder: number;
}

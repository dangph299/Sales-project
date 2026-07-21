import { StatusDisplay, toStatusDisplay } from '../../../shared/models/status-display.model';

/** String union, not a TS enum: values are backend codes and must match exactly. */
export type CategoryStatus = 'Draft' | 'Published' | 'Archived';

export const categoryStatusDisplays: Readonly<Record<CategoryStatus, StatusDisplay>> = {
  Draft: { label: 'Draft', tone: 'neutral' },
  Published: { label: 'Published', tone: 'success' },
  Archived: { label: 'Archived', tone: 'neutral' }
};

export function categoryStatusDisplay(status: string | null | undefined): StatusDisplay {
  return toStatusDisplay(status, categoryStatusDisplays);
}

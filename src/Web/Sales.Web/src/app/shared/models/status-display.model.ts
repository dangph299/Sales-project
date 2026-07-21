export type StatusTone = 'success' | 'warning' | 'danger' | 'neutral' | 'info';

/**
 * A status rendered for display. Features map their own status values to this
 * shape; nothing here knows what any particular feature's statuses are.
 */
export interface StatusDisplay {
  label: string;
  tone: StatusTone;
}

export const unknownStatusDisplay: StatusDisplay = { label: 'Unknown', tone: 'neutral' };

/** Falls back to showing the raw code when a feature has no mapping for it. */
export function toStatusDisplay(
  status: string | null | undefined,
  mapping: Readonly<Record<string, StatusDisplay>>): StatusDisplay {
  if (!status) {
    return unknownStatusDisplay;
  }

  return mapping[status] ?? { label: status, tone: 'neutral' };
}

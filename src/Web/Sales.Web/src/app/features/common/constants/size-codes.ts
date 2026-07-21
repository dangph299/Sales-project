/**
 * Size codes the frontend has real business behavior tied to.
 *
 * Only codes with behavior belong here. Every other size reaches the UI purely as dropdown data
 * loaded from the backend, so it gets no constant.
 */
export const SizeCodes = {
  /** Pre-selected when a new product variant form is opened. */
  Medium: 'M'
} as const;

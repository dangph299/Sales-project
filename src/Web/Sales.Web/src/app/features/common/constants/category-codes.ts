/**
 * Category codes the frontend has real business behavior tied to.
 *
 * Only codes with behavior belong here. Every other category reaches the UI purely as dropdown or
 * tree data loaded from the backend, so it gets no constant.
 */
export const CategoryCodes = {
  /** Default assignment for a new product, and expanded by default in the category tree. */
  Uncategorized: 'CAT001'
} as const;

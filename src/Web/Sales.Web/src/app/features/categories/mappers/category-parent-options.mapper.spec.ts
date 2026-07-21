import { CategoryResponse } from '../api/responses/category.response';
import { buildCategoryTree } from './category-tree.mapper';
import { ineligibleParentIds, toParentSelectorNodes } from './category-parent-options.mapper';

function category(id: string, name: string, parentCategoryId: string | null, status = 'Published'): CategoryResponse {
  return {
    id,
    categoryCode: id.toUpperCase(),
    name,
    parentCategoryId,
    sortOrder: 0,
    status,
    updatedAt: null
  };
}

describe('category parent options mapper', () => {
  const roots = buildCategoryTree([
    category('a', 'Apparel', null),
    category('b', 'Shirts', 'a'),
    category('c', 'Dress Shirts', 'b'),
    category('d', 'Footwear', null),
    category('e', 'Retired', null, 'Archived')
  ], new Set(['a', 'b', 'c', 'd', 'e'])).roots;

  it('treats the category itself and all its descendants as ineligible parents', () => {
    const ineligible = ineligibleParentIds(roots, 'a');

    expect(ineligible.has('a')).toBeTrue();
    expect(ineligible.has('b')).toBeTrue();
    expect(ineligible.has('c')).toBeTrue();
    expect(ineligible.has('d')).toBeFalse();
  });

  it('returns no ineligible ids when creating a new category', () => {
    expect(ineligibleParentIds(roots, undefined).size).toBe(0);
  });

  it('disables ineligible and archived nodes in the selector', () => {
    const nodes = toParentSelectorNodes(roots, 'a');
    const byKey = new Map(flatten(nodes).map(node => [node.key, node]));

    expect(byKey.get('a')?.disabled).toBeTrue();
    expect(byKey.get('c')?.disabled).toBeTrue();
    expect(byKey.get('d')?.disabled).toBeFalse();
    expect(byKey.get('e')?.disabled).toBeTrue();
  });

  function flatten(nodes: ReturnType<typeof toParentSelectorNodes>): ReturnType<typeof toParentSelectorNodes> {
    return nodes.flatMap(node => [node, ...flatten(node.children ?? [])]);
  }
});

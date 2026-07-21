import { CategoryResponse } from '../api/responses/category.response';
import { buildCategoryTree, filterCategoryTree, flattenVisibleCategoryTree } from './category-tree.mapper';

describe('category tree mapper', () => {
  it('builds roots and nested children from a flat list', () => {
    const result = buildCategoryTree([
      category('root', 'CAT001', 'Clothing', null, 1),
      category('men', 'CAT002', 'Men', 'root', 1),
      category('shirts', 'CAT003', 'Shirts', 'men', 1)
    ], new Set(['root', 'men']));

    expect(result.roots.length).toBe(1);
    expect(result.roots[0].children[0].children[0].name).toBe('Shirts');
    expect(flattenVisibleCategoryTree(result.roots).map(node => node.name)).toEqual(['Clothing', 'Men', 'Shirts']);
  });

  it('sorts siblings by sort order and then name', () => {
    const result = buildCategoryTree([
      category('root', 'CAT001', 'Root', null, 1),
      category('b', 'CAT003', 'Beta', 'root', 2),
      category('a', 'CAT002', 'Alpha', 'root', 2),
      category('c', 'CAT004', 'First', 'root', 1)
    ], new Set(['root']));

    expect(result.roots[0].children.map(node => node.name)).toEqual(['First', 'Alpha', 'Beta']);
  });

  it('renders missing parents at root with a diagnostic', () => {
    const result = buildCategoryTree([category('child', 'CAT002', 'Child', 'missing', 1)]);

    expect(result.roots[0].name).toBe('Child');
    expect(result.roots[0].isInvalid).toBeTrue();
    expect(result.diagnostics[0]).toContain('Missing parent');
  });

  it('handles self references without recursion', () => {
    const result = buildCategoryTree([category('self', 'CAT002', 'Self', 'self', 1)]);

    expect(result.roots[0].isInvalid).toBeTrue();
    expect(result.diagnostics[0]).toContain('Self-referencing parent');
  });

  it('handles circular references without recursion', () => {
    const result = buildCategoryTree([
      category('a', 'CAT001', 'A', 'b', 1),
      category('b', 'CAT002', 'B', 'a', 1)
    ]);

    expect(result.roots.length).toBeGreaterThan(0);
    expect(result.diagnostics.join(' ')).toContain('Circular parent reference');
  });

  it('reports duplicate IDs and renders the first safely', () => {
    const result = buildCategoryTree([
      category('dup', 'CAT001', 'First', null, 1),
      category('dup', 'CAT002', 'Second', null, 2)
    ]);

    expect(result.roots.length).toBe(1);
    expect(result.roots[0].isInvalid).toBeTrue();
    expect(result.diagnostics[0]).toContain('Duplicate category ID');
  });

  it('search reveals matching descendants and their ancestors', () => {
    const tree = buildCategoryTree([
      category('root', 'CAT001', 'Clothing', null, 1),
      category('men', 'CAT002', 'Men', 'root', 1),
      category('shirts', 'CAT003', 'T-Shirts', 'men', 1)
    ]).roots;

    const filtered = filterCategoryTree(tree, 'T-Shirts', '', false, '', '');

    expect(flattenVisibleCategoryTree(filtered).map(node => node.name)).toEqual(['Clothing', 'Men', 'T-Shirts']);
    expect(filtered[0].isContextOnly).toBeTrue();
  });

  it('does not expand unfiltered parents unless their IDs are expanded', () => {
    const tree = buildCategoryTree(categoryHierarchy()).roots;
    const filtered = filterCategoryTree(tree, '', '', false, '', '');

    expect(flattenVisibleCategoryTree(filtered).map(node => node.name)).toEqual(['Blank']);
  });

  it('expands by category IDs rather than row indexes or object references', () => {
    const tree = buildCategoryTree(categoryHierarchy(), new Set(['blank', 'shirt'])).roots;

    expect(flattenVisibleCategoryTree(tree).map(node => node.name)).toEqual(['Blank', 'Shirt', 'TShirt']);
  });

  it('collapsing an expanded parent hides all descendants', () => {
    const expanded = buildCategoryTree(categoryHierarchy(), new Set(['blank', 'shirt'])).roots;
    expect(flattenVisibleCategoryTree(expanded).map(node => node.name)).toEqual(['Blank', 'Shirt', 'TShirt']);

    const collapsed = buildCategoryTree(categoryHierarchy(), new Set(['shirt'])).roots;
    expect(flattenVisibleCategoryTree(collapsed).map(node => node.name)).toEqual(['Blank']);
  });

  it('filtering keeps the ancestor path visible without corrupting the stored expansion state', () => {
    const tree = buildCategoryTree(categoryHierarchy()).roots;
    const filtered = filterCategoryTree(tree, 'TShirt', '', false, '', '');

    expect(flattenVisibleCategoryTree(filtered).map(node => node.name)).toEqual(['Blank', 'Shirt', 'TShirt']);
    expect(tree[0].isExpanded).toBeFalse();
  });
});

function categoryHierarchy(): CategoryResponse[] {
  return [
    category('blank', 'BLANK', 'Blank', null, 1),
    category('shirt', 'SHIRT', 'Shirt', 'blank', 1),
    category('tshirt', 'TSHIRT', 'TShirt', 'shirt', 1)
  ];
}

function category(
  id: string,
  categoryCode: string,
  name: string,
  parentCategoryId: string | null,
  sortOrder: number): CategoryResponse {
  return {
    id,
    categoryCode,
    name,
    parentCategoryId,
    sortOrder,
    description: null,
    status: 'Draft'
  };
}

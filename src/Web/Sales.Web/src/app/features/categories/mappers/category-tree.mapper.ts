import { CategoryResponse } from '../api/responses/category.response';
import { CategoryTreeBuildResult, CategoryTreeNode } from '../models/category-tree-node.model';

export function buildCategoryTree(
  categories: CategoryResponse[],
  expandedCategoryIds: ReadonlySet<string> = new Set()): CategoryTreeBuildResult {
  const diagnostics: string[] = [];
  const nodes = new Map<string, CategoryTreeNode>();
  const duplicateIds = new Set<string>();

  for (const category of categories) {
    if (nodes.has(category.id)) {
      duplicateIds.add(category.id);
      diagnostics.push(`Duplicate category ID ${category.id} was rendered once at the root level.`);
      continue;
    }

    nodes.set(category.id, {
      id: category.id,
      code: category.categoryCode,
      name: category.name,
      description: category.description,
      parentCategoryId: category.parentCategoryId,
      status: category.status,
      sortOrder: category.sortOrder,
      productCount: category.productCount,
      updatedAt: category.updatedAt,
      level: 0,
      hasChildren: false,
      isExpanded: expandedCategoryIds.has(category.id),
      children: [],
      path: category.name,
      source: category,
      isInvalid: false
    });
  }

  const roots: CategoryTreeNode[] = [];
  for (const node of nodes.values()) {
    if (duplicateIds.has(node.id)) {
      node.isInvalid = true;
      node.invalidReason = 'Duplicate ID';
      roots.push(node);
      continue;
    }

    const parentId = node.parentCategoryId;
    if (!parentId) {
      roots.push(node);
      continue;
    }

    if (parentId === node.id) {
      markInvalidRoot(node, roots, diagnostics, 'Self-referencing parent');
      continue;
    }

    const parent = nodes.get(parentId);
    if (!parent) {
      markInvalidRoot(node, roots, diagnostics, `Missing parent ${parentId}`);
      continue;
    }

    if (hasAncestor(parent, node.id, nodes)) {
      markInvalidRoot(node, roots, diagnostics, 'Circular parent reference');
      continue;
    }

    parent.children.push(node);
    node.parentCategoryName = parent.name;
  }

  assignPresentation(roots, []);
  return {
    roots: sortNodes(roots),
    diagnostics
  };
}

export function flattenVisibleCategoryTree(nodes: CategoryTreeNode[]): CategoryTreeNode[] {
  const rows: CategoryTreeNode[] = [];
  for (const node of sortNodes(nodes)) {
    rows.push(node);
    if (node.isExpanded && node.children.length > 0) {
      rows.push(...flattenVisibleCategoryTree(node.children));
    }
  }

  return rows;
}

export function filterCategoryTree(
  nodes: CategoryTreeNode[],
  searchText: string,
  statusFilter: string,
  rootOnly: boolean,
  hasChildrenFilter: string,
  hasProductsFilter: string): CategoryTreeNode[] {
  const normalizedSearch = searchText.trim().toLowerCase();
  const hasActiveFilter = Boolean(normalizedSearch || statusFilter || rootOnly || hasChildrenFilter || hasProductsFilter);
  return sortNodes(nodes)
    .map(node => filterNode(node, normalizedSearch, statusFilter, rootOnly, hasChildrenFilter, hasProductsFilter, hasActiveFilter))
    .filter((node): node is CategoryTreeNode => node !== null);
}

function filterNode(
  node: CategoryTreeNode,
  normalizedSearch: string,
  statusFilter: string,
  rootOnly: boolean,
  hasChildrenFilter: string,
  hasProductsFilter: string,
  hasActiveFilter: boolean): CategoryTreeNode | null {
  const filteredChildren = node.children
    .map(child => filterNode(child, normalizedSearch, statusFilter, false, hasChildrenFilter, hasProductsFilter, hasActiveFilter))
    .filter((child): child is CategoryTreeNode => child !== null);

  const searchMatches = !normalizedSearch ||
    node.name.toLowerCase().includes(normalizedSearch) ||
    node.code.toLowerCase().includes(normalizedSearch) ||
    (node.parentCategoryName ?? '').toLowerCase().includes(normalizedSearch);
  const statusMatches = !statusFilter || node.status === statusFilter;
  const rootMatches = !rootOnly || node.level === 0;
  const childMatches = !hasChildrenFilter ||
    (hasChildrenFilter === 'yes' ? node.hasChildren : !node.hasChildren);
  const productMatches = !hasProductsFilter ||
    (hasProductsFilter === 'yes' ? (node.productCount ?? 0) > 0 : (node.productCount ?? 0) === 0);
  const ownMatch = searchMatches && statusMatches && rootMatches && childMatches && productMatches;

  if (!ownMatch && filteredChildren.length === 0) {
    return null;
  }

  return {
    ...node,
    isExpanded: hasActiveFilter && filteredChildren.length > 0 ? true : node.isExpanded,
    isContextOnly: !ownMatch && filteredChildren.length > 0,
    // Recomputed for the copy: keeping the unfiltered value would render an expand toggle on a row
    // whose children were all filtered away.
    hasChildren: filteredChildren.length > 0,
    children: filteredChildren
  };
}

function markInvalidRoot(
  node: CategoryTreeNode,
  roots: CategoryTreeNode[],
  diagnostics: string[],
  reason: string): void {
  node.isInvalid = true;
  node.invalidReason = reason;
  diagnostics.push(`${node.code} ${node.name}: ${reason}.`);
  roots.push(node);
}

function hasAncestor(
  node: CategoryTreeNode,
  targetId: string,
  nodes: ReadonlyMap<string, CategoryTreeNode>): boolean {
  const visited = new Set<string>();
  let current: CategoryTreeNode | undefined = node;
  while (current?.parentCategoryId) {
    if (current.id === targetId || current.parentCategoryId === targetId) {
      return true;
    }

    if (visited.has(current.id)) {
      return true;
    }

    visited.add(current.id);
    current = nodes.get(current.parentCategoryId);
  }

  return false;
}

function assignPresentation(nodes: CategoryTreeNode[], ancestors: string[]): void {
  for (const node of nodes) {
    node.level = Math.min(ancestors.length, 8);
    node.hasChildren = node.children.length > 0;
    node.path = [...ancestors, node.name].join(' / ');
    if (node.children.length > 0) {
      node.children = sortNodes(node.children);
      assignPresentation(node.children, [...ancestors, node.name]);
    }
  }
}

function sortNodes(nodes: CategoryTreeNode[]): CategoryTreeNode[] {
  return [...nodes].sort((left, right) =>
    left.sortOrder - right.sortOrder || left.name.localeCompare(right.name));
}

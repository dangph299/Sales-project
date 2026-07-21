import { NzTreeNodeOptions } from 'ng-zorro-antd/core/tree';
import { CategoryTreeNode } from '../models/category-tree-node.model';
import { flattenVisibleCategoryTree } from './category-tree.mapper';

/** Ids that must not be selectable as a parent: the node itself and its descendants. */
export function ineligibleParentIds(roots: CategoryTreeNode[], categoryId: string | undefined): Set<string> {
  const ineligible = new Set<string>();
  if (!categoryId) {
    return ineligible;
  }

  const node = findNode(roots, categoryId);
  if (!node) {
    return ineligible;
  }

  ineligible.add(categoryId);
  flattenVisibleCategoryTree([{ ...node, isExpanded: true }])
    .slice(1)
    .forEach(descendant => ineligible.add(descendant.id));

  return ineligible;
}

/** Projects the hierarchy into nz-tree-select nodes, disabling illegal parents. */
export function toParentSelectorNodes(
  roots: CategoryTreeNode[],
  selectedCategoryId: string | undefined): NzTreeNodeOptions[] {
  const ineligible = ineligibleParentIds(roots, selectedCategoryId);

  const mapNode = (node: CategoryTreeNode): NzTreeNodeOptions => ({
    title: `${node.path} (${node.code})`,
    key: node.id,
    value: node.id,
    disabled: ineligible.has(node.id) || node.status === 'Archived',
    children: node.children.map(mapNode)
  });

  return roots.map(mapNode);
}

function findNode(roots: CategoryTreeNode[], categoryId: string): CategoryTreeNode | null {
  const expandAll = (nodes: CategoryTreeNode[]): CategoryTreeNode[] =>
    nodes.flatMap(node => [node, ...expandAll(node.children)]);

  return expandAll(roots).find(node => node.id === categoryId) ?? null;
}

import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { ApiClientError, ApiResponseReader } from '../../../../core/api/api-client-result';
import { ValidationError } from '../../../../core/api/api-error.model';
import { DataTableComponent } from '../../../../shared/components/data-table/data-table.component';
import { TableCellTemplateDirective } from '../../../../shared/components/data-table/table-cell-template.directive';
import { DropdownComponent } from '../../../../shared/components/dropdown/dropdown.component';
import { FormDialogComponent } from '../../../../shared/components/form-dialog/form-dialog.component';
import { PageStateComponent } from '../../../../shared/components/page-state/page-state.component';
import { StatusTagComponent } from '../../../../shared/components/status-tag/status-tag.component';
import { FocusFirstRequiredDirective } from '../../../../shared/directives/focus-first-required.directive';
import { TablePageChange } from '../../../../shared/models/table.model';
import { DateTimePipe } from '../../../../shared/pipes/date-time.pipe';
import { ApiNotifierService } from '../../../../shared/services/api-notifier.service';
import { ConfirmDeleteService } from '../../../../shared/services/confirm-delete.service';
import { describeApiError } from '../../../../shared/utilities/describe-api-error';
import { CategoryCodes } from '../../../common/constants/category-codes';
import { CategoryApiService } from '../../api/category-api.service';
import { CategoryResponse } from '../../api/responses/category.response';
import { CategoryFormComponent } from '../../components/category-form/category-form.component';
import { CategoryStatus, categoryStatusDisplay } from '../../constants/category-status';
import { toParentSelectorNodes } from '../../mappers/category-parent-options.mapper';
import { buildCategoryTree, filterCategoryTree, flattenVisibleCategoryTree } from '../../mappers/category-tree.mapper';
import { CategoryFormModel, emptyCategoryForm } from '../../models/category-form.model';
import { CategoryTreeNode } from '../../models/category-tree-node.model';
import { categoryHierarchyColumns } from './category-hierarchy.columns';

@Component({
  selector: 'app-category-hierarchy-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DataTableComponent,
    TableCellTemplateDirective,
    DropdownComponent,
    FormDialogComponent,
    PageStateComponent,
    StatusTagComponent,
    CategoryFormComponent,
    DateTimePipe,
    FocusFirstRequiredDirective,
    NzAlertModule,
    NzButtonModule,
    NzCardModule,
    NzIconModule,
    NzInputModule,
    NzModalModule,
    NzTagModule,
    NzToolTipModule
  ],
  templateUrl: './category-hierarchy-page.component.html',
  styleUrl: './category-hierarchy-page.component.scss'
})
export class CategoryHierarchyPageComponent implements OnInit {
  private readonly categoryApi = inject(CategoryApiService);
  private readonly modal = inject(NzModalService);
  private readonly apiNotifier = inject(ApiNotifierService);
  private readonly confirmDelete = inject(ConfirmDeleteService);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly mutationErrorMessage = signal('');
  readonly validationErrors = signal<ValidationError[]>([]);
  readonly selectedCategory = signal<CategoryResponse | null>(null);
  readonly expandedCategoryIds = signal<ReadonlySet<string>>(new Set());
  readonly categories = signal<CategoryResponse[]>([]);
  readonly categoryModalOpen = signal(false);
  readonly categoryCreateFocusTrigger = signal(0);

  readonly searchText = signal('');
  readonly statusFilter = signal('');
  readonly rootFilter = signal('');
  readonly hasChildrenFilter = signal('');
  readonly hasProductsFilter = signal('');
  pageIndex = 1;
  pageSize = 20;
  readonly pageSizeOptions = [10, 20, 50];
  categoryForm: CategoryFormModel = emptyCategoryForm();

  readonly statusDisplay = categoryStatusDisplay;
  readonly tableColumns = categoryHierarchyColumns;
  readonly statusOptions = [
    { value: '', label: 'All' },
    { value: 'Draft', label: 'Draft' },
    { value: 'Published', label: 'Published' },
    { value: 'Archived', label: 'Archived' }
  ];
  readonly rowIdentity = (node: CategoryTreeNode): string => node.id;
  readonly rowClass = (node: CategoryTreeNode): string => node.isContextOnly ? 'context-row' : '';
  readonly rowAriaLevel = (node: CategoryTreeNode): number => node.level + 1;
  readonly rowAriaExpanded = (node: CategoryTreeNode): boolean | null => node.hasChildren ? node.isExpanded : null;

  readonly treeResult = computed(() => buildCategoryTree(this.categories(), this.expandedCategoryIds()));
  readonly treeDiagnostics = computed(() => this.treeResult().diagnostics);
  readonly filteredTree = computed(() => filterCategoryTree(
    this.treeResult().roots,
    this.searchText(),
    this.statusFilter(),
    this.rootFilter() === 'root',
    this.hasChildrenFilter(),
    this.hasProductsFilter()));
  readonly visibleRows = computed(() => flattenVisibleCategoryTree(this.filteredTree()));

  readonly parentOptions = computed(() =>
    toParentSelectorNodes(this.treeResult().roots, this.selectedCategory()?.id));

  ngOnInit(): void {
    void this.loadCategories();
  }

  async loadCategories(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');
    try {
      const categories = await this.categoryApi.list();
      this.categories.set(categories);
      this.expandDefaultCategory();
    } catch (error) {
      this.errorMessage.set(describeApiError(error));
    } finally {
      this.loading.set(false);
    }
  }

  readonly modalTitle = computed(() => this.selectedCategory() ? 'Edit Category' : 'Create Category');

  toggleCategory(categoryId: string): void {
    this.expandedCategoryIds.update(current => {
      const next = new Set(current);
      if (next.has(categoryId)) {
        next.delete(categoryId);
      } else {
        next.add(categoryId);
      }

      return next;
    });
  }

  isExpanded(categoryId: string): boolean {
    return this.expandedCategoryIds().has(categoryId);
  }

  expandAll(): void {
    this.expandedCategoryIds.set(new Set(this.getExpandableCategoryIds(this.filteredTree())));
  }

  collapseAll(): void {
    this.expandedCategoryIds.set(new Set());
  }

  resetFilters(): void {
    this.searchText.set('');
    this.statusFilter.set('');
    this.rootFilter.set('');
    this.hasChildrenFilter.set('');
    this.hasProductsFilter.set('');
    this.pageIndex = 1;
  }

  changeSearchText(searchText: string): void {
    this.searchText.set(searchText);
    this.pageIndex = 1;
  }

  changeStatusFilter(status: string): void {
    this.statusFilter.set(status);
    this.pageIndex = 1;
  }

  changeTablePage(page: TablePageChange): void {
    this.pageIndex = page.pageIndex;
    this.pageSize = page.pageSize;
  }

  pagedVisibleRows(): CategoryTreeNode[] {
    const start = (this.effectivePageIndex() - 1) * this.pageSize;
    return this.visibleRows().slice(start, start + this.pageSize);
  }

  visibleRowTotal(): number {
    return this.visibleRows().length;
  }

  effectivePageIndex(): number {
    const maxPage = Math.max(1, Math.ceil(this.visibleRowTotal() / this.pageSize));
    return Math.min(this.pageIndex, maxPage);
  }

  openCreateCategory(): void {
    if (this.saving()) {
      return;
    }

    this.selectedCategory.set(null);
    this.categoryForm = emptyCategoryForm();
    this.validationErrors.set([]);
    this.mutationErrorMessage.set('');
    this.categoryModalOpen.set(true);
  }

  openEditCategory(category: CategoryResponse): void {
    if (this.saving()) {
      return;
    }

    this.selectedCategory.set(category);
    this.categoryForm = {
      name: category.name,
      description: category.description || '',
      parentCategoryId: category.parentCategoryId || '',
      sortOrder: category.sortOrder,
      status: category.status as CategoryStatus
    };
    this.validationErrors.set([]);
    this.mutationErrorMessage.set('');
    this.categoryModalOpen.set(true);
  }

  closeCategoryModal(): void {
    if (this.saving()) {
      return;
    }

    this.categoryModalOpen.set(false);
    this.selectedCategory.set(null);
    this.categoryForm = emptyCategoryForm();
    this.validationErrors.set([]);
    this.mutationErrorMessage.set('');
  }

  triggerCategoryCreateFocus(): void {
    if (this.categoryModalOpen() && !this.selectedCategory()) {
      this.categoryCreateFocusTrigger.update(value => value + 1);
    }
  }

  async saveCategory(form = this.categoryForm): Promise<void> {
    if (this.saving()) {
      return;
    }

    this.categoryForm = { ...form };
    const description = form.description.trim();
    const missingFieldErrors: ValidationError[] = [];
    if (!form.name.trim()) {
      missingFieldErrors.push({ field: 'Name', message: 'Name is required.' });
    }

    if (missingFieldErrors.length > 0) {
      this.validationErrors.set(missingFieldErrors);
      return;
    }

    this.saving.set(true);
    this.mutationErrorMessage.set('');
    this.validationErrors.set([]);
    try {
      const selected = this.selectedCategory();
      const saved = selected
        ? await this.categoryApi.update(selected.id, {
            name: form.name.trim(),
            description: description || null,
            parentCategoryId: form.parentCategoryId || null,
            sortOrder: form.sortOrder,
            status: form.status
          })
        : await this.categoryApi.create({
            name: form.name.trim(),
            description: description || null,
            parentCategoryId: form.parentCategoryId || null,
            sortOrder: form.sortOrder
          });

      this.upsertCategory(saved);
      if (saved.parentCategoryId) {
        this.expandedCategoryIds.set(new Set([...this.expandedCategoryIds(), saved.parentCategoryId]));
      }

      this.selectedCategory.set(saved);
      this.categoryModalOpen.set(false);
      this.categoryForm = emptyCategoryForm();
      this.validationErrors.set([]);
      this.mutationErrorMessage.set('');
    } catch (error) {
      await this.handleMutationError(error, 'Save Category Failed');
    } finally {
      this.saving.set(false);
    }
  }

  async deleteCategory(category: CategoryResponse): Promise<void> {
    if (this.saving()) {
      return;
    }

    if (!await this.confirmDelete.open({
      title: 'Delete Category',
      itemName: category.name
    })) {
      return;
    }

    await this.confirmDeleteCategory(category);
  }

  trackByCategoryId(_: number, node: CategoryTreeNode): string {
    return node.id;
  }

  private getExpandableCategoryIds(nodes: CategoryTreeNode[]): string[] {
    return nodes.flatMap(node => [
      ...(node.hasChildren ? [node.id] : []),
      ...this.getExpandableCategoryIds(node.children)
    ]);
  }

  /** Opens the default category on first load so the tree never renders fully collapsed. */
  private expandDefaultCategory(): void {
    if (this.expandedCategoryIds().size > 0) {
      return;
    }

    const defaultCategory = this.categories().find(
      category => category.categoryCode === CategoryCodes.Uncategorized);
    if (defaultCategory) {
      this.expandedCategoryIds.set(new Set([defaultCategory.id]));
    }
  }

  private upsertCategory(category: CategoryResponse): void {
    const categories = this.categories().filter(existing => existing.id !== category.id);
    this.categories.set([...categories, category]);
  }

  private async confirmDeleteCategory(category: CategoryResponse): Promise<void> {
    if (this.saving()) {
      return;
    }

    this.saving.set(true);
    this.mutationErrorMessage.set('');
    try {
      await this.categoryApi.delete(category.id);
      this.categories.set(this.categories().filter(existing => existing.id !== category.id));
      const expanded = new Set(this.expandedCategoryIds());
      expanded.delete(category.id);
      this.expandedCategoryIds.set(expanded);
    } catch (error) {
      await this.handleMutationError(error, 'Delete Category Failed');
    } finally {
      this.saving.set(false);
    }
  }

  private async handleMutationError(error: unknown, title: string): Promise<void> {
    if (this.isConcurrencyConflict(error)) {
      this.categoryModalOpen.set(false);
      this.validationErrors.set([]);
      this.mutationErrorMessage.set('');
      await this.loadCategories();
      this.modal.warning({
        nzTitle: 'Category changed',
        nzContent: 'The category was changed by another request. The latest data has been loaded; please try again.'
      });
      return;
    }

    this.handleFormError(error, title);
  }

  private isConcurrencyConflict(error: unknown): boolean {
    if (!(error instanceof ApiClientError) || error.status !== 409) {
      return false;
    }

    return error.result.errorCode === 'concurrency_conflict'
      || error.result.errors.some(item => item.code === 'current_version');
  }

  private handleFormError(error: unknown, title: string): void {
    if (error instanceof ApiClientError) {
      this.validationErrors.set(error.result.validationErrors);
      const message = ApiResponseReader.formatFailure(error.result);
      this.mutationErrorMessage.set(message);
      this.apiNotifier.error(title, message);
      return;
    }

    const message = this.apiNotifier.error(title, error);
    this.mutationErrorMessage.set(message);
  }
}

import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { ApiClientError, ApiResponseReader } from '../../../../core/api/api-client-result';
import { ValidationError } from '../../../../core/api/api-error.model';
import { PageStateComponent } from '../../../../shared/components/page-state/page-state.component';
import { StatusTagComponent } from '../../../../shared/components/status-tag/status-tag.component';
import { DateTimePipe } from '../../../../shared/pipes/date-time.pipe';
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

@Component({
  selector: 'app-category-hierarchy-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageStateComponent,
    StatusTagComponent,
    CategoryFormComponent,
    DateTimePipe,
    NzAlertModule,
    NzButtonModule,
    NzCardModule,
    NzIconModule,
    NzInputModule,
    NzModalModule,
    NzSelectModule,
    NzTableModule,
    NzTagModule,
    NzToolTipModule
  ],
  templateUrl: './category-hierarchy-page.component.html',
  styleUrl: './category-hierarchy-page.component.scss'
})
export class CategoryHierarchyPageComponent implements OnInit {
  private readonly categoryApi = inject(CategoryApiService);
  private readonly modal = inject(NzModalService);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly validationErrors = signal<ValidationError[]>([]);
  readonly selectedCategory = signal<CategoryResponse | null>(null);
  readonly expandedCategoryIds = signal<ReadonlySet<string>>(new Set());
  readonly categories = signal<CategoryResponse[]>([]);
  readonly categoryModalOpen = signal(false);

  readonly searchText = signal('');
  readonly statusFilter = signal('');
  readonly rootFilter = signal('');
  readonly hasChildrenFilter = signal('');
  readonly hasProductsFilter = signal('');
  categoryForm: CategoryFormModel = emptyCategoryForm();

  readonly statusDisplay = categoryStatusDisplay;

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
  }

  openCreateCategory(): void {
    this.selectedCategory.set(null);
    this.categoryForm = emptyCategoryForm();
    this.validationErrors.set([]);
    this.errorMessage.set('');
    this.categoryModalOpen.set(true);
  }

  openEditCategory(category: CategoryResponse): void {
    this.selectedCategory.set(category);
    this.categoryForm = {
      name: category.name,
      description: category.description || '',
      parentCategoryId: category.parentCategoryId || '',
      sortOrder: category.sortOrder,
      status: category.status as CategoryStatus
    };
    this.validationErrors.set([]);
    this.errorMessage.set('');
    this.categoryModalOpen.set(true);
  }

  closeCategoryModal(): void {
    this.categoryModalOpen.set(false);
    this.selectedCategory.set(null);
    this.categoryForm = emptyCategoryForm();
    this.validationErrors.set([]);
    this.errorMessage.set('');
  }

  async saveCategory(): Promise<void> {
    const missingFieldErrors: ValidationError[] = [];
    if (!this.categoryForm.name.trim()) {
      missingFieldErrors.push({ field: 'Name', message: 'Name is required.' });
    }

    if (missingFieldErrors.length > 0) {
      this.validationErrors.set(missingFieldErrors);
      return;
    }

    this.saving.set(true);
    this.errorMessage.set('');
    this.validationErrors.set([]);
    try {
      const selected = this.selectedCategory();
      const saved = selected
        ? await this.categoryApi.update(selected.id, {
            name: this.categoryForm.name.trim(),
            description: this.categoryForm.description.trim() || null,
            parentCategoryId: this.categoryForm.parentCategoryId || null,
            sortOrder: this.categoryForm.sortOrder,
            status: this.categoryForm.status
          })
        : await this.categoryApi.create({
            name: this.categoryForm.name.trim(),
            description: this.categoryForm.description.trim() || null,
            parentCategoryId: this.categoryForm.parentCategoryId || null,
            sortOrder: this.categoryForm.sortOrder
          });

      this.upsertCategory(saved);
      if (saved.parentCategoryId) {
        this.expandedCategoryIds.set(new Set([...this.expandedCategoryIds(), saved.parentCategoryId]));
      }

      this.selectedCategory.set(saved);
      this.categoryModalOpen.set(false);
    } catch (error) {
      await this.handleMutationError(error);
    } finally {
      this.saving.set(false);
    }
  }

  deleteCategory(category: CategoryResponse): void {
    this.modal.confirm({
      nzTitle: `Delete category "${category.name}"?`,
      nzContent: 'This action cannot be undone.',
      nzOkText: 'Delete',
      nzOkDanger: true,
      nzCancelText: 'Cancel',
      nzOnOk: () => this.confirmDeleteCategory(category)
    });
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
    this.errorMessage.set('');
    try {
      await this.categoryApi.delete(category.id);
      this.categories.set(this.categories().filter(existing => existing.id !== category.id));
      const expanded = new Set(this.expandedCategoryIds());
      expanded.delete(category.id);
      this.expandedCategoryIds.set(expanded);
    } catch (error) {
      await this.handleMutationError(error);
    }
  }

  private async handleMutationError(error: unknown): Promise<void> {
    if (this.isConcurrencyConflict(error)) {
      this.categoryModalOpen.set(false);
      this.validationErrors.set([]);
      await this.loadCategories();
      this.modal.warning({
        nzTitle: 'Category changed',
        nzContent: 'The category was changed by another request. The latest data has been loaded; please try again.'
      });
      return;
    }

    this.handleFormError(error);
  }

  private isConcurrencyConflict(error: unknown): boolean {
    if (!(error instanceof ApiClientError) || error.status !== 409) {
      return false;
    }

    return error.result.errorCode === 'concurrency_conflict'
      || error.result.errors.some(item => item.code === 'current_version');
  }

  private handleFormError(error: unknown): void {
    if (error instanceof ApiClientError) {
      this.validationErrors.set(error.result.validationErrors);
      this.errorMessage.set(ApiResponseReader.formatFailure(error.result));
      return;
    }

    this.errorMessage.set(describeApiError(error));
  }
}

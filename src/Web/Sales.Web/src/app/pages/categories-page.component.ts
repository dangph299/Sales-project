import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClientError, ApiResponseReader } from '../api-client-result';
import { ApiService } from '../api.service';
import { CategoryDto, ECategoryStatus, ValidationError } from '../models';
import { uncategorizedCategoryId } from '../reference-data/reference-data';
import { PageStateComponent } from '../shared/page-state.component';
import { StatusBadgeComponent } from '../shared/status-badge.component';

interface CategoryForm {
  categoryCode: string;
  name: string;
  description: string;
  parentCategoryId: string;
  sortOrder: number;
  status: ECategoryStatus;
}

@Component({
  selector: 'app-categories-page',
  standalone: true,
  imports: [CommonModule, FormsModule, PageStateComponent, StatusBadgeComponent],
  template: `
    <section class="page-header">
      <div>
        <p class="eyebrow">Catalog</p>
        <h1>Categories</h1>
        <p>Tao danh muc, publish de gan vao product, archive khi ngung su dung.</p>
      </div>
    </section>

    <section class="notice-panel">
      <strong>Backend gap</strong>
      <p>Sales API hien chi co create/update category, chua co list/detail. Bang ben duoi hien cac category tao hoac sua trong phien UI nay va category mac dinh.</p>
    </section>

    <section class="content-grid two">
      <article class="panel-card">
        <div class="section-title">
          <h2>Hierarchy View</h2>
          <span>{{ categories().length }} visible</span>
        </div>
        <app-page-state [empty]="categories().length === 0" emptyTitle="Chua co category trong phien nay" emptyText="Tao category moi hoac dung Uncategorized mac dinh."></app-page-state>
        <div class="table-wrap" *ngIf="categories().length > 0">
          <table class="data-table">
            <thead>
              <tr>
                <th>Code</th>
                <th>Name</th>
                <th>Parent</th>
                <th>Sort</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let category of sortedCategories()" (click)="selectCategory(category)" [class.selected]="selectedCategory()?.id === category.id">
                <td>{{ category.categoryCode }}</td>
                <td>{{ category.name }}</td>
                <td>{{ parentName(category.parentCategoryId) }}</td>
                <td>{{ category.sortOrder }}</td>
                <td><app-status-badge [status]="category.status"></app-status-badge></td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>

      <article class="panel-card">
        <div class="section-title">
          <h2>{{ selectedCategory() ? 'Edit Category' : 'Create Category' }}</h2>
          <button type="button" class="secondary" (click)="resetForm()">New</button>
        </div>
        <div class="error-panel" *ngIf="errorMessage()">{{ errorMessage() }}</div>
        <div class="error-panel" *ngIf="formErrorSummary().length > 0">
          <p *ngFor="let message of formErrorSummary()">{{ message }}</p>
        </div>

        <form (ngSubmit)="saveCategory()" class="form-grid">
          <label>Category code
            <input name="categoryCode" [(ngModel)]="categoryForm.categoryCode" [disabled]="!!selectedCategory()" required>
            <small class="field-error" *ngIf="fieldError('CategoryCode')">{{ fieldError('CategoryCode') }}</small>
          </label>
          <label>Name
            <input name="categoryName" [(ngModel)]="categoryForm.name" required>
            <small class="field-error" *ngIf="fieldError('Name')">{{ fieldError('Name') }}</small>
          </label>
          <label>Description
            <textarea name="categoryDescription" [(ngModel)]="categoryForm.description" rows="3"></textarea>
          </label>
          <label>Parent category
            <select name="parentCategoryId" [(ngModel)]="categoryForm.parentCategoryId">
              <option value="">No parent</option>
              <option *ngFor="let category of parentOptions()" [value]="category.id">{{ category.categoryCode }} - {{ category.name }}</option>
            </select>
          </label>
          <label>Sort order
            <input name="sortOrder" type="number" [(ngModel)]="categoryForm.sortOrder" required>
          </label>
          <label>Status
            <select name="categoryStatus" [(ngModel)]="categoryForm.status" [disabled]="!selectedCategory()">
              <option value="Draft">Draft</option>
              <option value="Published">Published</option>
              <option value="Archived">Archived</option>
            </select>
          </label>
          <div class="form-actions">
            <button type="submit" [disabled]="saving()">{{ selectedCategory() ? 'Save changes' : 'Create category' }}</button>
            <button type="button" class="secondary" (click)="resetForm()">Cancel</button>
          </div>
        </form>

        <section class="detail-panel" *ngIf="selectedCategory() as category">
          <h3>Lifecycle actions</h3>
          <p>Published category moi duoc gan cho product moi.</p>
          <div class="actions wrap">
            <button type="button" (click)="changeStatus('Published')" [disabled]="category.status === 'Published'">Publish</button>
            <button type="button" class="warning" (click)="changeStatus('Archived')" [disabled]="category.status === 'Archived'">Archive</button>
          </div>
        </section>
      </article>
    </section>
  `
})
export class CategoriesPageComponent {
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly validationErrors = signal<ValidationError[]>([]);
  readonly selectedCategory = signal<CategoryDto | null>(null);
  readonly categories = signal<CategoryDto[]>([
    {
      id: uncategorizedCategoryId,
      categoryCode: 'CAT001',
      name: 'Uncategorized',
      description: 'Default seeded category',
      parentCategoryId: null,
      sortOrder: 0,
      status: 'Published'
    }
  ]);
  categoryForm: CategoryForm = this.emptyForm();

  constructor(private readonly api: ApiService) {}

  sortedCategories(): CategoryDto[] {
    return [...this.categories()].sort((leftCategory, rightCategory) => leftCategory.sortOrder - rightCategory.sortOrder);
  }

  parentOptions(): CategoryDto[] {
    const selectedCategory = this.selectedCategory();
    return this.categories().filter(category => category.id !== selectedCategory?.id && category.status !== 'Archived');
  }

  parentName(parentCategoryId: string | null | undefined): string {
    if (!parentCategoryId) {
      return '-';
    }

    return this.categories().find(category => category.id === parentCategoryId)?.name ?? parentCategoryId;
  }

  selectCategory(category: CategoryDto): void {
    this.selectedCategory.set(category);
    this.categoryForm = {
      categoryCode: category.categoryCode,
      name: category.name,
      description: category.description || '',
      parentCategoryId: category.parentCategoryId || '',
      sortOrder: category.sortOrder,
      status: category.status as ECategoryStatus
    };
    this.validationErrors.set([]);
  }

  resetForm(): void {
    this.selectedCategory.set(null);
    this.categoryForm = this.emptyForm();
    this.validationErrors.set([]);
    this.errorMessage.set('');
  }

  async saveCategory(): Promise<void> {
    if (!this.categoryForm.categoryCode.trim() || !this.categoryForm.name.trim()) {
      this.validationErrors.set([
        { field: 'CategoryCode', message: 'Category code is required.' },
        { field: 'Name', message: 'Name is required.' }
      ]);
      return;
    }

    this.saving.set(true);
    this.errorMessage.set('');
    this.validationErrors.set([]);
    try {
      const selectedCategory = this.selectedCategory();
      const savedCategory = selectedCategory
        ? await this.api.updateCategory(selectedCategory.id, this.updateRequest(this.categoryForm.status))
        : await this.api.createCategory({
            categoryCode: this.categoryForm.categoryCode.trim(),
            name: this.categoryForm.name.trim(),
            description: this.categoryForm.description.trim() || null,
            parentCategoryId: this.categoryForm.parentCategoryId || null,
            sortOrder: this.categoryForm.sortOrder
          });
      this.upsertCategory(savedCategory);
      this.selectCategory(savedCategory);
    } catch (error) {
      this.handleFormError(error);
    } finally {
      this.saving.set(false);
    }
  }

  async changeStatus(status: ECategoryStatus): Promise<void> {
    const selectedCategory = this.selectedCategory();
    if (!selectedCategory) {
      return;
    }

    if (status === 'Archived' && !confirm('Archive Category\n\nArchived category khong duoc gan cho product moi. Product cu van giu lich su.\n\nBan co chac chan tiep tuc?')) {
      return;
    }

    this.categoryForm.status = status;
    await this.saveCategory();
  }

  fieldError(field: string): string {
    return this.validationErrors().find(error => error.field.toLowerCase() === field.toLowerCase())?.message ?? '';
  }

  formErrorSummary(): string[] {
    return this.validationErrors().map(error => `${error.field}: ${error.message}`);
  }

  private updateRequest(status: ECategoryStatus) {
    return {
      name: this.categoryForm.name.trim(),
      description: this.categoryForm.description.trim() || null,
      parentCategoryId: this.categoryForm.parentCategoryId || null,
      sortOrder: this.categoryForm.sortOrder,
      status
    };
  }

  private upsertCategory(category: CategoryDto): void {
    const categories = this.categories().filter(existingCategory => existingCategory.id !== category.id);
    this.categories.set([...categories, category]);
  }

  private emptyForm(): CategoryForm {
    return {
      categoryCode: `CAT${Math.floor(100 + Math.random() * 899)}`,
      name: 'New Category',
      description: '',
      parentCategoryId: '',
      sortOrder: 10,
      status: 'Draft'
    };
  }

  private handleFormError(error: unknown): void {
    if (error instanceof ApiClientError) {
      this.validationErrors.set(error.result.validationErrors);
      this.errorMessage.set(ApiResponseReader.formatFailure(error.result));
      return;
    }

    this.errorMessage.set(error instanceof Error ? error.message : 'Request failed.');
  }
}

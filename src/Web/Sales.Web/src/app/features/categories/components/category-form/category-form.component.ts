import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzTreeNodeOptions } from 'ng-zorro-antd/core/tree';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTreeSelectModule } from 'ng-zorro-antd/tree-select';
import { ValidationError } from '../../../../core/api/api-error.model';
import { CategoryFormModel } from '../../models/category-form.model';

@Component({
  selector: 'app-category-form',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NzAlertModule,
    NzButtonModule,
    NzFormModule,
    NzInputModule,
    NzInputNumberModule,
    NzSelectModule,
    NzTreeSelectModule
  ],
  templateUrl: './category-form.component.html',
  styleUrl: './category-form.component.scss'
})
export class CategoryFormComponent {
  @Input({ required: true }) model!: CategoryFormModel;
  @Input() parentOptions: NzTreeNodeOptions[] = [];
  @Input() validationErrors: ValidationError[] = [];
  @Input() errorMessage = '';
  @Input() saving = false;
  @Input() editing = false;

  /** Backend-assigned category code, shown read-only while editing. */
  @Input() assignedCode = '';

  @Output() save = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  fieldError(field: string): string {
    return this.validationErrors.find(error => error.field.toLowerCase() === field.toLowerCase())?.message ?? '';
  }

  get errorSummary(): string[] {
    return this.validationErrors.map(error => `${error.field}: ${error.message}`);
  }
}

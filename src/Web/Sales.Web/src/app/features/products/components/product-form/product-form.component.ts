import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { DropdownComponent } from '../../../../shared/components/dropdown/dropdown.component';
import { SelectOption } from '../../../../shared/models/select-option.model';
import { ValidationError } from '../../../../core/api/api-error.model';
import { CommonStore } from '../../../common/services/common-store.service';
import { ProductFormModel } from '../../models/product-form.model';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, FormsModule, DropdownComponent, NzButtonModule, NzFormModule, NzInputModule],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss'
})
export class ProductFormComponent {
  private readonly common = inject(CommonStore);

  @Input({ required: true }) model!: ProductFormModel;
  @Input() validationErrors: ValidationError[] = [];
  @Input() errorMessage = '';
  @Input() saving = false;
  @Input() editing = false;

  /** Backend-assigned product code, shown read-only while editing. */
  @Input() assignedCode = '';

  @Output() save = new EventEmitter<ProductFormModel>();
  @Output() cancel = new EventEmitter<void>();

  /** Backend-loaded categories, labelled `CODE - Name`. */
  readonly categoryOptions = computed<SelectOption<string>[]>(() =>
    this.common.categoryOptions().map(category => ({ value: category.id, label: category.label })));

  submit(): void {
    this.save.emit({ ...this.model });
  }
}

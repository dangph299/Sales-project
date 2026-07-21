import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { ValidationError } from '../../../../core/api/api-error.model';
import { CommonStore } from '../../../common/services/common-store.service';
import { ProductFormModel } from '../../models/product-form.model';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, FormsModule, NzAlertModule, NzButtonModule, NzFormModule, NzInputModule, NzSelectModule],
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

  @Output() save = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  /** Backend-loaded categories, labelled `CODE - Name`. */
  readonly categoryOptions = this.common.categoryOptions;

  get errorSummary(): string[] {
    return this.validationErrors.map(error => `${error.field}: ${error.message}`);
  }
}

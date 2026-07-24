import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { ValidationError } from '../../../../core/api/api-error.model';
import { FormFieldComponent } from '../../../../shared/components/form-field/form-field.component';
import { CustomerFormModel } from '../../models/customer-form.model';

@Component({
  selector: 'app-customer-form',
  standalone: true,
  imports: [CommonModule, FormsModule, FormFieldComponent, NzFormModule, NzInputModule],
  templateUrl: './customer-form.component.html',
  styleUrl: './customer-form.component.scss'
})
export class CustomerFormComponent {
  @Input({ required: true }) model!: CustomerFormModel;
  @Input() validationErrors: ValidationError[] = [];
  @Input() errorMessage = '';

  @Output() save = new EventEmitter<void>();

  fieldError(field: string): string {
    return this.validationErrors.find(error => error.field.toLowerCase() === field.toLowerCase())?.message ?? '';
  }
}

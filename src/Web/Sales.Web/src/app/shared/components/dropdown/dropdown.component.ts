import { CommonModule } from '@angular/common';
import { Component, ContentChild, EventEmitter, Input, Output, forwardRef } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { SelectOption } from '../../models/select-option.model';
import { DropdownOptionTemplateDirective } from './dropdown-option-template.directive';

@Component({
  selector: 'app-dropdown',
  standalone: true,
  imports: [CommonModule, FormsModule, DropdownOptionTemplateDirective, NzFormModule, NzSelectModule],
  templateUrl: './dropdown.component.html',
  styleUrl: './dropdown.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DropdownComponent),
      multi: true
    }
  ]
})
export class DropdownComponent<T> implements ControlValueAccessor {
  @Input() label = '';
  @Input() name = '';
  @Input() placeholder = 'Select';
  @Input() required = false;
  @Input() disabled = false;
  @Input() readonly = false;
  @Input() loading = false;
  @Input() clearable = false;
  @Input() searchable = false;
  @Input() errorMessage = '';
  @Input() options: SelectOption<T>[] = [];
  @Input() compareWith: (left: T | null, right: T | null) => boolean = (left, right) => left === right;
  @Input() presentation: 'form' | 'toolbar' | 'control' = 'form';
  @Input() controlWidth = '';

  @Output() selected = new EventEmitter<T | null>();

  @ContentChild(DropdownOptionTemplateDirective) readonly optionTemplate?: DropdownOptionTemplateDirective<T>;

  value: T | null = null;

  private onChange: (value: T | null) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  writeValue(value: T | null): void {
    this.value = value;
  }

  registerOnChange(fn: (value: T | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  changeValue(value: T | null): void {
    if (this.readonly || this.disabled) {
      return;
    }

    this.value = value;
    this.onChange(value);
    this.selected.emit(value);
  }

  markTouched(): void {
    this.onTouched();
  }
}

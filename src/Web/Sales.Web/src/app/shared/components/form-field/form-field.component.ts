import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { NzFormModule } from 'ng-zorro-antd/form';

export interface FormFieldMessages {
  required?: string;
  minlength?: string;
  maxlength?: string;
  email?: string;
  pattern?: string;
  min?: string;
  max?: string;
}

const DEFAULT_MESSAGES: Required<FormFieldMessages> = {
  required: 'This field is required.',
  minlength: 'Value is too short.',
  maxlength: 'Value is too long.',
  email: 'Enter a valid email address.',
  pattern: 'Value is in an invalid format.',
  min: 'Value is too small.',
  max: 'Value is too large.'
};

/**
 * Shared wrapper for a single form field: label, required marker, hint and
 * validation message. Works either with a plain `error` string (the app's
 * existing pattern of server/manual validation) or a Reactive Forms
 * `control`, whose built-in Validators errors are mapped to a message.
 */
@Component({
  selector: 'app-form-field',
  standalone: true,
  imports: [CommonModule, NzFormModule],
  template: `
    <nz-form-item>
      <nz-form-label [nzRequired]="required" [nzFor]="for">{{ label }}</nz-form-label>
      <nz-form-control [nzErrorTip]="errorMessage" [nzExtra]="hint">
        <ng-content></ng-content>
      </nz-form-control>
    </nz-form-item>
  `
})
export class FormFieldComponent {
  @Input({ required: true }) label = '';
  @Input() required = false;
  @Input() hint = '';
  @Input() for = '';
  /** Direct error message (existing app pattern: server/manual validation). */
  @Input() error = '';
  /** Optional Reactive Forms control to derive a message from Validators errors. */
  @Input() control: AbstractControl | null = null;
  @Input() messages: FormFieldMessages = {};

  get errorMessage(): string {
    if (this.error) {
      return this.error;
    }

    return this.controlErrorMessage();
  }

  private controlErrorMessage(): string {
    const control = this.control;
    if (!control || !control.errors || (!control.touched && !control.dirty)) {
      return '';
    }

    const errors = control.errors;
    const overrides = this.messages;
    const key = Object.keys(errors).find(errorKey => errorKey in DEFAULT_MESSAGES) as keyof FormFieldMessages | undefined;
    if (!key) {
      return '';
    }

    return overrides[key] ?? DEFAULT_MESSAGES[key];
  }
}

import { TemplateRef } from '@angular/core';

export interface SelectOption<T> {
  value: T;
  label: string;
  disabled?: boolean;
}

export interface SelectOptionContext<T> {
  $implicit: SelectOption<T>;
  option: SelectOption<T>;
}

export interface AutocompleteOptionContext<T> {
  $implicit: T;
  option: T;
}

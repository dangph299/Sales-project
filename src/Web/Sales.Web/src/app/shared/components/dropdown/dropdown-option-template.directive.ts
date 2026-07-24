import { Directive, TemplateRef } from '@angular/core';
import { SelectOptionContext } from '../../models/select-option.model';

@Directive({
  selector: 'ng-template[appDropdownOption]',
  standalone: true
})
export class DropdownOptionTemplateDirective<T> {
  constructor(readonly template: TemplateRef<SelectOptionContext<T>>) {}
}

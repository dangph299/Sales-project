import { Directive, TemplateRef } from '@angular/core';
import { AutocompleteOptionContext } from '../../models/select-option.model';

@Directive({
  selector: 'ng-template[appAutocompleteOption]',
  standalone: true
})
export class AutocompleteOptionTemplateDirective<T> {
  constructor(readonly template: TemplateRef<AutocompleteOptionContext<T>>) {}
}

import { Directive, Input, TemplateRef } from '@angular/core';
import { TableCellContext } from '../../models/table.model';

@Directive({
  selector: 'ng-template[appTableCell]',
  standalone: true
})
export class TableCellTemplateDirective<T> {
  @Input('appTableCell') key = '';

  constructor(readonly template: TemplateRef<TableCellContext<T>>) {}
}

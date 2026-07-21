import { Pipe, PipeTransform } from '@angular/core';
import { formatMoney } from '../utilities/display-formatters';

@Pipe({ name: 'money', standalone: true })
export class MoneyPipe implements PipeTransform {
  transform(amount: number | null | undefined): string {
    return formatMoney(amount);
  }
}

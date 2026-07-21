import { Pipe, PipeTransform } from '@angular/core';
import { formatDateTime } from '../utilities/display-formatters';

@Pipe({ name: 'dateTime', standalone: true })
export class DateTimePipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    return formatDateTime(value);
  }
}

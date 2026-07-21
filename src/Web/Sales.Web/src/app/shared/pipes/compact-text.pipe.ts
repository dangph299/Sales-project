import { Pipe, PipeTransform } from '@angular/core';
import { compactText } from '../utilities/display-formatters';

@Pipe({ name: 'compactText', standalone: true })
export class CompactTextPipe implements PipeTransform {
  transform(value: string | null | undefined, maxLength = 48): string {
    return compactText(value, maxLength);
  }
}

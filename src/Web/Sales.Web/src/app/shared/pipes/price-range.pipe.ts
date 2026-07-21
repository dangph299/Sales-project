import { Pipe, PipeTransform } from '@angular/core';
import { formatPriceRange } from '../utilities/display-formatters';

@Pipe({ name: 'priceRange', standalone: true })
export class PriceRangePipe implements PipeTransform {
  transform(minPrice: number | null | undefined, maxPrice: number | null | undefined): string {
    return formatPriceRange(minPrice, maxPrice);
  }
}

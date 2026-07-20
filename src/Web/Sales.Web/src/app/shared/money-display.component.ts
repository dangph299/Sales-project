import { Component, Input } from '@angular/core';
import { formatMoney, formatPriceRange } from './display-formatters';

@Component({
  selector: 'app-money-display',
  standalone: true,
  template: `{{ text }}`
})
export class MoneyDisplayComponent {
  @Input() amount: number | null | undefined;
  @Input() minPrice: number | null | undefined;
  @Input() maxPrice: number | null | undefined;

  get text(): string {
    if (this.minPrice !== undefined || this.maxPrice !== undefined) {
      return formatPriceRange(this.minPrice, this.maxPrice);
    }

    return formatMoney(this.amount);
  }
}

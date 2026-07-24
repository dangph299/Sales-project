import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { DataTableComponent } from '../../../../shared/components/data-table/data-table.component';
import { TableCellTemplateDirective } from '../../../../shared/components/data-table/table-cell-template.directive';
import { PageStateComponent } from '../../../../shared/components/page-state/page-state.component';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import {
  CartLine,
  cartGrandTotal,
  cartLineTotal,
  cartSubtotal,
  normalizeDiscountPercent,
  normalizeQuantity
} from '../../models/cart-line.model';
import { orderLineEditorColumns } from './order-line-editor.columns';

/** Owns cart line editing: quantity/discount normalisation, line and cart totals. */
@Component({
  selector: 'app-order-line-editor',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DataTableComponent,
    TableCellTemplateDirective,
    PageStateComponent,
    MoneyPipe,
    NzButtonModule,
    NzInputNumberModule
  ],
  templateUrl: './order-line-editor.component.html',
  styleUrl: './order-line-editor.component.scss'
})
export class OrderLineEditorComponent {
  @Input() lines: CartLine[] = [];
  @Input() canCreateOrder = false;
  @Input() actionLabel = 'Save';
  @Input() saving = false;
  @Input() readonly = false;
  @Input() showFooterAction = true;

  /**
   * Whether this component shows the cart totals. The order modal turns them off
   * because it presents the total as a statistic card of its own.
   */
  @Input() showSummary = true;

  @Output() linesChange = new EventEmitter<CartLine[]>();
  @Output() createOrder = new EventEmitter<void>();

  readonly lineTotal = cartLineTotal;
  readonly tableColumns = orderLineEditorColumns;
  readonly rowIdentity = (line: CartLine): string => line.variant.id;

  get subtotal(): number {
    return cartSubtotal(this.lines);
  }

  get grandTotal(): number {
    return cartGrandTotal(this.lines);
  }

  changeQuantity(productVariantId: string, quantity: number): void {
    if (this.readonly) {
      return;
    }

    this.linesChange.emit(this.lines.map(line =>
      line.variant.id === productVariantId
        ? { ...line, quantity: normalizeQuantity(quantity) }
        : line));
  }

  changeDiscount(productVariantId: string, discountPercent: number): void {
    if (this.readonly) {
      return;
    }

    this.linesChange.emit(this.lines.map(line =>
      line.variant.id === productVariantId
        ? { ...line, discountPercent: normalizeDiscountPercent(discountPercent) }
        : line));
  }

  removeLine(productVariantId: string): void {
    if (this.readonly) {
      return;
    }

    this.linesChange.emit(this.lines.filter(line => line.variant.id !== productVariantId));
  }
}

import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { DropdownComponent } from '../../../../shared/components/dropdown/dropdown.component';
import { SelectOption } from '../../../../shared/models/select-option.model';
import { StockAdjustmentFormModel, StockAdjustmentOperation } from '../../models/stock-adjustment-form.model';
import { StockRow, canAdjustStock } from '../../models/stock-row.model';

@Component({
  selector: 'app-stock-adjustment-form',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DropdownComponent,
    MoneyPipe,
    NzAlertModule,
    NzButtonModule,
    NzDescriptionsModule,
    NzFormModule,
    NzInputModule,
    NzInputNumberModule
  ],
  templateUrl: './stock-adjustment-form.component.html',
  styleUrl: './stock-adjustment-form.component.scss'
})
export class StockAdjustmentFormComponent {
  @Input({ required: true }) row!: StockRow;
  @Input({ required: true }) model!: StockAdjustmentFormModel;
  @Input() saving = false;
  @Input() errorMessage = '';

  @Output() apply = new EventEmitter<void>();

  readonly operationOptions: SelectOption<StockAdjustmentOperation>[] = [
    { value: 'receive', label: 'Receive Stock' },
    { value: 'adjust', label: 'Adjust Stock' }
  ];

  get canAdjust(): boolean {
    return canAdjustStock(this.row);
  }
}

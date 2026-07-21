import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { formatMoney } from '../../../../shared/utilities/display-formatters';
import { CommonStore } from '../../../common/services/common-store.service';
import { buildSkuPreview } from '../../../common/sku/build-sku-preview';
import { ProductVariantFormModel } from '../../models/product-variant-form.model';
import { ProductVariantStatus } from '../../constants/product-variant-status';

@Component({
  selector: 'app-product-variant-form',
  standalone: true,
  imports: [CommonModule, FormsModule, NzButtonModule, NzFormModule, NzInputNumberModule, NzSelectModule],
  templateUrl: './product-variant-form.component.html',
  styleUrl: './product-variant-form.component.scss'
})
export class ProductVariantFormComponent {
  private readonly common = inject(CommonStore);

  @Input({ required: true }) model!: ProductVariantFormModel;
  /** Product code the SKU preview is derived from. */
  @Input() productCode = '';
  @Input() saving = false;
  @Input() disabled = false;
  @Input() editing = false;
  @Input() statusOptions: ProductVariantStatus[] = ['Draft', 'Published'];

  @Output() save = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  readonly colors = this.common.colors;
  readonly sizes = this.common.sizes;

  get skuPreview(): string {
    return buildSkuPreview(this.productCode, this.model.colorId, this.model.sizeId, this.colors(), this.sizes());
  }

  /** Swatch color for the currently selected color, used to tint the preview strip. */
  get selectedColorHex(): string {
    return this.colors().find(color => color.id === this.model.colorId)?.hexCode || 'transparent';
  }

  /** Reuses the shared money formatter so the input reads the same as every price in the app. */
  readonly formatPrice = (value: number): string => formatMoney(value);

  readonly parsePrice = (value: string): string => value.replace(/[^\d]/g, '');
}

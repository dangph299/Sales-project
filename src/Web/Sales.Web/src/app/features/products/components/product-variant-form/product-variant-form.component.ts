import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { DropdownComponent } from '../../../../shared/components/dropdown/dropdown.component';
import { DropdownOptionTemplateDirective } from '../../../../shared/components/dropdown/dropdown-option-template.directive';
import { FormFieldComponent } from '../../../../shared/components/form-field/form-field.component';
import { SelectOption } from '../../../../shared/models/select-option.model';
import { formatMoney } from '../../../../shared/utilities/display-formatters';
import { CommonStore } from '../../../common/services/common-store.service';
import { ColorResponse } from '../../../common/contracts/color.response';
import { SizeLookupResponse } from '../../../common/contracts/size-lookup.response';
import { buildSkuPreview } from '../../../common/sku/build-sku-preview';
import { ProductVariantFormModel } from '../../models/product-variant-form.model';
import { ProductVariantStatus } from '../../constants/product-variant-status';

@Component({
  selector: 'app-product-variant-form',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DropdownComponent,
    DropdownOptionTemplateDirective,
    FormFieldComponent,
    NzAlertModule,
    NzFormModule,
    NzInputNumberModule
  ],
  templateUrl: './product-variant-form.component.html',
  styleUrl: './product-variant-form.component.scss'
})
export class ProductVariantFormComponent {
  private readonly common = inject(CommonStore);

  @Input({ required: true }) model!: ProductVariantFormModel;
  /** Product code the SKU preview is derived from. */
  @Input() productCode = '';
  @Input() errorMessage = '';
  @Input() editing = false;
  @Input() statusOptions: ProductVariantStatus[] = ['Draft', 'Published'];

  @Output() save = new EventEmitter<void>();

  readonly colors = this.common.colors;
  readonly sizes = this.common.sizes;
  readonly colorOptions = computed<SelectOption<string>[]>(() =>
    this.colors().map(color => ({ value: color.id, label: `${color.code} - ${color.name}` })));
  readonly sizeOptions = computed<SelectOption<string>[]>(() =>
    this.sizes().map(size => ({ value: size.id, label: `${size.code} - ${size.name}` })));
  get statusSelectOptions(): SelectOption<ProductVariantStatus>[] {
    return this.statusOptions.map(status => ({ value: status, label: status }));
  }

  get skuPreview(): string {
    return buildSkuPreview(this.productCode, this.model.colorId, this.model.sizeId, this.colors(), this.sizes());
  }

  /** Swatch color for the currently selected color, used to tint the preview strip. */
  get selectedColorHex(): string {
    return this.colors().find(color => color.id === this.model.colorId)?.hexCode || 'transparent';
  }

  colorById(colorId: string): ColorResponse | null {
    return this.colors().find(color => color.id === colorId) ?? null;
  }

  sizeById(sizeId: string): SizeLookupResponse | null {
    return this.sizes().find(size => size.id === sizeId) ?? null;
  }

  /** Reuses the shared money formatter so the input reads the same as every price in the app. */
  readonly formatPrice = (value: number): string => formatMoney(value);

  readonly parsePrice = (value: string): string => value.replace(/[^\d]/g, '');
}

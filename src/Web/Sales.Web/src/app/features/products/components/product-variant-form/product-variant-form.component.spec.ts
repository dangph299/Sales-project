import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { CommonStore } from '../../../common/services/common-store.service';
import { emptyProductVariantForm } from '../../models/product-variant-form.model';
import { ProductVariantFormComponent } from './product-variant-form.component';

/** Backend ids that are not the seeded GUIDs, so hardcoded identifiers cannot pass these tests. */
const colors = signal([
  { id: 'color-id-1', code: 'BLK', name: 'Black', hexCode: '#000000' },
  { id: 'color-id-2', code: 'RED', name: 'Red', hexCode: '#FF0000' }
]);

const sizes = signal([
  { id: 'size-id-1', code: 'S', name: 'Small', sortOrder: 30 },
  { id: 'size-id-2', code: 'M', name: 'Medium', sortOrder: 40 }
]);

describe('ProductVariantFormComponent', () => {
  let fixture: ComponentFixture<ProductVariantFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductVariantFormComponent],
      providers: [
        provideNoopAnimations(),
        { provide: CommonStore, useValue: { colors, sizes } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductVariantFormComponent);
    fixture.componentInstance.model = emptyProductVariantForm();
  });

  it('derives the SKU from the codes of the selected backend ids', () => {
    const component = fixture.componentInstance;
    component.productCode = 'prd123';
    component.model.colorId = 'color-id-2';
    component.model.sizeId = 'size-id-2';

    expect(component.skuPreview).toBe('PRD123-RED-M');
  });

  it('keeps the backend id in the model while displaying the code', () => {
    const component = fixture.componentInstance;
    component.productCode = 'PRD123';
    component.model.colorId = 'color-id-1';
    component.model.sizeId = 'size-id-1';

    expect(component.skuPreview).toBe('PRD123-BLK-S');
    expect(component.model.colorId).toBe('color-id-1');
    expect(component.model.sizeId).toBe('size-id-1');
  });

  it('starts with no colour or size until the caller supplies loaded defaults', () => {
    const form = emptyProductVariantForm();

    expect(form.colorId).toBe('');
    expect(form.sizeId).toBe('');
  });

  it('shows a placeholder until the product code is entered', () => {
    fixture.componentInstance.productCode = '   ';

    expect(fixture.componentInstance.skuPreview).toBe('-');
  });

  it('shows a placeholder when the colour is not a known reference value', () => {
    const component = fixture.componentInstance;
    component.productCode = 'PRD123';
    component.model.colorId = 'not-a-colour';

    expect(component.skuPreview).toBe('-');
  });

  it('tints the preview with the hex of the selected colour', () => {
    fixture.componentInstance.model.colorId = 'color-id-2';

    expect(fixture.componentInstance.selectedColorHex).toBe('#FF0000');
  });
});

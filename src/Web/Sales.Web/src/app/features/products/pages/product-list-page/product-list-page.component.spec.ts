import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { NzNotificationService } from 'ng-zorro-antd/notification';
import { InventoryApiService } from '../../../inventory/api/inventory-api.service';
import { CommonStore } from '../../../common/services/common-store.service';
import { ProductApiService } from '../../api/product-api.service';
import { ProductResponse } from '../../api/responses/product.response';
import { ProductListPageComponent } from './product-list-page.component';

const products = [product('product-1', 'PRD-1', 'Product one', 'Existing description')];

describe('ProductListPageComponent description payload mapping', () => {
  let fixture: ComponentFixture<ProductListPageComponent>;
  let component: ProductListPageComponent;
  let productApi: jasmine.SpyObj<ProductApiService>;

  beforeEach(async () => {
    productApi = jasmine.createSpyObj<ProductApiService>(
      'ProductApiService',
      ['search', 'create', 'update', 'delete', 'addVariant', 'updateVariant']);
    productApi.search.and.resolveTo({
      items: products,
      page: 1,
      pageSize: 20,
      total: products.length
    });

    await TestBed.configureTestingModule({
      imports: [ProductListPageComponent],
      providers: [
        provideNoopAnimations(),
        { provide: ProductApiService, useValue: productApi },
        { provide: InventoryApiService, useValue: jasmine.createSpyObj<InventoryApiService>('InventoryApiService', ['getByVariant']) },
        { provide: NzNotificationService, useValue: jasmine.createSpyObj<NzNotificationService>('NzNotificationService', ['success', 'error', 'warning']) },
        { provide: CommonStore, useValue: commonStoreFake() }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('includes description in the create product request payload', async () => {
    productApi.create.and.resolveTo(product('product-2', 'PRD-2', 'Product two', 'Create description'));

    component.openCreateProduct();
    component.productForm = {
      name: 'Product two',
      description: '  Create description  ',
      categoryId: 'category-1',
      status: 'Draft'
    };

    await component.saveProduct();

    expect(productApi.create).toHaveBeenCalledOnceWith({
      name: 'Product two',
      description: 'Create description',
      categoryId: 'category-1',
      variants: []
    });
  });

  it('patches and includes description in the update product request payload', async () => {
    productApi.update.and.resolveTo({ ...products[0], description: 'Updated description' });

    component.openEditProduct(products[0]);
    expect(component.productForm.description).toBe('Existing description');

    component.productForm.description = '  Updated description  ';
    await component.saveProduct();

    expect(productApi.update).toHaveBeenCalledOnceWith('product-1', {
      name: 'Product one',
      description: 'Updated description',
      categoryId: 'category-1',
      status: 'Draft'
    });
  });

  it('includes description typed into the edit textarea in the update payload', async () => {
    productApi.update.and.resolveTo({ ...products[0], description: 'Typed description' });

    component.openEditProduct(products[0]);
    fixture.detectChanges();
    await fixture.whenStable();

    const descriptionInput = document.body.querySelector('textarea[name="productDescription"]') as HTMLTextAreaElement | null;
    expect(descriptionInput).not.toBeNull();
    descriptionInput!.value = 'Typed description';
    descriptionInput!.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
    await fixture.whenStable();

    const submitButton = document.body.querySelector('app-product-form button[type="submit"]') as HTMLButtonElement | null;
    expect(submitButton).not.toBeNull();
    submitButton!.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(productApi.update).toHaveBeenCalledOnceWith('product-1', jasmine.objectContaining({
      description: 'Typed description'
    }));
  });

  it('sends null description when the product description is blank', async () => {
    productApi.create.and.resolveTo(product('product-2', 'PRD-2', 'Product two', null));

    component.openCreateProduct();
    component.productForm = {
      name: 'Product two',
      description: '   ',
      categoryId: 'category-1',
      status: 'Draft'
    };

    await component.saveProduct();

    expect(productApi.create).toHaveBeenCalledOnceWith({
      name: 'Product two',
      description: null,
      categoryId: 'category-1',
      variants: []
    });
  });
});

function product(id: string, productCode: string, name: string, description: string | null): ProductResponse {
  return {
    id,
    sku: productCode,
    productCode,
    name,
    description,
    categoryId: 'category-1',
    category: { id: 'category-1', categoryCode: 'CAT-1', name: 'Category' },
    status: 'Draft',
    minPrice: null,
    maxPrice: null,
    variants: [],
    isActive: true,
    version: 1,
    updatedAt: '2026-07-21T00:00:00Z',
    isDelete: false
  };
}

function commonStoreFake(): Partial<CommonStore> {
  return {
    colors: signal([]),
    sizes: signal([]),
    categories: signal([{ id: 'category-1', categoryCode: 'CAT-1', name: 'Category', sortOrder: 1, status: 'Draft' }]),
    categoryOptions: signal([{ id: 'category-1', code: 'CAT-1', label: 'CAT-1 - Category' }]),
    ensureLoaded: () => Promise.resolve(),
    defaultCategoryId: () => 'category-1',
    defaultColorId: () => '',
    defaultSizeId: () => ''
  };
}

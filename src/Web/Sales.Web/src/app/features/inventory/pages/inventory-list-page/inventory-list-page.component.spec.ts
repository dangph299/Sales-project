import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { PagedResult } from '../../../../core/api/paged-result.model';
import { ProductLookupApiService } from '../../../common/api/product-lookup-api.service';
import { ProductLookupResponse } from '../../../common/contracts/product-lookup.response';
import { InventoryApiService } from '../../api/inventory-api.service';
import { InventoryListPageComponent } from './inventory-list-page.component';

describe('InventoryListPageComponent stock rows', () => {
  let fixture: ComponentFixture<InventoryListPageComponent>;
  let productLookup: FakeProductLookupApiService;

  beforeEach(async () => {
    productLookup = new FakeProductLookupApiService();

    await TestBed.configureTestingModule({
      imports: [InventoryListPageComponent],
      providers: [
        provideNoopAnimations(),
        { provide: ProductLookupApiService, useValue: productLookup },
        { provide: InventoryApiService, useValue: new FakeInventoryApiService() }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryListPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => {
    fixture.destroy();
  });

  // Stock is physical: a variant holds units whatever lifecycle state its product is in, and a
  // product is normally stocked before it is published. Filtering the lookup by product status hid
  // every variant of an unpublished product, leaving no row to adjust from.
  it('does not restrict the stock list to published products', async () => {
    await fixture.componentInstance.loadStockRows();

    expect(productLookup.lastFilters?.status).toBe('');
  });

  it('lists a variant whose product is still a draft', async () => {
    productLookup.products = [draftProductWithVariant()];

    await fixture.componentInstance.loadStockRows();

    expect(fixture.componentInstance.stockRows().map(row => row.variant.sku)).toEqual(['PRD02-BLK-M']);
  });
});

class FakeProductLookupApiService {
  lastFilters: { status?: string } | null = null;
  products: ProductLookupResponse[] = [];

  search(filters: { status?: string } = {}): Promise<PagedResult<ProductLookupResponse>> {
    this.lastFilters = filters;
    return Promise.resolve({ items: this.products, page: 1, pageSize: 20, total: this.products.length });
  }
}

class FakeInventoryApiService {
  /** No stock has ever been recorded for these variants, which is the case under test. */
  getByVariant(): Promise<null> {
    return Promise.resolve(null);
  }
}

function draftProductWithVariant(): ProductLookupResponse {
  return {
    id: '88888888-8888-8888-8888-888888888888',
    sku: 'PRD02-BLK-M',
    productCode: 'PRD02',
    name: 'Clothing',
    status: 'Draft',
    variants: [
      {
        id: '99999999-9999-9999-9999-999999999999',
        sku: 'PRD02-BLK-M',
        price: 100,
        status: 'Published'
      }
    ]
  };
}

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

  it('asks the server for the requested page instead of only the first', async () => {
    // The grid used to be pinned to page 1, so a product sorted past the first page — and every
    // variant under it — was unreachable.
    fixture.componentInstance.changePage(3);
    await fixture.whenStable();

    expect(productLookup.lastFilters?.page).toBe(3);
  });

  it('returns to the first page when the filters change', async () => {
    fixture.componentInstance.changePage(3);
    await fixture.whenStable();

    fixture.componentInstance.search();
    await fixture.whenStable();

    expect(fixture.componentInstance.pageIndex).toBe(1);
    expect(productLookup.lastFilters?.page).toBe(1);
  });

  it('reports the product count the pager walks through', async () => {
    productLookup.total = 57;

    await fixture.componentInstance.loadStockRows();

    expect(fixture.componentInstance.total()).toBe(57);
  });

  it('steps back to the last populated page when the current one has gone', async () => {
    productLookup.products = [];
    productLookup.total = 20;
    fixture.componentInstance.pageIndex = 5;

    await fixture.componentInstance.loadStockRows();

    expect(fixture.componentInstance.pageIndex).toBe(1);
  });

  it('lists a variant whose product is still a draft', async () => {
    productLookup.products = [draftProductWithVariant()];

    await fixture.componentInstance.loadStockRows();

    expect(fixture.componentInstance.stockRows().map(row => row.variant.sku)).toEqual(['PRD02-BLK-M']);
  });
});

class FakeProductLookupApiService {
  lastFilters: { status?: string; page?: number; pageSize?: number } | null = null;
  products: ProductLookupResponse[] = [];
  total: number | null = null;

  search(filters: { status?: string; page?: number; pageSize?: number } = {}): Promise<PagedResult<ProductLookupResponse>> {
    this.lastFilters = filters;
    return Promise.resolve({
      items: this.products,
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20,
      total: this.total ?? this.products.length
    });
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

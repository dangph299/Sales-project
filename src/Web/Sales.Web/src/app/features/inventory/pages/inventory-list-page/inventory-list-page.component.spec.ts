import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { PagedResult } from '../../../../core/api/paged-result.model';
import { ProductLookupApiService } from '../../../common/api/product-lookup-api.service';
import { ProductVariantPageResponse } from '../../../common/contracts/product-lookup.response';
import { InventoryResponse } from '../../api/responses/inventory.response';
import { InventoryApiService } from '../../api/inventory-api.service';
import { InventoryListPageComponent } from './inventory-list-page.component';

describe('InventoryListPageComponent stock rows', () => {
  let fixture: ComponentFixture<InventoryListPageComponent>;
  let productLookup: FakeProductLookupApiService;
  let inventoryApi: FakeInventoryApiService;
  let routeStub: ReturnType<typeof route>;

  beforeEach(async () => {
    productLookup = new FakeProductLookupApiService();
    inventoryApi = new FakeInventoryApiService();
    routeStub = route({});

    await TestBed.configureTestingModule({
      imports: [InventoryListPageComponent],
      providers: [
        provideNoopAnimations(),
        { provide: ProductLookupApiService, useValue: productLookup },
        { provide: InventoryApiService, useValue: inventoryApi },
        { provide: ActivatedRoute, useValue: routeStub }
      ]
    }).compileComponents();

    await createComponent();
  });

  afterEach(() => {
    fixture.destroy();
  });

  it('searches the variant page endpoint instead of the product page endpoint', async () => {
    await fixture.componentInstance.loadStockRows();

    expect(productLookup.searchCalls).toBe(0);
    expect(productLookup.searchVariantsCalls).toBeGreaterThan(0);
  });

  it('asks the server for the requested page instead of only the first', async () => {
    await fixture.componentInstance.changeTablePage({ pageIndex: 3, pageSize: fixture.componentInstance.pageSize });

    expect(productLookup.lastFilters?.page).toBe(3);
  });

  it('returns to the first page when the filters change', async () => {
    await fixture.componentInstance.changeTablePage({ pageIndex: 3, pageSize: fixture.componentInstance.pageSize });

    fixture.componentInstance.search();
    await fixture.whenStable();

    expect(fixture.componentInstance.pageIndex).toBe(1);
    expect(productLookup.lastFilters?.page).toBe(1);
  });

  it('reports the variant count the pager walks through', async () => {
    productLookup.total = 57;

    await fixture.componentInstance.loadStockRows();

    expect(fixture.componentInstance.total()).toBe(57);
  });

  it('steps back to the last populated page when the current one has gone', async () => {
    productLookup.variants = [];
    productLookup.total = 20;

    await fixture.componentInstance.changeTablePage({ pageIndex: 5, pageSize: fixture.componentInstance.pageSize });

    expect(fixture.componentInstance.pageIndex).toBe(1);
  });

  it('sends sorting to the server', async () => {
    productLookup.variants = productWithVariants();
    await fixture.componentInstance.loadStockRows();

    fixture.componentInstance.changeTableSort({ key: 'color', direction: 'descend' });
    await fixture.whenStable();

    expect(productLookup.lastFilters?.sortBy).toBe('color');
    expect(productLookup.lastFilters?.sortDirection).toBe('descend');
  });

  it('marks only the column being sorted', async () => {
    fixture.componentInstance.changeTableSort({ key: 'size', direction: 'ascend' });

    expect(fixture.componentInstance.tableSort()).toEqual({ key: 'size', direction: 'ascend' });
  });

  it('sorts variants missing a colour or size without dropping them', async () => {
    productLookup.variants = productWithUnsetColour();
    await fixture.componentInstance.loadStockRows();

    fixture.componentInstance.changeTableSort({ key: 'color', direction: 'ascend' });

    expect(fixture.componentInstance.filteredRows().length).toBe(2);
  });

  it('lists a variant whose product is still a draft', async () => {
    productLookup.variants = [draftProductWithVariant()];

    await fixture.componentInstance.loadStockRows();

    expect(fixture.componentInstance.stockRows().map(row => row.variant.sku)).toEqual(['PRD02-BLK-M']);
  });

  it('uses one inventory batch request for the loaded variant page', async () => {
    productLookup.variants = productWithVariants();
    inventoryApi.resetCalls();

    await fixture.componentInstance.loadStockRows();

    expect(inventoryApi.batchCalls.length).toBe(1);
    expect(inventoryApi.batchCalls[0]).toEqual(['v-1', 'v-2', 'v-3']);
    expect(inventoryApi.getByVariantCalls).toBe(0);
  });

  it('merges inventory by product variant id and defaults missing rows to zero', async () => {
    productLookup.variants = productWithVariants();
    inventoryApi.items = [
      { productId: 'v-2', sku: 'PRD01-BLK-M', available: 8, reserved: 2, version: 3 }
    ];

    await fixture.componentInstance.loadStockRows();

    const rows = fixture.componentInstance.stockRows();
    expect(rows.find(row => row.variant.id === 'v-2')?.inventory?.available).toBe(8);
    expect(rows.find(row => row.variant.id === 'v-1')?.inventory?.available).toBe(0);
  });

  it('applies the in-stock deep-link filter during initialization', async () => {
    await createComponent({ stock: 'in-stock' });

    expect(fixture.componentInstance.stockStateFilter).toBe('available');
  });

  it('applies the low-stock deep-link filter during initialization', async () => {
    await createComponent({ stock: 'low' });

    expect(fixture.componentInstance.stockStateFilter).toBe('low');
  });

  it('applies the out-of-stock deep-link filter during initialization', async () => {
    await createComponent({ stock: 'out' });

    expect(fixture.componentInstance.stockStateFilter).toBe('out');
  });

  async function createComponent(queryParams: Record<string, string> = {}): Promise<void> {
    fixture?.destroy();
    routeStub.snapshot.queryParamMap = convertToParamMap(queryParams);
    fixture = TestBed.createComponent(InventoryListPageComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  }
});

function route(queryParams: Record<string, string>) {
  return {
    snapshot: {
      queryParamMap: convertToParamMap(queryParams)
    }
  };
}

class FakeProductLookupApiService {
  lastFilters: { sortBy?: string; sortDirection?: string; page?: number; pageSize?: number } | null = null;
  variants: ProductVariantPageResponse[] = [];
  total: number | null = null;
  searchCalls = 0;
  searchVariantsCalls = 0;

  search(): Promise<never> {
    this.searchCalls++;
    return Promise.reject(new Error('Inventory page must not use product lookup search.'));
  }

  searchVariants(filters: { sortBy?: string; sortDirection?: string; page?: number; pageSize?: number } = {}):
    Promise<PagedResult<ProductVariantPageResponse>> {
    this.searchVariantsCalls++;
    this.lastFilters = filters;
    return Promise.resolve({
      items: this.variants,
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20,
      total: this.total ?? this.variants.length
    });
  }
}

class FakeInventoryApiService {
  items: InventoryResponse[] = [];
  batchCalls: string[][] = [];
  getByVariantCalls = 0;

  getByVariant(): Promise<null> {
    this.getByVariantCalls++;
    return Promise.resolve(null);
  }

  getByVariants(productVariantIds: string[]): Promise<{ items: InventoryResponse[] }> {
    this.batchCalls.push(productVariantIds);
    return Promise.resolve({ items: this.items });
  }

  resetCalls(): void {
    this.batchCalls = [];
    this.getByVariantCalls = 0;
  }
}

function productWithVariants(): ProductVariantPageResponse[] {
  return [
    variant('v-1', 'PRD01-WHT-M', 'Shirt', 'WHT'),
    variant('v-2', 'PRD01-BLK-M', 'Shirt', 'BLK'),
    variant('v-3', 'PRD01-RED-M', 'Shirt', 'RED')
  ];
}

function productWithUnsetColour(): ProductVariantPageResponse[] {
  return [
    variant('v-4', 'PRD03-BLK-M', 'Cap', 'BLK'),
    { ...variant('v-5', 'PRD03-ONE', 'Cap', 'BLK'), color: null }
  ];
}

function colour(code: string, name: string) {
  return { id: `colour-${code}`, code, name, hexCode: '#000000' };
}

function draftProductWithVariant(): ProductVariantPageResponse {
  return {
    productId: '88888888-8888-8888-8888-888888888888',
    productCode: 'PRD02',
    productName: 'Clothing',
    productStatus: 'Draft',
    productVariantId: '99999999-9999-9999-9999-999999999999',
    sku: 'PRD02-BLK-M',
    price: 100,
    variantStatus: 'Published'
  };
}

function variant(id: string, sku: string, productName: string, colorCode: string): ProductVariantPageResponse {
  return {
    productId: '77777777-7777-7777-7777-777777777777',
    productCode: sku.split('-')[0],
    productName,
    productStatus: 'Published',
    productVariantId: id,
    sku,
    color: colour(colorCode, colorCode),
    size: null,
    price: 100,
    variantStatus: 'Published'
  };
}

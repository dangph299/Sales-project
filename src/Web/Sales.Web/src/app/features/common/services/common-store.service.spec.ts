import { TestBed, fakeAsync, flushMicrotasks } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { SessionService } from '../../../core/auth/session.service';
import { CommonStore } from './common-store.service';

/**
 * These identifiers are deliberately not the seeded database GUIDs, and the lists are deliberately
 * not in seeded order. If any frontend code still resolved common lookup data by a hardcoded id or by
 * array position, these expectations would fail.
 */
const colors = [
  { id: 'color-id-1', code: 'BLK', name: 'Black', hexCode: '#000000' },
  { id: 'color-id-2', code: 'RED', name: 'Red', hexCode: '#FF0000' }
];

const sizes = [
  { id: 'size-id-1', code: 'S', name: 'Small', sortOrder: 30 },
  { id: 'size-id-2', code: 'M', name: 'Medium', sortOrder: 40 }
];

const categories = [
  { id: 'category-id-1', categoryCode: 'CAT009', name: 'Shirts', sortOrder: 1, status: 'Published' },
  { id: 'category-id-2', categoryCode: 'CAT001', name: 'Uncategorized', sortOrder: 0, status: 'Published' }
];

describe('CommonStore', () => {
  let store: CommonStore;
  let httpMock: HttpTestingController;
  let salesBase: string;

  beforeEach(async () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    salesBase = TestBed.inject(ApiEndpointConfigurationService).salesBase();
    store = TestBed.inject(CommonStore);
    httpMock = TestBed.inject(HttpTestingController);

    const loaded = store.ensureLoaded();
    flushLookup('/api/common/colors', colors);
    flushLookup('/api/common/sizes', sizes);
    flushLookup('/api/categories', categories);
    await loaded;
  });

  afterEach(() => httpMock.verify());

  function flushLookup(path: string, data: unknown): void {
    httpMock.expectOne(`${salesBase}${path}`).flush(JSON.stringify({ success: true, data }));
  }

  it('loads every lookup from the backend', () => {
    expect(store.colors().length).toBe(2);
    expect(store.sizes().length).toBe(2);
    expect(store.categories().length).toBe(2);
  });

  it('resolves the default size by code and returns the backend id', () => {
    expect(store.defaultSizeId()).toBe('size-id-2');
  });

  it('resolves the default category by code rather than by position', () => {
    expect(store.defaultCategoryId()).toBe('category-id-2');
  });

  it('matches reference items on code', () => {
    expect(store.colorByCode('RED')?.id).toBe('color-id-2');
    expect(store.sizeByCode('S')?.id).toBe('size-id-1');
    expect(store.categoryByCode('CAT001')?.name).toBe('Uncategorized');
  });

  it('returns null for a code the backend did not publish', () => {
    expect(store.colorByCode('PURPLE')).toBeNull();
  });

  it('labels category options as CODE - Name for the picker', () => {
    expect(store.categoryOptions()[0].label).toBe('CAT009 - Shirts');
  });

  it('loads once and shares the result across callers', async () => {
    await store.ensureLoaded();

    httpMock.verify();
  });
});

describe('CommonStore authentication retry', () => {
  let store: CommonStore;
  let httpMock: HttpTestingController;
  let salesBase: string;

  beforeEach(() => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    salesBase = TestBed.inject(ApiEndpointConfigurationService).salesBase();
    store = TestBed.inject(CommonStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loads lookups when a user logs in after the store has been created', fakeAsync(() => {
    httpMock.expectNone(`${salesBase}/api/common/colors`);

    TestBed.inject(SessionService).setTokens({
      accessToken: 'access-token',
      expiresIn: 3600,
      refreshToken: 'refresh-token'
    });

    TestBed.flushEffects();
    flushMicrotasks();

    httpMock.expectOne(`${salesBase}/api/common/colors`).flush(JSON.stringify({ success: true, data: colors }));
    httpMock.expectOne(`${salesBase}/api/common/sizes`).flush(JSON.stringify({ success: true, data: sizes }));
    httpMock.expectOne(`${salesBase}/api/categories`).flush(JSON.stringify({ success: true, data: categories }));

    flushMicrotasks();

    expect(store.colors().length).toBe(2);
    expect(store.sizes().length).toBe(2);
    expect(store.categories().length).toBe(2);
  }));
});

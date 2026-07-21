import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { SessionService } from '../../../core/auth/session.service';
import { describeApiError } from '../../../shared/utilities/describe-api-error';
import { CommonApiService } from '../api/common-api.service';
import { CategoryCodes } from '../constants/category-codes';
import { SizeCodes } from '../constants/size-codes';
import { CategoryLookupResponse } from '../contracts/category-lookup.response';
import { ColorResponse } from '../contracts/color.response';
import { SizeLookupResponse } from '../contracts/size-lookup.response';

/**
 * Application-lifetime cache of backend common lookup data.
 *
 * Common lookup data is seeded and effectively immutable, so it is fetched once and shared. Callers
 * match on `code` for business decisions and submit the `id` carried alongside it, which is why no
 * seeded identifier appears anywhere in the frontend.
 */
@Injectable({ providedIn: 'root' })
export class CommonStore {
  private readonly commonApi = inject(CommonApiService);
  private readonly session = inject(SessionService);
  private inFlight: Promise<void> | null = null;

  readonly colors = signal<ColorResponse[]>([]);
  readonly sizes = signal<SizeLookupResponse[]>([]);
  readonly categories = signal<CategoryLookupResponse[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal('');

  constructor() {
    effect(() => {
      if (this.session.isAuthenticated() && this.needsLoad()) {
        queueMicrotask(() => void this.ensureLoaded());
      }
    });
  }

  /** Category options ordered for a picker, labelled `CODE - Name`. */
  readonly categoryOptions = computed(() => this.categories().map(category => ({
    id: category.id,
    code: category.categoryCode,
    label: `${category.categoryCode} - ${category.name}`
  })));

  /**
   * Loads common lookup data once. Concurrent callers share a single request, and a failed load can be
   * retried by calling again.
   */
  ensureLoaded(): Promise<void> {
    if (this.inFlight) {
      return this.inFlight;
    }

    this.inFlight = this.load().finally(() => {
      if (this.loadError()) {
        this.inFlight = null;
      }
    });

    return this.inFlight;
  }

  /** Forces a refetch, used after common lookup data is known to have changed. */
  reload(): Promise<void> {
    this.inFlight = null;
    return this.ensureLoaded();
  }

  colorByCode(code: string): ColorResponse | null {
    return this.colors().find(color => color.code === code) ?? null;
  }

  sizeByCode(code: string): SizeLookupResponse | null {
    return this.sizes().find(size => size.code === code) ?? null;
  }

  categoryByCode(categoryCode: string): CategoryLookupResponse | null {
    return this.categories().find(category => category.categoryCode === categoryCode) ?? null;
  }

  /**
   * Backend id of the size a new variant starts on, falling back to the first loaded size so the
   * form still works if the seeded default is ever renamed.
   */
  defaultSizeId(): string {
    return this.sizeByCode(SizeCodes.Medium)?.id ?? this.sizes()[0]?.id ?? '';
  }

  /** Backend id of the first color, which is a presentation default rather than a business rule. */
  defaultColorId(): string {
    return this.colors()[0]?.id ?? '';
  }

  /**
   * Backend id of the category a new product defaults to, falling back to the first loaded
   * category.
   */
  defaultCategoryId(): string {
    return this.categoryByCode(CategoryCodes.Uncategorized)?.id ?? this.categories()[0]?.id ?? '';
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set('');
    try {
      const [colors, sizes, categories] = await Promise.all([
        this.commonApi.listColors(),
        this.commonApi.listSizes(),
        this.commonApi.listCategories()
      ]);

      this.colors.set(colors);
      this.sizes.set(sizes);
      this.categories.set(categories);
    } catch (error) {
      this.loadError.set(describeApiError(error));
    } finally {
      this.loading.set(false);
    }
  }

  private needsLoad(): boolean {
    return !this.inFlight
      && (this.colors().length === 0 || this.sizes().length === 0 || this.categories().length === 0);
  }
}

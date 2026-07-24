import { Signal, signal } from '@angular/core';
import { ListQuerySort, ListQueryState } from '../models/list-query-state.model';

/**
 * Small composition helper for list pages: holds page/sort/filter state and
 * guards against request races (a stale response never overwrites a newer
 * one). Not a base class - each list page owns an instance as a plain field
 * and calls `run()` with its own API call.
 */
export class ListQueryController<TFilter> {
  private readonly _state = signal<ListQueryState<TFilter>>(this.initialState);
  private readonly _loading = signal(false);
  private sequence = 0;

  readonly state: Signal<ListQueryState<TFilter>> = this._state.asReadonly();
  readonly loading: Signal<boolean> = this._loading.asReadonly();

  constructor(private readonly initialState: ListQueryState<TFilter>) {}

  setFilters(filters: TFilter): void {
    this._state.update(current => ({ ...current, filters, pageIndex: 1 }));
  }

  setSort(sort: ListQuerySort | undefined): void {
    this._state.update(current => ({ ...current, sort }));
  }

  setPage(pageIndex: number, pageSize?: number): void {
    this._state.update(current => ({
      ...current,
      pageIndex,
      pageSize: pageSize ?? current.pageSize
    }));
  }

  reset(): void {
    this._state.set(this.initialState);
  }

  /**
   * Runs `loader` against the current state. If a newer call starts before
   * this one resolves, this call's result is discarded and `undefined` is
   * returned, so the caller must skip applying it on `undefined`.
   */
  async run<T>(loader: (state: ListQueryState<TFilter>) => Promise<T>): Promise<T | undefined> {
    const token = ++this.sequence;
    this._loading.set(true);
    try {
      const result = await loader(this._state());
      return token === this.sequence ? result : undefined;
    } finally {
      if (token === this.sequence) {
        this._loading.set(false);
      }
    }
  }
}

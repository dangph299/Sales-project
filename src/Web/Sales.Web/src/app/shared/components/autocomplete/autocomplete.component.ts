import { CommonModule } from '@angular/common';
import { Component, ContentChild, ElementRef, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild, forwardRef, signal } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { NzAutocompleteModule, NzOptionSelectionChange } from 'ng-zorro-antd/auto-complete';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { Observable, Subject, catchError, debounceTime, distinctUntilChanged, finalize, from, of, switchMap, takeUntil } from 'rxjs';
import { AutocompleteOptionTemplateDirective } from './autocomplete-option-template.directive';

@Component({
  selector: 'app-autocomplete',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    AutocompleteOptionTemplateDirective,
    NzAutocompleteModule,
    NzButtonModule,
    NzFormModule,
    NzInputModule
  ],
  templateUrl: './autocomplete.component.html',
  styleUrl: './autocomplete.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => AutocompleteComponent),
      multi: true
    }
  ]
})
export class AutocompleteComponent<T> implements ControlValueAccessor, OnInit, OnDestroy {
  @Input() label = '';
  @Input() name = '';
  @Input() placeholder = '';
  @Input() required = false;
  @Input() disabled = false;
  @Input() readonly = false;
  @Input() loading = false;
  @Input() errorMessage = '';
  @Input() presentation: 'form' | 'control' = 'form';
  @Input() clearable = false;
  @Input() showStatusHint = true;
  @Input() minSearchLength = 1;
  @Input() debounceMs = 300;
  @Input() options: T[] = [];
  @Input() searchFn?: (term: string) => Promise<T[]> | Observable<T[]>;
  @Input() displayWith: (value: T | string | null) => string = value => value === null ? '' : String(value);
  @Input() optionKey: (option: T) => string = option => String(option);
  @Input() requireSelection = false;
  @Input() valueMode: 'option' | 'text' = 'option';

  @Output() searchChanged = new EventEmitter<string>();
  @Output() selected = new EventEmitter<T>();
  @Output() cleared = new EventEmitter<void>();
  @Output() searchingChanged = new EventEmitter<boolean>();

  @ContentChild(AutocompleteOptionTemplateDirective) readonly optionTemplate?: AutocompleteOptionTemplateDirective<T>;
  @ViewChild('textInput') private readonly textInput?: ElementRef<HTMLInputElement>;

  readonly remoteOptions = signal<T[]>([]);
  readonly searching = signal(false);
  readonly loadErrorMessage = signal('');

  inputText = '';
  selectedOption: T | null = null;

  private readonly searchTerms = new Subject<string>();
  private readonly destroyed = new Subject<void>();
  private onChange: (value: T | string | null) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  ngOnInit(): void {
    this.searchTerms
      .pipe(
        debounceTime(this.debounceMs),
        distinctUntilChanged(),
        switchMap(term => this.search(term)),
        takeUntil(this.destroyed))
      .subscribe(options => this.remoteOptions.set(options));
  }

  ngOnDestroy(): void {
    this.destroyed.next();
    this.destroyed.complete();
  }

  writeValue(value: T | string | null): void {
    this.selectedOption = typeof value === 'string' || value === null ? null : value;
    this.inputText = this.displayWith(value);
  }

  registerOnChange(fn: (value: T | string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  get visibleOptions(): T[] {
    return this.searchFn ? this.remoteOptions() : this.options;
  }

  get invalidSelection(): boolean {
    return this.requireSelection && this.inputText.trim().length > 0 && this.selectedOption === null;
  }

  changeText(term: string): void {
    if (this.readonly) {
      return;
    }

    this.inputText = term;
    this.selectedOption = null;
    this.searchChanged.emit(term);
    this.onChange(this.requireSelection ? null : term);
    this.searchTerms.next(term);
  }

  selectOption(option: T, selection: NzOptionSelectionChange): void {
    if (!selection.isUserInput) {
      return;
    }

    this.selectedOption = option;
    this.inputText = this.displayWith(option);
    this.onChange(this.valueMode === 'text' ? this.inputText : option);
    this.selected.emit(option);
  }

  clear(): void {
    this.inputText = '';
    this.selectedOption = null;
    this.remoteOptions.set([]);
    this.onChange(null);
    this.cleared.emit();
  }

  markTouched(): void {
    this.onTouched();
  }

  focus(options: FocusOptions = { preventScroll: true }): void {
    this.textInput?.nativeElement.focus(options);
  }

  private search(term: string): Observable<T[]> {
    const normalizedTerm = term.trim();
    if (normalizedTerm.length < this.minSearchLength) {
      this.setSearching(false);
      this.loadErrorMessage.set('');
      return of([]);
    }

    if (!this.searchFn) {
      return of(this.options);
    }

    this.setSearching(true);
    this.loadErrorMessage.set('');
    const result = this.searchFn(normalizedTerm);
    const source = result instanceof Observable ? result : from(result);
    return source.pipe(
      catchError(() => {
        this.loadErrorMessage.set('Could not load results.');
        return of([]);
      }),
      finalize(() => this.setSearching(false)));
  }

  private setSearching(isSearching: boolean): void {
    if (this.searching() === isSearching) {
      return;
    }

    this.searching.set(isSearching);
    this.searchingChanged.emit(isSearching);
  }
}

import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnDestroy, OnInit, Output, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { Subject, debounceTime, distinctUntilChanged, switchMap, takeUntil } from 'rxjs';
import { CustomerLookupApiService } from '../../../common/api/customer-lookup-api.service';
import { CustomerPhoneSuggestionResponse } from '../../../common/contracts/customer-lookup.response';
import { OrderCustomerRequest } from '../../api/requests/order-customer.request';

/** How long to wait after the last keystroke before asking the server for suggestions. */
const suggestionDebounceMs = 300;

/** Validation messages shown under the fields, keyed by field. */
export interface OrderCustomerFormErrors {
  name?: string;
  phone?: string;
}

/**
 * The customer section of the order form, used by both create and edit.
 *
 * The phone field suggests existing customers as the user types, but the
 * suggestions are only a convenience: whatever ends up in these fields is what
 * gets saved. On create the backend resolves or creates the customer from the
 * phone number, so an unrecognised number is a new customer rather than an
 * error. On edit these values are the order's own snapshot, and saving them
 * never writes back to the customer record.
 */
@Component({
  selector: 'app-order-customer-form',
  standalone: true,
  imports: [CommonModule, FormsModule, NzInputModule, NzSelectModule],
  templateUrl: './order-customer-form.component.html',
  styleUrl: './order-customer-form.component.scss'
})
export class OrderCustomerFormComponent implements OnInit, OnDestroy {
  private readonly customerLookup = inject(CustomerLookupApiService);

  @Input({ required: true }) customer!: OrderCustomerRequest;
  @Input() errors: OrderCustomerFormErrors = {};
  @Input() disabled = false;

  /** True while creating, when an unmatched phone number means a customer will be created. */
  @Input() showNewCustomerHint = true;

  @Output() customerChange = new EventEmitter<OrderCustomerRequest>();

  readonly suggestions = signal<CustomerPhoneSuggestionResponse[]>([]);
  readonly searching = signal(false);
  readonly suggestionErrorMessage = signal('');

  private readonly phoneSearchTerms = new Subject<string>();
  private readonly destroyed = new Subject<void>();

  ngOnInit(): void {
    this.phoneSearchTerms
      .pipe(
        debounceTime(suggestionDebounceMs),
        distinctUntilChanged(),
        switchMap(customerPhoneSearchTerm => this.loadSuggestions(customerPhoneSearchTerm)),
        takeUntil(this.destroyed))
      .subscribe(suggestions => {
        this.suggestions.set(suggestions);
        this.searching.set(false);
      });
  }

  ngOnDestroy(): void {
    this.destroyed.next();
    this.destroyed.complete();
  }

  /**
   * True when the user has typed a usable phone number that matched no existing
   * customer. Deliberately not an error: the backend will create the customer.
   */
  isNewCustomer(): boolean {
    if (!this.showNewCustomerHint || this.searching()) {
      return false;
    }

    const normalizedCustomerPhone = this.digitsOf(this.customer.phone);
    if (normalizedCustomerPhone.length < 9) {
      return false;
    }

    return !this.suggestions().some(suggestion => this.digitsOf(suggestion.phone) === normalizedCustomerPhone);
  }

  searchByPhone(customerPhoneSearchTerm: string): void {
    this.searching.set(true);
    this.phoneSearchTerms.next(customerPhoneSearchTerm);
  }

  /** Typing in the phone box both edits the snapshot and drives the suggestions. */
  changePhone(customerPhone: string): void {
    this.emitChange({ ...this.customer, phone: customerPhone });
    this.searchByPhone(customerPhone);
  }

  changeName(customerName: string): void {
    this.emitChange({ ...this.customer, name: customerName });
  }

  changeEmail(customerEmail: string): void {
    this.emitChange({ ...this.customer, email: customerEmail });
  }

  changeAddress(customerAddress: string): void {
    this.emitChange({ ...this.customer, address: customerAddress });
  }

  /**
   * Fills the form from a suggestion. Every field stays editable afterwards —
   * picking a customer is a shortcut, not a commitment to their current details.
   */
  selectSuggestion(customerId: string): void {
    const suggestion = this.suggestions().find(x => x.customerId === customerId);
    if (!suggestion) {
      return;
    }

    this.emitChange({
      name: suggestion.name,
      phone: suggestion.phone,
      email: suggestion.email ?? '',
      address: suggestion.address ?? ''
    });
  }

  private async loadSuggestions(customerPhoneSearchTerm: string): Promise<CustomerPhoneSuggestionResponse[]> {
    if (this.digitsOf(customerPhoneSearchTerm).length === 0) {
      this.suggestionErrorMessage.set('');
      return [];
    }

    try {
      const suggestions = await this.customerLookup.suggestByPhone(customerPhoneSearchTerm);
      this.suggestionErrorMessage.set('');
      return suggestions;
    } catch {
      // A failed lookup must not block the form: the user can still type the
      // details in full and let the backend sort the customer out on submit.
      this.suggestionErrorMessage.set('Could not load customer suggestions.');
      return [];
    }
  }

  private emitChange(customer: OrderCustomerRequest): void {
    this.customer = customer;
    this.customerChange.emit(customer);
  }

  private digitsOf(customerPhone: string | null | undefined): string {
    return (customerPhone ?? '').replace(/\D/g, '');
  }
}

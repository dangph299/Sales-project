import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { AutocompleteComponent } from '../../../../shared/components/autocomplete/autocomplete.component';
import { CustomerLookupApiService } from '../../../common/api/customer-lookup-api.service';
import { CustomerPhoneSuggestionResponse } from '../../../common/contracts/customer-lookup.response';
import { OrderCustomerRequest } from '../../api/requests/order-customer.request';

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
 *
 * The phone box is therefore a plain text input with an autocomplete overlay,
 * not a picker. A picker would only accept phone numbers that already exist,
 * which is exactly the case this form has to support, and it would hold a
 * customer id as its value while the field is supposed to hold a phone number.
 */
@Component({
  selector: 'app-order-customer-form',
  standalone: true,
  imports: [CommonModule, FormsModule, AutocompleteComponent, NzFormModule, NzInputModule],
  templateUrl: './order-customer-form.component.html'
})
export class OrderCustomerFormComponent {
  private readonly customerLookup = inject(CustomerLookupApiService);

  @Input({ required: true }) customer!: OrderCustomerRequest;
  @Input() errors: OrderCustomerFormErrors = {};
  @Input() disabled = false;

  /** True while creating, when an unmatched phone number means a customer will be created. */
  @Input() showNewCustomerHint = true;

  @Output() customerChange = new EventEmitter<OrderCustomerRequest>();

  readonly suggestions = signal<CustomerPhoneSuggestionResponse[]>([]);
  readonly suggestionErrorMessage = signal('');
  readonly searching = signal(false);

  readonly phoneSuggestionSearch = (term: string): Promise<CustomerPhoneSuggestionResponse[]> => this.loadSuggestions(term);
  readonly displayPhoneSuggestion = (suggestion: CustomerPhoneSuggestionResponse | string | null): string => {
    if (!suggestion) {
      return '';
    }

    return typeof suggestion === 'string' ? suggestion : suggestion.phone;
  };

  /**
   * True when the user has typed a usable phone number that matched no existing
   * customer. Deliberately not an error: the backend will create the customer.
   */
  isNewCustomer(): boolean {
    if (!this.showNewCustomerHint) {
      return false;
    }

    const normalizedCustomerPhone = this.digitsOf(this.customer.phone);
    if (normalizedCustomerPhone.length < 9) {
      return false;
    }

    return !this.suggestions().some(suggestion => this.digitsOf(suggestion.phone) === normalizedCustomerPhone);
  }

  /** Typing in the phone box both edits the snapshot and drives the suggestions. */
  changePhone(customerPhone: string): void {
    if (!customerPhone.trim()) {
      this.suggestions.set([]);
      this.suggestionErrorMessage.set('');
    }

    this.emitChange({ ...this.customer, phone: customerPhone });
  }

  changePhoneValue(value: string | CustomerPhoneSuggestionResponse | null): void {
    this.changePhone(typeof value === 'string' ? value : value?.phone ?? '');
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
   * Fills the form from a suggestion the user picked. Every field stays editable
   * afterwards — picking a customer is a shortcut, not a commitment to their
   * current details.
   */
  selectSuggestion(suggestion: CustomerPhoneSuggestionResponse): void {
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
      this.suggestions.set(suggestions);
      this.suggestionErrorMessage.set('');
      return suggestions;
    } catch {
      // A failed lookup must not block the form: the user can still type the
      // details in full and let the backend sort the customer out on submit.
      this.suggestionErrorMessage.set('Could not load customer suggestions.');
      this.suggestions.set([]);
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

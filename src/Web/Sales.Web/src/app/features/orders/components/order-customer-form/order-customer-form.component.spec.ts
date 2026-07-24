import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { CustomerLookupApiService } from '../../../common/api/customer-lookup-api.service';
import { CustomerPhoneSuggestionResponse } from '../../../common/contracts/customer-lookup.response';
import { OrderCustomerRequest } from '../../api/requests/order-customer.request';
import { OrderCustomerFormComponent } from './order-customer-form.component';

// The phone box was a picker before, which broke in two ways a class-level test
// could not see: it held customer ids while the model held a phone number, so a
// picked customer left the box blank, and it discarded anything the user typed
// that was not already an option, so a new customer could not be entered at all.
// These go through the rendered input for that reason.
describe('OrderCustomerFormComponent phone entry', () => {
  let fixture: ComponentFixture<OrderCustomerFormComponent>;
  let component: OrderCustomerFormComponent;
  let lookup: FakeCustomerLookupApiService;

  beforeEach(async () => {
    lookup = new FakeCustomerLookupApiService();

    await TestBed.configureTestingModule({
      imports: [OrderCustomerFormComponent],
      providers: [
        provideNoopAnimations(),
        { provide: CustomerLookupApiService, useValue: lookup }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(OrderCustomerFormComponent);
    component = fixture.componentInstance;
    component.customer = { name: '', phone: '', email: '', address: '' };
    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => {
    fixture.destroy();
  });

  it('keeps a phone number that matches no existing customer', async () => {
    const emitted: OrderCustomerRequest[] = [];
    component.customerChange.subscribe(customer => emitted.push(customer));

    typePhone('0987654321');
    await fixture.whenStable();

    expect(emitted.at(-1)?.phone).toBe('0987654321');
    expect(phoneInput().value).toBe('0987654321');
  });

  it('fills every field from a picked suggestion, phone included', async () => {
    const emitted: OrderCustomerRequest[] = [];
    component.customerChange.subscribe(customer => emitted.push(customer));

    component.selectSuggestion(suggestion());
    fixture.detectChanges();
    // NgModel writes the new value to the input on the next microtask.
    await fixture.whenStable();

    expect(emitted.at(-1)).toEqual({
      name: 'Nguyen Van A',
      phone: '0901234567',
      email: 'a@example.com',
      address: '12 Le Loi'
    });
    expect(component.customer.phone).toBe('0901234567');
  });

  it('does not treat a picked customer as new', async () => {
    lookup.suggestions = [suggestion()];
    component.selectSuggestion(suggestion());
    typePhone('0901234567');
    await settleDebounce();

    expect(component.isNewCustomer()).toBeFalse();
  });

  it('offers to create a customer for a complete unmatched number', async () => {
    lookup.suggestions = [];
    typePhone('0987654321');
    await settleDebounce();

    expect(component.isNewCustomer()).toBeTrue();
  });

  it('recovers customer state when the same number is re-entered after clearing', async () => {
    lookup.suggestions = [suggestion()]; // 0901234567

    typePhone('0901234567');
    await settleDebounce();
    expect(component.isNewCustomer()).toBeFalse();

    typePhone('');
    await settleDebounce();

    typePhone('0901234567');
    await settleDebounce();

    expect(component.isNewCustomer()).toBeFalse();
  });

  it('does not treat repeated matched input as a new customer', async () => {
    lookup.suggestions = [suggestion()];

    typePhone('0901234567');
    await settleDebounce();

    typePhone('0901234567');
    await settleDebounce();

    expect(component.isNewCustomer()).toBeFalse();
  });

  it('resets loading when the lookup fails', async () => {
    lookup.shouldError = true;

    typePhone('0987654321');
    await settleDebounce();

    expect(component.suggestionErrorMessage()).not.toBe('');
  });

  it('clears suggestions when the field is emptied', async () => {
    lookup.suggestions = [suggestion()];
    typePhone('0901234567');
    await settleDebounce();
    expect(component.suggestions().length).toBe(1);

    typePhone('');
    await settleDebounce();

    expect(component.suggestions()).toEqual([]);
  });

  function phoneInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input[name="orderCustomerPhone"]') as HTMLInputElement;
  }

  function typePhone(customerPhone: string): void {
    const input = phoneInput();
    input.value = customerPhone;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  }

  async function settleDebounce(): Promise<void> {
    await new Promise(resolve => setTimeout(resolve, 400));
    await fixture.whenStable();
    fixture.detectChanges();
  }
});

function suggestion(): CustomerPhoneSuggestionResponse {
  return {
    customerId: '11111111-1111-1111-1111-111111111111',
    phone: '0901234567',
    name: 'Nguyen Van A',
    email: 'a@example.com',
    address: '12 Le Loi'
  };
}

class FakeCustomerLookupApiService {
  suggestions: CustomerPhoneSuggestionResponse[] = [];
  shouldError = false;
  manualResolve = false;

  private resolvers: Array<(value: CustomerPhoneSuggestionResponse[]) => void> = [];

  suggestByPhone(): Promise<CustomerPhoneSuggestionResponse[]> {
    if (this.shouldError) {
      return Promise.reject(new Error('lookup failed'));
    }

    if (this.manualResolve) {
      return new Promise<CustomerPhoneSuggestionResponse[]>(resolve => this.resolvers.push(resolve));
    }

    return Promise.resolve(this.suggestions);
  }

  /** Completes every lookup left pending by {@link manualResolve}. */
  resolveAll(): void {
    const pending = this.resolvers;
    this.resolvers = [];
    pending.forEach(resolve => resolve(this.suggestions));
  }

  search(): Promise<never> {
    return Promise.reject(new Error('The order customer form must not list customers.'));
  }
}

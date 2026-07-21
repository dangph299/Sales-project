import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ApiClientError } from '../../../../core/api/api-client-result';
import { CustomerApiService } from '../../api/customer-api.service';
import { CustomerResponse } from '../../api/responses/customer.response';
import { CustomerListPageComponent } from './customer-list-page.component';

const customers: CustomerResponse[] = [
  {
    id: 'customer-1',
    customerCode: 'CUST-1',
    name: 'Jane Smith',
    phone: '0900000001',
    email: 'jane@example.com',
    address: 'District 1',
    status: 'Normal',
    version: 1,
    updatedAt: '2026-07-20T00:00:00Z',
    createdAt: '2026-07-20T00:00:00Z',
    isDelete: false
  }
];

describe('CustomerListPageComponent modal editor', () => {
  let fixture: ComponentFixture<CustomerListPageComponent>;
  let component: CustomerListPageComponent;
  let customerApi: jasmine.SpyObj<CustomerApiService>;

  beforeEach(async () => {
    customerApi = jasmine.createSpyObj<CustomerApiService>(
      'CustomerApiService',
      ['search', 'create', 'update', 'updateStatus']);
    customerApi.search.and.resolveTo({
      items: customers,
      page: 1,
      pageSize: 20,
      total: customers.length
    });

    await TestBed.configureTestingModule({
      imports: [CustomerListPageComponent],
      providers: [
        provideNoopAnimations(),
        { provide: CustomerApiService, useValue: customerApi }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('opens the shared customer form modal in add mode', () => {
    component.openCreateCustomer();
    fixture.detectChanges();

    expect(component.customerModalOpen()).toBeTrue();
    expect(component.selectedCustomer()).toBeNull();
    expect(component.modalTitle()).toBe('Add Customer');
  });

  it('opens the shared customer form modal in edit mode', () => {
    component.openEditCustomer(customers[0]);
    fixture.detectChanges();

    expect(component.customerModalOpen()).toBeTrue();
    expect(component.selectedCustomer()?.id).toBe('customer-1');
    expect(component.customerForm.name).toBe('Jane Smith');
    expect(component.modalTitle()).toBe('Edit Customer');
  });

  it('keeps the modal open and shows a generic customer conflict for duplicate unique conflicts', async () => {
    customerApi.create.and.rejectWith(new ApiClientError({
      isSuccess: false,
      status: 409,
      statusText: 'Conflict',
      data: null,
      message: 'A resource with the same unique value already exists.',
      errorCode: 'unique_violation',
      correlationId: 'corr-1',
      traceId: 'trace-1',
      errors: [],
      validationErrors: []
    }));

    component.openCreateCustomer();
    component.customerForm = {
      name: 'Jane Smith',
      phone: '0900000001',
      email: '',
      address: ''
    };

    await component.saveCustomer();
    fixture.detectChanges();

    expect(component.customerModalOpen()).toBeTrue();
    expect(component.validationErrors()).toEqual([]);
    expect(component.errorMessage()).toContain('same code or phone');
  });
});

import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzDescriptionsModule } from 'ng-zorro-antd/descriptions';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { NzModalModule, NzModalService } from 'ng-zorro-antd/modal';
import { NzNotificationService } from 'ng-zorro-antd/notification';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTableModule } from 'ng-zorro-antd/table';
import { ApiClientError, ApiResponseReader } from '../../../../core/api/api-client-result';
import { ValidationError } from '../../../../core/api/api-error.model';
import { PageStateComponent } from '../../../../shared/components/page-state/page-state.component';
import { StatusTagComponent } from '../../../../shared/components/status-tag/status-tag.component';
import { CompactTextPipe } from '../../../../shared/pipes/compact-text.pipe';
import { DateTimePipe } from '../../../../shared/pipes/date-time.pipe';
import { confirmAction } from '../../../../shared/utilities/confirm-action';
import { describeApiError } from '../../../../shared/utilities/describe-api-error';
import { CustomerApiService } from '../../api/customer-api.service';
import { CustomerResponse } from '../../api/responses/customer.response';
import { CustomerFormComponent } from '../../components/customer-form/customer-form.component';
import { CustomerStatus, customerStatusDisplay } from '../../constants/customer-status';
import { PhoneMatch } from '../../enums/phone-match.enum';
import { CustomerFormModel, emptyCustomerForm } from '../../models/customer-form.model';

type CustomerSortKey = 'customerCode' | 'name' | 'phone' | 'status' | 'address' | 'updatedAt';
type SortDirection = 'ascend' | 'descend' | null;

@Component({
  selector: 'app-customer-list-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PageStateComponent,
    StatusTagComponent,
    CustomerFormComponent,
    CompactTextPipe,
    DateTimePipe,
    NzButtonModule,
    NzCardModule,
    NzDescriptionsModule,
    NzDropDownModule,
    NzInputModule,
    NzMenuModule,
    NzModalModule,
    NzSelectModule,
    NzTableModule
  ],
  templateUrl: './customer-list-page.component.html',
  styleUrl: './customer-list-page.component.scss'
})
export class CustomerListPageComponent implements OnInit {
  private readonly customerApi = inject(CustomerApiService);
  private readonly modal = inject(NzModalService);
  private readonly notification = inject(NzNotificationService);

  readonly phoneMatchPrefix = PhoneMatch.Prefix;
  readonly phoneMatchSuffix = PhoneMatch.Suffix;

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly customers = signal<CustomerResponse[]>([]);
  readonly total = signal(0);
  readonly selectedCustomer = signal<CustomerResponse | null>(null);
  readonly validationErrors = signal<ValidationError[]>([]);
  readonly customerModalOpen = signal(false);
  readonly detailModalOpen = signal(false);
  readonly sortKey = signal<CustomerSortKey>('updatedAt');
  readonly sortDirection = signal<SortDirection>('descend');

  searchName = '';
  searchPhone = '';
  phoneMatch = PhoneMatch.Prefix;
  pageIndex = 1;
  pageSize = 20;
  readonly pageSizeOptions = [10, 20, 50];
  customerForm: CustomerFormModel = emptyCustomerForm();

  readonly statusDisplay = customerStatusDisplay;
  readonly modalTitle = computed(() => this.selectedCustomer() ? 'Edit Customer' : 'Add Customer');
  readonly sortedCustomers = computed(() => this.sortCustomers(this.customers()));

  ngOnInit(): void {
    void this.loadCustomers();
  }

  async loadCustomers(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');
    try {
      const page = await this.customerApi.search({
        name: this.searchName,
        phone: this.searchPhone,
        phoneMatch: this.phoneMatch,
        page: this.pageIndex,
        pageSize: this.pageSize
      });
      this.customers.set(page.items);
      this.total.set(page.total);
    } catch (error) {
      this.notifyError('Load Customers Failed', error);
    } finally {
      this.loading.set(false);
    }
  }

  selectCustomer(customer: CustomerResponse): void {
    this.selectedCustomer.set(customer);
    this.detailModalOpen.set(true);
  }

  openCreateCustomer(): void {
    this.selectedCustomer.set(null);
    this.detailModalOpen.set(false);
    this.customerForm = emptyCustomerForm();
    this.validationErrors.set([]);
    this.errorMessage.set('');
    this.customerModalOpen.set(true);
  }

  openEditCustomer(customer: CustomerResponse): void {
    this.selectedCustomer.set(customer);
    this.customerForm = {
      name: customer.name,
      phone: customer.phone,
      email: customer.email || '',
      address: customer.address || ''
    };
    this.validationErrors.set([]);
    this.errorMessage.set('');
    this.customerModalOpen.set(true);
  }

  closeCustomerModal(): void {
    this.customerModalOpen.set(false);
    this.customerForm = emptyCustomerForm();
    this.validationErrors.set([]);
    this.errorMessage.set('');
  }

  closeDetailModal(): void {
    this.detailModalOpen.set(false);
  }

  resetFilters(): void {
    this.searchName = '';
    this.searchPhone = '';
    this.phoneMatch = PhoneMatch.Prefix;
    this.pageIndex = 1;
    void this.loadCustomers();
  }

  searchCustomers(): void {
    this.pageIndex = 1;
    void this.loadCustomers();
  }

  changePage(pageIndex: number): void {
    this.pageIndex = pageIndex;
    void this.loadCustomers();
  }

  changePageSize(pageSize: number): void {
    this.pageSize = pageSize;
    this.pageIndex = 1;
    void this.loadCustomers();
  }

  sortBy(key: CustomerSortKey): void {
    if (this.sortKey() !== key) {
      this.sortKey.set(key);
      this.sortDirection.set('ascend');
      return;
    }

    this.sortDirection.set(this.sortDirection() === 'ascend' ? 'descend' : 'ascend');
  }

  sortIndicator(key: CustomerSortKey): string {
    if (this.sortKey() !== key) {
      return '';
    }

    return this.sortDirection() === 'ascend' ? '↑' : '↓';
  }

  async saveCustomer(): Promise<void> {
    if (!this.customerForm.name.trim() || !this.customerForm.phone.trim()) {
      this.validationErrors.set([
        { field: 'Name', message: 'Name is required.' },
        { field: 'Phone', message: 'Phone is required.' }
      ]);
      this.notification.warning('Check Customer Form', 'Name and phone are required.');
      return;
    }

    this.saving.set(true);
    this.validationErrors.set([]);
    try {
      const selected = this.selectedCustomer();
      const request = {
        name: this.customerForm.name.trim(),
        phone: this.customerForm.phone.trim(),
        email: this.customerForm.email.trim() || null,
        address: this.customerForm.address.trim() || null
      };
      const saved = selected
        ? await this.customerApi.update(selected.id, request)
        : await this.customerApi.create(request);
      this.selectCustomer(saved);
      await this.loadCustomers();
      this.customerModalOpen.set(false);
      this.customerForm = emptyCustomerForm();
      this.validationErrors.set([]);
      this.notification.success(selected ? 'Customer Updated' : 'Customer Created', `${saved.customerCode || saved.name} - ${saved.name}`);
    } catch (error) {
      this.handleFormError(error);
    } finally {
      this.saving.set(false);
    }
  }

  async changeStatus(status: CustomerStatus): Promise<void> {
    const customer = this.selectedCustomer();
    await this.changeCustomerStatus(customer, status);
  }

  lifecycleActions(customer: CustomerResponse): CustomerStatus[] {
    return (['Normal', 'Suspended', 'Blocked'] as CustomerStatus[])
      .filter(status => status !== customer.status);
  }

  async changeCustomerStatus(customer: CustomerResponse | null, status: CustomerStatus): Promise<void> {
    if (!customer) {
      return;
    }

    if (status === 'Blocked' && !await confirmAction(
      this.modal,
      'Block Customer',
      'Blocked customers cannot create new orders. This action should be performed by an administrator.')) {
      return;
    }

    this.saving.set(true);
    try {
      const updated = await this.customerApi.updateStatus(customer.id, status);
      this.selectedCustomer.set(updated);
      await this.loadCustomers();
      this.notification.success('Customer Status Updated', `${updated.customerCode || updated.name} is now ${updated.status}.`);
    } catch (error) {
      this.notifyError('Update Customer Status Failed', error);
    } finally {
      this.saving.set(false);
    }
  }

  async deleteCustomer(customer: CustomerResponse): Promise<void> {
    if (!await confirmAction(
      this.modal,
      'Delete Customer',
      `Delete ${customer.name}? This removes the customer from active management.`)) {
      return;
    }

    this.saving.set(true);
    try {
      await this.customerApi.delete(customer.id);
      if (this.selectedCustomer()?.id === customer.id) {
        this.selectedCustomer.set(null);
        this.detailModalOpen.set(false);
      }
      await this.loadCustomers();
      this.notification.success('Customer Deleted', `${customer.customerCode || customer.name} - ${customer.name}`);
    } catch (error) {
      this.notifyError('Delete Customer Failed', error);
    } finally {
      this.saving.set(false);
    }
  }

  private sortCustomers(customers: CustomerResponse[]): CustomerResponse[] {
    const key = this.sortKey();
    const direction = this.sortDirection();
    if (!direction) {
      return customers;
    }

    return [...customers].sort((left, right) => {
      const result = this.sortValue(left, key).localeCompare(this.sortValue(right, key), undefined, {
        numeric: true,
        sensitivity: 'base'
      });

      return direction === 'ascend' ? result : -result;
    });
  }

  private sortValue(customer: CustomerResponse, key: CustomerSortKey): string {
    const value = customer[key];
    return value === null || value === undefined ? '' : String(value);
  }

  private handleFormError(error: unknown): void {
    if (error instanceof ApiClientError) {
      this.validationErrors.set(error.result.validationErrors);
      this.notifyError('Save Customer Failed', this.customerErrorMessage(error));
      return;
    }

    this.notifyError('Save Customer Failed', error);
  }

  private customerErrorMessage(error: ApiClientError): string {
    if (error.result.validationErrors.length > 0) {
      return '';
    }

    if (error.status === 409 && error.result.errorCode === 'unique_violation') {
      return 'A customer with the same code or phone already exists. Refresh the list and try again.';
    }

    return ApiResponseReader.failureMessages(error.result).join(' ');
  }

  private notifyError(title: string, error: unknown): void {
    const message = typeof error === 'string' ? error : describeApiError(error);
    this.notification.error(title, message);
  }
}

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
import { ApiClientError, ApiResponseReader } from '../../../../core/api/api-client-result';
import { ValidationError } from '../../../../core/api/api-error.model';
import { DataTableComponent } from '../../../../shared/components/data-table/data-table.component';
import { TableCellTemplateDirective } from '../../../../shared/components/data-table/table-cell-template.directive';
import { FormDialogComponent } from '../../../../shared/components/form-dialog/form-dialog.component';
import { PageStateComponent } from '../../../../shared/components/page-state/page-state.component';
import { StatusTagComponent } from '../../../../shared/components/status-tag/status-tag.component';
import { CompactTextPipe } from '../../../../shared/pipes/compact-text.pipe';
import { DateTimePipe } from '../../../../shared/pipes/date-time.pipe';
import { TablePageChange, TableSort } from '../../../../shared/models/table.model';
import { FocusFirstRequiredDirective } from '../../../../shared/directives/focus-first-required.directive';
import { ApiNotifierService } from '../../../../shared/services/api-notifier.service';
import { ConfirmDeleteService } from '../../../../shared/services/confirm-delete.service';
import { confirmAction } from '../../../../shared/utilities/confirm-action';
import { ListQueryController } from '../../../../shared/utilities/list-query-controller';
import { CustomerApiService } from '../../api/customer-api.service';
import { CustomerResponse } from '../../api/responses/customer.response';
import { CustomerFormComponent } from '../../components/customer-form/customer-form.component';
import { CustomerStatus, customerStatusDisplay } from '../../constants/customer-status';
import { CustomerFormModel, emptyCustomerForm } from '../../models/customer-form.model';
import { customerListColumns } from './customer-list.columns';

type CustomerSortKey = 'customerCode' | 'name' | 'phone' | 'status' | 'address' | 'updatedAt';
type SortDirection = 'ascend' | 'descend' | null;

@Component({
  selector: 'app-customer-list-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DataTableComponent,
    TableCellTemplateDirective,
    FormDialogComponent,
    PageStateComponent,
    StatusTagComponent,
    CustomerFormComponent,
    CompactTextPipe,
    DateTimePipe,
    FocusFirstRequiredDirective,
    NzButtonModule,
    NzCardModule,
    NzDescriptionsModule,
    NzDropDownModule,
    NzInputModule,
    NzMenuModule,
    NzModalModule
  ],
  templateUrl: './customer-list-page.component.html',
  styleUrl: './customer-list-page.component.scss'
})
export class CustomerListPageComponent implements OnInit {
  private readonly customerApi = inject(CustomerApiService);
  private readonly modal = inject(NzModalService);
  private readonly notification = inject(NzNotificationService);
  private readonly apiNotifier = inject(ApiNotifierService);
  private readonly confirmDelete = inject(ConfirmDeleteService);

  private readonly query = new ListQueryController<{ name: string; phone: string }>({
    pageIndex: 1,
    pageSize: 20,
    filters: { name: '', phone: '' }
  });

  readonly loading = this.query.loading;
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly mutationErrorMessage = signal('');
  readonly customers = signal<CustomerResponse[]>([]);
  readonly total = signal(0);
  readonly selectedCustomer = signal<CustomerResponse | null>(null);
  readonly validationErrors = signal<ValidationError[]>([]);
  readonly customerModalOpen = signal(false);
  readonly customerCreateFocusTrigger = signal(0);
  readonly detailModalOpen = signal(false);
  readonly sortKey = signal<CustomerSortKey>('updatedAt');
  readonly sortDirection = signal<SortDirection>('descend');
  readonly tableColumns = customerListColumns;

  searchName = '';
  searchPhone = '';
  readonly pageSizeOptions = [10, 20, 50];
  customerForm: CustomerFormModel = emptyCustomerForm();

  get pageIndex(): number {
    return this.query.state().pageIndex;
  }

  get pageSize(): number {
    return this.query.state().pageSize;
  }

  readonly statusDisplay = customerStatusDisplay;
  readonly modalTitle = computed(() => this.selectedCustomer() ? 'Edit Customer' : 'Add Customer');
  readonly sortedCustomers = computed(() => this.sortCustomers(this.customers()));
  readonly tableSort = computed<TableSort>(() => ({ key: this.sortKey(), direction: this.sortDirection() }));

  ngOnInit(): void {
    void this.loadCustomers();
  }

  async loadCustomers(): Promise<void> {
    this.errorMessage.set('');
    try {
      const page = await this.query.run(state => this.customerApi.search({
        name: state.filters.name,
        phone: state.filters.phone,
        page: state.pageIndex,
        pageSize: state.pageSize
      }));
      if (!page) {
        return;
      }

      if (page.items.length === 0 && page.total > 0 && this.pageIndex > 1) {
        this.query.setPage(Math.max(1, Math.ceil(page.total / this.pageSize)));
        await this.loadCustomers();
        return;
      }

      this.customers.set(page.items);
      this.total.set(page.total);
    } catch (error) {
      this.notifyError('Load Customers Failed', error, 'page');
    }
  }

  selectCustomer(customer: CustomerResponse): void {
    this.selectedCustomer.set(customer);
    this.detailModalOpen.set(true);
  }

  openCreateCustomer(): void {
    if (this.saving()) {
      return;
    }

    this.selectedCustomer.set(null);
    this.detailModalOpen.set(false);
    this.customerForm = emptyCustomerForm();
    this.validationErrors.set([]);
    this.mutationErrorMessage.set('');
    this.customerModalOpen.set(true);
  }

  openEditCustomer(customer: CustomerResponse): void {
    if (this.saving()) {
      return;
    }

    this.selectedCustomer.set(customer);
    this.customerForm = {
      name: customer.name,
      phone: customer.phone,
      email: customer.email || '',
      address: customer.address || ''
    };
    this.validationErrors.set([]);
    this.mutationErrorMessage.set('');
    this.customerModalOpen.set(true);
  }

  triggerCustomerCreateFocus(): void {
    if (this.customerModalOpen() && !this.selectedCustomer()) {
      this.customerCreateFocusTrigger.update(value => value + 1);
    }
  }

  closeCustomerModal(): void {
    if (this.saving()) {
      return;
    }

    this.customerModalOpen.set(false);
    this.customerForm = emptyCustomerForm();
    this.validationErrors.set([]);
    this.mutationErrorMessage.set('');
  }

  closeDetailModal(): void {
    this.detailModalOpen.set(false);
  }

  resetFilters(): void {
    this.searchName = '';
    this.searchPhone = '';
    this.query.setFilters({ name: '', phone: '' });
    void this.loadCustomers();
  }

  searchCustomers(): void {
    this.query.setFilters({ name: this.searchName, phone: this.searchPhone });
    void this.loadCustomers();
  }

  changeTablePage(page: TablePageChange): void {
    this.query.setPage(page.pageIndex, page.pageSize);
    void this.loadCustomers();
  }

  changeTableSort(sort: TableSort): void {
    this.sortKey.set(sort.key as CustomerSortKey);
    this.sortDirection.set(sort.direction);
  }

  async saveCustomer(): Promise<void> {
    if (this.saving()) {
      return;
    }

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
    this.mutationErrorMessage.set('');
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
      this.mutationErrorMessage.set('');
      this.apiNotifier.success(selected ? 'Customer Updated' : 'Customer Created', `${saved.customerCode || saved.name} - ${saved.name}`);
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
    if (this.saving()) {
      return;
    }

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
      this.apiNotifier.success('Customer Status Updated', `${updated.customerCode || updated.name} is now ${updated.status}.`);
    } catch (error) {
      this.notifyError('Update Customer Status Failed', error, 'mutation');
    } finally {
      this.saving.set(false);
    }
  }

  async deleteCustomer(customer: CustomerResponse): Promise<void> {
    if (this.saving()) {
      return;
    }

    if (!await this.confirmDelete.open({
      title: 'Delete Customer',
      itemName: customer.name,
      warningMessage: 'This removes the customer from active management.'
    })) {
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
      this.apiNotifier.success('Customer Deleted', `${customer.customerCode || customer.name} - ${customer.name}`);
    } catch (error) {
      this.notifyError('Delete Customer Failed', error, 'mutation');
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
      this.notifyError('Save Customer Failed', this.customerErrorMessage(error), 'mutation');
      return;
    }

    this.notifyError('Save Customer Failed', error, 'mutation');
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

  private notifyError(title: string, error: unknown, target: 'page' | 'mutation'): void {
    const message = this.apiNotifier.error(title, error);
    if (target === 'page') {
      this.errorMessage.set(message);
    } else {
      this.mutationErrorMessage.set(message);
    }
  }
}

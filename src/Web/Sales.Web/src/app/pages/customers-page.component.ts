import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiClientError, ApiResponseReader } from '../api-client-result';
import { ApiService } from '../api.service';
import { CustomerDto, ECustomerStatus, PhoneMatch, ValidationError } from '../models';
import { compactText, formatDateTime } from '../shared/display-formatters';
import { PageStateComponent } from '../shared/page-state.component';
import { StatusBadgeComponent } from '../shared/status-badge.component';

interface CustomerForm {
  name: string;
  phone: string;
  email: string;
  address: string;
}

@Component({
  selector: 'app-customers-page',
  standalone: true,
  imports: [CommonModule, FormsModule, PageStateComponent, StatusBadgeComponent],
  template: `
    <section class="page-header">
      <div>
        <p class="eyebrow">Sales</p>
        <h1>Customers</h1>
        <p>Quan ly khach hang va lifecycle Normal, Suspended, Blocked.</p>
      </div>
      <button type="button" (click)="loadCustomers()">Refresh</button>
    </section>

    <section class="toolbar">
      <label>Customer code or name
        <input name="customerNameSearch" [(ngModel)]="searchName" (keyup.enter)="loadCustomers()" placeholder="CUS000001 or Nguyen Van A">
      </label>
      <label>Phone
        <input name="customerPhoneSearch" [(ngModel)]="searchPhone" (keyup.enter)="loadCustomers()" placeholder="090...">
      </label>
      <label>Phone match
        <select name="phoneMatch" [(ngModel)]="phoneMatch">
          <option [ngValue]="phoneMatchPrefix">Prefix</option>
          <option [ngValue]="phoneMatchSuffix">Suffix</option>
        </select>
      </label>
      <button type="button" (click)="loadCustomers()">Search</button>
    </section>

    <app-page-state [loading]="loading()" [errorMessage]="errorMessage()" [empty]="customers().length === 0" emptyTitle="Chua co khach hang" emptyText="Tao khach hang moi de bat dau ban hang." (retry)="loadCustomers()"></app-page-state>

    <section class="content-grid two" *ngIf="!loading()">
      <article class="panel-card">
        <div class="section-title">
          <h2>Customer List</h2>
          <span>{{ total() }} records</span>
        </div>
        <div class="table-wrap" *ngIf="customers().length > 0">
          <table class="data-table">
            <thead>
              <tr>
                <th>Code</th>
                <th>Name</th>
                <th>Phone</th>
                <th>Status</th>
                <th>Address</th>
                <th>Updated</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let customer of customers()" (click)="selectCustomer(customer)" [class.selected]="selectedCustomer()?.id === customer.id">
                <td>{{ customer.customerCode || '-' }}</td>
                <td>{{ customer.name }}</td>
                <td>{{ customer.phone }}</td>
                <td><app-status-badge [status]="customer.status || 'Normal'"></app-status-badge></td>
                <td>{{ compactText(customer.address) }}</td>
                <td>{{ formatDateTime(customer.updatedAt) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>

      <article class="panel-card">
        <div class="section-title">
          <h2>{{ selectedCustomer() ? 'Edit Customer' : 'Create Customer' }}</h2>
          <button type="button" class="secondary" (click)="resetForm()">New</button>
        </div>

        <div class="error-panel" *ngIf="formErrorSummary().length > 0">
          <p *ngFor="let message of formErrorSummary()">{{ message }}</p>
        </div>

        <form (ngSubmit)="saveCustomer()" class="form-grid">
          <label>Name
            <input name="customerName" [(ngModel)]="customerForm.name" required>
            <small class="field-error" *ngIf="fieldError('Name')">{{ fieldError('Name') }}</small>
          </label>
          <label>Phone
            <input name="customerPhone" [(ngModel)]="customerForm.phone" required>
            <small class="field-error" *ngIf="fieldError('Phone')">{{ fieldError('Phone') }}</small>
          </label>
          <label>Email
            <input name="customerEmail" [(ngModel)]="customerForm.email" type="email">
            <small class="field-error" *ngIf="fieldError('Email')">{{ fieldError('Email') }}</small>
          </label>
          <label>Address
            <textarea name="customerAddress" [(ngModel)]="customerForm.address" rows="3"></textarea>
            <small class="field-error" *ngIf="fieldError('Address')">{{ fieldError('Address') }}</small>
          </label>
          <div class="form-actions">
            <button type="submit" [disabled]="saving()">{{ selectedCustomer() ? 'Save changes' : 'Create customer' }}</button>
            <button type="button" class="secondary" (click)="resetForm()">Cancel</button>
          </div>
        </form>

        <section class="detail-panel" *ngIf="selectedCustomer() as customer">
          <h3>Customer Detail</h3>
          <dl>
            <dt>Code</dt><dd>{{ customer.customerCode || '-' }}</dd>
            <dt>Status</dt><dd><app-status-badge [status]="customer.status || 'Normal'"></app-status-badge></dd>
            <dt>Email</dt><dd>{{ customer.email || '-' }}</dd>
            <dt>Created</dt><dd>{{ formatDateTime(customer.createdAt) }}</dd>
            <dt>Updated</dt><dd>{{ formatDateTime(customer.updatedAt) }}</dd>
          </dl>
          <div class="actions wrap">
            <button type="button" class="secondary" (click)="changeStatus('Normal')" [disabled]="customer.status === 'Normal'">Set Normal</button>
            <button type="button" class="warning" (click)="changeStatus('Suspended')" [disabled]="customer.status === 'Suspended'">Suspend</button>
            <button type="button" class="danger" (click)="changeStatus('Blocked')" [disabled]="customer.status === 'Blocked'">Block</button>
          </div>
        </section>
      </article>
    </section>
  `
})
export class CustomersPageComponent implements OnInit {
  readonly phoneMatchPrefix = PhoneMatch.Prefix;
  readonly phoneMatchSuffix = PhoneMatch.Suffix;
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal('');
  readonly customers = signal<CustomerDto[]>([]);
  readonly total = signal(0);
  readonly selectedCustomer = signal<CustomerDto | null>(null);
  readonly validationErrors = signal<ValidationError[]>([]);
  searchName = '';
  searchPhone = '';
  phoneMatch = PhoneMatch.Prefix;
  customerForm: CustomerForm = this.emptyForm();

  constructor(private readonly api: ApiService) {}

  ngOnInit(): void {
    void this.loadCustomers();
  }

  async loadCustomers(): Promise<void> {
    this.loading.set(true);
    this.errorMessage.set('');
    try {
      const customerPage = await this.api.searchCustomers({
        name: this.searchName,
        phone: this.searchPhone,
        phoneMatch: this.phoneMatch,
        page: 1,
        pageSize: 20
      });
      this.customers.set(customerPage.items);
      this.total.set(customerPage.total);
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  selectCustomer(customer: CustomerDto): void {
    this.selectedCustomer.set(customer);
    this.customerForm = {
      name: customer.name,
      phone: customer.phone,
      email: customer.email || '',
      address: customer.address || ''
    };
    this.validationErrors.set([]);
  }

  resetForm(): void {
    this.selectedCustomer.set(null);
    this.customerForm = this.emptyForm();
    this.validationErrors.set([]);
  }

  async saveCustomer(): Promise<void> {
    if (!this.customerForm.name.trim() || !this.customerForm.phone.trim()) {
      this.validationErrors.set([
        { field: 'Name', message: 'Name is required.' },
        { field: 'Phone', message: 'Phone is required.' }
      ]);
      return;
    }

    this.saving.set(true);
    this.validationErrors.set([]);
    try {
      const selectedCustomer = this.selectedCustomer();
      const customerRequest = {
        name: this.customerForm.name.trim(),
        phone: this.customerForm.phone.trim(),
        email: this.customerForm.email.trim() || null,
        address: this.customerForm.address.trim() || null
      };
      const savedCustomer = selectedCustomer
        ? await this.api.updateCustomer(selectedCustomer.id, customerRequest)
        : await this.api.createCustomer(customerRequest);
      this.selectCustomer(savedCustomer);
      await this.loadCustomers();
    } catch (error) {
      this.handleFormError(error);
    } finally {
      this.saving.set(false);
    }
  }

  async changeStatus(status: ECustomerStatus): Promise<void> {
    const customer = this.selectedCustomer();
    if (!customer) {
      return;
    }

    if (status === 'Blocked' && !confirm('Block Customer\n\nKhach hang bi Blocked se khong duoc tao order moi. Chi Admin nen thuc hien thao tac nay.\n\nBan co chac chan tiep tuc?')) {
      return;
    }

    this.saving.set(true);
    try {
      const updatedCustomer = await this.api.updateCustomerStatus(customer.id, status);
      this.selectCustomer(updatedCustomer);
      await this.loadCustomers();
    } catch (error) {
      this.errorMessage.set(this.describeError(error));
    } finally {
      this.saving.set(false);
    }
  }

  fieldError(field: string): string {
    return this.validationErrors().find(error => error.field.toLowerCase() === field.toLowerCase())?.message ?? '';
  }

  formErrorSummary(): string[] {
    return this.validationErrors().map(error => `${error.field}: ${error.message}`);
  }

  compactText(text: string | null | undefined): string {
    return compactText(text);
  }

  formatDateTime(text: string | null | undefined): string {
    return formatDateTime(text);
  }

  private emptyForm(): CustomerForm {
    return {
      name: 'Nguyen Van A',
      phone: `090${Math.floor(1000000 + Math.random() * 8999999)}`,
      email: '',
      address: ''
    };
  }

  private handleFormError(error: unknown): void {
    if (error instanceof ApiClientError) {
      this.validationErrors.set(error.result.validationErrors);
      this.errorMessage.set(ApiResponseReader.formatFailure(error.result));
      return;
    }

    this.errorMessage.set(this.describeError(error));
  }

  private describeError(error: unknown): string {
    if (error instanceof ApiClientError) {
      return ApiResponseReader.formatFailure(error.result);
    }

    return error instanceof Error ? error.message : 'Request failed.';
  }
}

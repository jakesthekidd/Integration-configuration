import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { Customer } from '../../models/customer.model';
import { GeneralService } from '../../services/general.service';

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './customers.component.html',
  styleUrl: './customers.component.scss',
})
export class CustomersComponent implements OnInit {
  customers: Customer[] = [];
  filterActive: boolean | null = null;
  showCreateForm = false;
  editingCustomer: Customer | null = null;
  error = '';
  success = '';
  creating: boolean = false;
  updating: boolean = false;
  isInitialLoading: boolean = true;
  isLoading: boolean = false;
  deleting: { [id: string]: boolean } = {};
  togglingStatus: { [id: string]: boolean } = {};
  tmsOptions: string[] = ['Legacy McLeod', 'TruckMate', 'BrokerAI'];

  formData: Customer = this.emptyForm();

  constructor(
    private apiService: ApiService,
    private generalService: GeneralService,
  ) {}

  ngOnInit() {
    this.loadCustomers();
  }

  loadCustomers() {
    this.isInitialLoading = true;
    this.apiService.getCustomers(this.filterActive ?? undefined).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.customers = response.data.customers;
        } else {
          this.customers = [];
        }
        this.isInitialLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load customers';
        console.error(err);
        this.isInitialLoading = false;
      },
    });
  }

  toggleCreateForm() {
    this.showCreateForm = !this.showCreateForm;
    this.editingCustomer = null;
    this.formData = this.emptyForm();
    this.clearMessages();
  }

  startEdit(customer: Customer) {
    this.editingCustomer = customer;
    this.showCreateForm = true;
    this.clearMessages();

    this.formData = {
      ...this.emptyForm(),
      ...customer,
      lastSyncTime: customer.lastSyncTime ?? new Date().toISOString(),
    };

    setTimeout(() => {
      document.querySelector('.form-card')?.scrollIntoView({ behavior: 'smooth' });
    }, 50);
  }
  saveCustomer() {
    this.clearMessages();
    const payload = this.CustomerPayload();
    const isUpdate = !!this.editingCustomer;

    if (isUpdate) {
      this.updating = true;

      this.apiService.updateCustomer(this.editingCustomer!.customerId, payload).subscribe({
        next: async (response) => {
          if (response.success) {
            // Wait until the success Swal is closed
            await this.generalService.success('Customer updated successfully');
            this.cancelEditAndReload();
          } else {
            const msg = response.errors?.join(', ') || response.message || 'Failed to update customer';
            this.generalService.error(msg);
          }
        },
        error: (err) => {
          const msg = err.error?.message || err.message || 'Network error while updating customer';
          this.generalService.error(msg);
          this.updating = false;
        },
        complete: () => {
          this.updating = false;
        },
      });
    } else {
      this.creating = true;

      this.apiService.createCustomer(payload).subscribe({
        next: async (response) => {
          if (response.success) {
            await this.generalService.success('Customer created successfully');
            this.cancelEditAndReload();
          } else {
            const msg = response.errors?.join(', ') || response.message || 'Failed to create customer';
            this.generalService.error(msg);
          }
        },
        error: (err) => {
          const msg = err.error?.message || err.message || 'Network error while creating customer';
          this.generalService.error(msg);
          this.creating = false;
        },
        complete: () => {
          this.creating = false;
        },
      });
    }
  }

  private cancelEditAndReload() {
    this.cancelEdit();
    this.loadCustomers();
  }

  private getFormattedTime(date?: string): string {
    const d = date ? new Date(date) : new Date();
    const pad = (n: number) => n.toString().padStart(2, '0');

    return (
      d.getFullYear().toString() +
      pad(d.getMonth() + 1) +
      pad(d.getDate()) +
      pad(d.getHours()) +
      pad(d.getMinutes()) +
      pad(d.getSeconds()) +
      '-0000'
    );
  }
  private CustomerPayload(): Customer {
    const credentials = { ...this.formData.credentials };

    const tmsName = this.tmsOptions.includes(this.formData.tmsName) ? this.formData.tmsName : '';

    return {
      customerId: (this.formData.customerId || '').substring(0, 50),
      customerName: this.formData.customerName || '',
      tmsName: tmsName,
      lastSyncTime: this.formData.lastSyncTime || this.getFormattedTime(),
      updateOrInsertStatuses: this.formData.updateOrInsertStatuses || '',
      updateOnlyStatuses: this.formData.updateOnlyStatuses || null,
      credentials: credentials,
      settings: this.formData.settings || null,
      syncFrequencyMinutes: this.formData.syncFrequencyMinutes,
      orderRetentionDays: this.formData.orderRetentionDays,
      enabled: this.formData.enabled ?? true,
      outboundEnabled: this.formData.outboundEnabled ?? true,
      tonuCode: this.formData.tonuCode || null,
      whiteListedOrders: this.formData.whiteListedOrders || null,
      syncBatchSize: this.formData.syncBatchSize ?? null,
    };
  }

  toggleStatus(customer: Customer) {
    const newStatus = !customer.enabled;
    const actionText = newStatus ? 'activate' : 'deactivate';

    this.generalService
      .confirm({
        title: `${newStatus ? 'Activate' : 'Deactivate'} Customer`,
        text: `Are you sure you want to ${actionText} this customer?`,
        confirmText: newStatus ? 'Yes, Activate' : 'Yes, Deactivate',
        confirmColor: newStatus ? '#28a745' : '#28a745',
        icon: 'question',
      })
      .then((result) => {
        if (!result.isConfirmed) return;

        this.togglingStatus[customer.customerId] = true;
        console.log('customer.customerId', customer.customerId);

        this.apiService.setCustomerStatus(customer.customerId, newStatus).subscribe({
          next: (response) => {
            if (response.success) {
              customer.enabled = newStatus;
              this.generalService.success(`Customer ${actionText}d successfully`);
            } else {
              const msg = response.errors?.join(', ') || `Failed to ${actionText} customer`;
              this.generalService.error(msg);
            }
          },
          error: (err) => {
            this.generalService.error(`Failed to ${actionText} customer: ${err.message || err}`);
            this.togglingStatus[customer.customerId] = false;
          },
          complete: () => {
            this.togglingStatus[customer.customerId] = false;
          },
        });
      });
  }

  deleteCustomer(customerId: string) {
    this.generalService
      .confirm({
        title: 'Delete Customer',
        text: 'Are you sure you want to delete this customer?',
        confirmText: 'Yes, Delete',
        confirmColor: '#e74c3c',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;

        this.deleting[customerId] = true;

        this.apiService.deleteCustomer(customerId).subscribe({
          next: () => {
            this.generalService.success('Customer has been deleted.').then(() => {
              this.loadCustomers();
            });
          },
          error: (err) => {
            this.generalService.error('Failed to delete customer: ' + (err.message || err));
            this.deleting[customerId] = false;
          },
          complete: () => {
            this.deleting[customerId] = false;
          },
        });
      });
  }

  cancelEdit() {
    this.creating = false;
    this.updating = false;
    this.editingCustomer = null;
    this.showCreateForm = false;
    this.formData = this.emptyForm();
    this.clearMessages();
  }

  private clearMessages() {
    this.error = '';
    this.success = '';
  }

  private emptyForm(): Customer {
    return {
      customerId: '',
      customerName: '',
      tmsName: '',
      lastSyncTime: new Date().toISOString(),
      enabled: true,
      outboundEnabled: false,
      credentials: {},
    };
  }

  getCurrentTmsKeys(): string[] {
    return this.formData.tmsName ? this.tmsCredentialKeys[this.formData.tmsName] || [] : [];
  }

  tmsCredentialKeys: { [tms: string]: string[] } = {
    'Legacy McLeod': [
      'mcleod-url',
      'mcleod-auth-header',
      'company-id-header',
      'x1-url',
      'x1-auth-header',
      'wfai-url',
      'wfai-integration-base-url',
      'wfai-portal-customer-id',
      'tonuCode',
    ],
    TruckMate: [
      'truckmate-url',
      'truckmate-auth-token',
      'wfai-url',
      'wfai-integration-base-url',
      'wfai-portal-customer-id',
    ],
    BrokerAI: [
      'brokerai-url',
      'brokerai-username',
      'brokerai-password',
      'brokerai-divisionid',
      'wfai-url',
      'wfai-integration-base-url',
      'wfai-portal-customer-id',
    ],
  };

  allowOnlyNumbers(event: KeyboardEvent) {
    const charCode = event.which ? event.which : event.keyCode;
    if (charCode < 48 || charCode > 57) {
      event.preventDefault();
    }
  }
}

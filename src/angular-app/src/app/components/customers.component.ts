import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { Customer, CustomerRequest } from '../models/customer.model';
import { GeneralService } from '../services/general.service';

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container">
      <h2>Customers</h2>

      <div class="toolbar">
        <div class="filter-group">
          <label>
            Status:
            <select [(ngModel)]="filterActive" (change)="loadCustomers()">
              <option [ngValue]="null">All</option>
              <option [ngValue]="true">Active</option>
              <option [ngValue]="false">Inactive</option>
            </select>
          </label>
        </div>
        <button class="btn-primary" (click)="toggleCreateForm()">
          {{ showCreateForm ? 'Cancel' : '+ Add Customer' }}
        </button>
      </div>

      <!-- Create / Edit form -->
      <div *ngIf="showCreateForm || editingCustomer" class="form-card">
    <h3>{{ editingCustomer ? 'Edit Customer' : 'New Customer' }}</h3>

    <form (ngSubmit)="saveCustomer()" #customerForm="ngForm">

    <div class="form-row">
    <div class="form-group">
        <label>Customer ID <span class="required">*</span></label>
        <input type="text" [(ngModel)]="formData.customerId" name="customerId" required />
      </div>
      <div class="form-group">
        <label>Customer Name <span class="required">*</span></label>
        <input type="text" [(ngModel)]="formData.customerName" name="customerName" required />
      </div>

      <div class="form-group">
      <label>TMS Name <span class="required">*</span></label>
      <select [(ngModel)]="formData.tmsName" name="tmsName" required class="form-control">
        <option value="" disabled>Select TMS</option>
        <option *ngFor="let tms of tmsOptions" [value]="tms">{{ tms }}</option>
      </select>
    </div>
    <div class="form-group">
  <label>Sync Frequency Minutes <span class="required">*</span></label>
  <input
    type="number"
    [(ngModel)]="formData.syncFrequencyMinutes"
    name="syncFrequencyMinutes"
    class="form-control"
    required
    min="0"
    (keypress)="allowOnlyNumbers($event)"
  />
</div>

<div class="form-group">
  <label>Order Retention Days <span class="required">*</span></label>
  <input
    type="number"
    [(ngModel)]="formData.orderRetentionDays"
    name="orderRetentionDays"
    class="form-control"
    required
    min="0"
    (keypress)="allowOnlyNumbers($event)"
  />
</div>
    </div>
    
    <div class="form-group checkbox">
      <label>
        <input type="checkbox" [(ngModel)]="formData.enabled" name="enabled" />
        Enabled
      </label>
    </div>

    <div class="form-group checkbox">
      <label>
        <input type="checkbox" [(ngModel)]="formData.outboundEnabled" name="outboundEnabled" />
        Outbound Enabled
      </label>
    </div>
    <label><b>Credential</b></label><br>
    <div *ngIf="formData.tmsName && tmsCredentialKeys[formData.tmsName]?.length">
  <div class="form-row">
    <div class="col" *ngFor="let key of tmsCredentialKeys[formData.tmsName]; let i = index">
      <div class="form-group">
        <label>{{ key }}</label>
        <input
          type="text"
          [(ngModel)]="formData.credentials[key]"
          [name]="'credentials.' + key"
          class="form-control"
        />
      </div>
    </div>
  </div>
</div>

    <div class="form-actions">
    <button class="btn-primary" type="submit"
        [disabled]="!customerForm.form.valid || creating || updating">
      <span *ngIf="creating || updating" class="spinner"></span>
      {{ creating ? 'Creating...' : updating ? 'Saving...' : (editingCustomer ? 'Save Changes' : 'Create Customer') }}
    </button>

      <button type="button" class="btn-secondary" (click)="cancelEdit()">Cancel</button>
    </div>

    </form>
</div>


      <div *ngIf="error" class="alert alert-error">{{ error }}</div>
      <div *ngIf="success" class="alert alert-success">{{ success }}</div>

      <!-- Table -->
      <div class="table-container">
      <table>
  <thead>
    <tr>
      <th>Name</th>
      <th>TMS</th>
      <th>Status</th>
      <th>Sync (min)</th>
      <th>Retention (days)</th>
      <th>Last Sync</th>
      <th>Actions</th>
    </tr>
  </thead>

  <tbody>
    <tr *ngFor="let c of customers">
      <td><strong>{{ c.customerName }}</strong></td>
      <td>{{ c.tmsName }}</td>

      <td>
        <span class="badge" 
              [class.badge-active]="c.enabled" 
              [class.badge-inactive]="!c.enabled">
          {{ c.enabled ? 'Active' : 'Inactive' }}
        </span>
      </td>

      <td>{{ c.syncFrequencyMinutes || '—' }}</td>
      <td>{{ c.orderRetentionDays || '—' }}</td>

      <td>{{ c.lastSyncTime | date:'medium' }}</td>

      <td class="actions">
        <button class="btn-small btn-info" (click)="startEdit(c)">Edit</button>

        <button class="btn-small btn-info" 
                (click)="toggleStatus(c)" 
                [disabled]="togglingStatus[c.customerId]">
          <span *ngIf="togglingStatus[c.customerId]" class="spinner"></span>
          <span *ngIf="!togglingStatus[c.customerId]">
            {{ c.enabled ? 'Deactivate' : 'Activate' }}
          </span>
        </button>

        <button class="btn-small btn-danger" 
                (click)="deleteCustomer(c.customerId)" 
                [disabled]="deleting[c.customerId]">
          <span *ngIf="deleting[c.customerId]" class="spinner"></span>
          <span *ngIf="!deleting[c.customerId]">Delete</span>
        </button>
      </td>
    </tr>

       <tr *ngIf="customers.length === 0">
         <td colspan="7" class="no-data">No customers found.</td>
       </tr>
      </tbody>
    </table>
    <div *ngIf="isInitialLoading" class="fullscreen-loader">
  <div class="loader-spinner"></div>
</div>
      </div>
    </div>

  `,
  styles: [`
    .container { max-width: 1400px; margin: 0 auto; padding: 20px; }

    h2 { color: #2c3e50; margin-bottom: 20px; }

    .toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
      gap: 12px;
    }

    .filter-group {
      display: flex;
      gap: 12px;
      align-items: center;
    }

    .filter-group label {
      font-weight: 500;
      color: #555;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .filter-group select {
      padding: 6px 10px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
    }

    /* Form card */
    .form-card {
      background: white;
      border: 1px solid #ddd;
      border-radius: 6px;
      padding: 24px;
      margin-bottom: 24px;
      box-shadow: 0 2px 6px rgba(0,0,0,0.08);
    }

    .form-card h3 { margin: 0 0 20px; color: #2c3e50; }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
    }

    @media (max-width: 700px) {
      .form-row { grid-template-columns: 1fr; }
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 6px;
      margin-bottom: 14px;
    }

    .form-group label {
      font-weight: 500;
      color: #555;
      font-size: 14px;
    }

    .form-group input,
    .form-group select,
    .form-group textarea {
      padding: 8px 12px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
    }
    .spinner {
      display: inline-block;
      width: 16px;
      height: 16px;
      border: 2px solid rgba(255,255,255,0.6);
      border-top-color: white;
      border-radius: 50%;
      animation: spin 0.6s linear infinite;
      vertical-align: middle;
      margin-right: 5px;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }
    .form-group.checkbox { flex-direction: row; align-items: center; }
    .form-group.checkbox label { display: flex; align-items: center; gap: 8px; font-weight: 400; }

    .required { color: #e74c3c; }

    .form-actions {
      display: flex;
      gap: 10px;
      margin-top: 8px;
    }

    /* Alerts */
    .alert {
      padding: 12px 16px;
      border-radius: 4px;
      margin-bottom: 16px;
      font-size: 14px;
    }
    .alert-error  { background: #fee; color: #c33; border-left: 4px solid #e74c3c; }
    .alert-success { background: #d4edda; color: #155724; border-left: 4px solid #27ae60; }

    /* Table */
    .table-container {
      background: white;
      border-radius: 6px;
      border: 1px solid #ddd;
      overflow: auto;
      box-shadow: 0 2px 4px rgba(0,0,0,0.06);
    }

    table { width: 100%; border-collapse: collapse; font-size: 14px; }

    thead tr { background: #f8f9fa; }

    th, td {
      padding: 12px 14px;
      text-align: left;
      border-bottom: 1px solid #eee;
    }

    th { font-weight: 600; color: #555; font-size: 13px; }

    tr:last-child td { border-bottom: none; }

    tr:hover td { background: #fafafa; }

    .notes-cell { max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: #666; }

    .muted { color: #999; }

    .no-data { text-align: center; color: #999; font-style: italic; padding: 40px; }

    /* Badges */
    .badge {
      display: inline-block;
      padding: 2px 10px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 600;
    }
    .badge-active   { background: #d4edda; color: #155724; }
    .badge-inactive { background: #f8d7da; color: #721c24; }

    /* Buttons */
    .btn-primary, .btn-secondary, .btn-small {
      padding: 8px 18px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
    }
    .btn-primary   { background: #27ae60; color: white; }
    .btn-primary:hover:not(:disabled) { background: #229954; }
    .btn-primary:disabled { background: #95a5a6; cursor: not-allowed; }
    .btn-secondary { background: #95a5a6; color: white; }
    .btn-secondary:hover { background: #7f8c8d; }
    .btn-small { padding: 4px 12px; font-size: 12px; }
    .btn-info   { background: #3498db; color: white; }
    .btn-info:hover { background: #2980b9; }
    .btn-danger { background: #e74c3c; color: white; }
    .btn-danger:hover { background: #c0392b; }

    .actions { display: flex; gap: 6px; }

    /*body spinner */
.fullscreen-loader {
  position: fixed;     
  top: 0;
  left: 0;
  width: 100vw;         
  height: 100vh;       

  display: flex;
  justify-content: center;
  align-items: center;

  background: rgba(0, 0, 0, 0.35);
  backdrop-filter: blur(4px);

  z-index: 9999;   
}

.loader-spinner {
  width: 70px;
  height: 70px;
  border: 7px solid #e0e0e0;
  border-top: 7px solid #3498db;
  border-radius: 50%;
  animation: loaderSpin 0.8s linear infinite;
}

@keyframes loaderSpin {
  to { transform: rotate(360deg); }
}
  `]
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

  constructor(private apiService: ApiService, private generalService: GeneralService) { }

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
      }
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
  
      this.apiService.updateCustomer(this.editingCustomer!.customerId, payload)
        .subscribe({
          next: async (response) => {
            if (response.success) {
              // Wait until the success Swal is closed
              await this.generalService.success("Customer updated successfully");
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
          complete: () => { this.updating = false; }
        });
  
    } else {
      this.creating = true;
  
      this.apiService.createCustomer(payload)
        .subscribe({
          next: async (response) => {
            if (response.success) {
              await this.generalService.success("Customer created successfully");
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
          complete: () => { this.creating = false; }
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

    const tmsName = this.tmsOptions.includes(this.formData.tmsName)
      ? this.formData.tmsName
      : '';

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
      syncBatchSize: this.formData.syncBatchSize ?? null
    };
  }

  toggleStatus(customer: Customer) {
    const newStatus = !customer.enabled;
    const actionText = newStatus ? 'activate' : 'deactivate';

    this.generalService.confirm({
      title: `${newStatus ? 'Activate' : 'Deactivate'} Customer`,
      text: `Are you sure you want to ${actionText} this customer?`,
      confirmText: newStatus ? 'Yes, Activate' : 'Yes, Deactivate',
      confirmColor: newStatus ? '#28a745' : '#28a745',
      icon: 'question'
    })
      .then(result => {
        if (!result.isConfirmed) return;

        this.togglingStatus[customer.customerId] = true;
        console.log("customer.customerId", customer.customerId);

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
          complete: () => { this.togglingStatus[customer.customerId] = false; }
        });
      });
  }

  deleteCustomer(customerId: string) {
    this.generalService.confirm({
      title: 'Delete Customer',
      text: 'Are you sure you want to delete this customer?',
      confirmText: 'Yes, Delete',
      confirmColor: '#e74c3c',
      icon: 'warning'
    })
    .then(result => {
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
        complete: () => { this.deleting[customerId] = false; }
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
      'tonuCode'
    ],
    'TruckMate': [
      'truckmate-url',
      'truckmate-auth-token',
      'wfai-url',
      'wfai-integration-base-url',
      'wfai-portal-customer-id'
    ],
    'BrokerAI': [
      'brokerai-url',
      'brokerai-username',
      'brokerai-password',
      'brokerai-divisionid',
      'wfai-url',
      'wfai-integration-base-url',
      'wfai-portal-customer-id'
    ]
  };

  allowOnlyNumbers(event: KeyboardEvent) {
    const charCode = event.which ? event.which : event.keyCode;
    if (charCode < 48 || charCode > 57) {
      event.preventDefault();
    }
  }

}

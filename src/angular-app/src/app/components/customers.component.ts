import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { Customer, CreateCustomerRequest } from '../models/customer.model';

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
        <form (ngSubmit)="editingCustomer ? updateCustomer() : createCustomer()" #customerForm="ngForm">

          <div class="form-row">
            <div class="form-group">
              <label>Name <span class="required">*</span></label>
              <input type="text" [(ngModel)]="formData.name" name="name" required
                     placeholder="e.g. Cheema Transport" />
            </div>
            <div class="form-group">
              <label>Code</label>
              <input type="text" [(ngModel)]="formData.code" name="code"
                     placeholder="e.g. cheema (unique short ID)" />
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Contact Email</label>
              <input type="email" [(ngModel)]="formData.contactEmail" name="contactEmail"
                     placeholder="contact@company.com" />
            </div>
            <div class="form-group">
              <label>Contact Phone</label>
              <input type="tel" [(ngModel)]="formData.contactPhone" name="contactPhone"
                     placeholder="+1 555 000 0000" />
            </div>
          </div>

          <div class="form-group">
            <label>Notes</label>
            <textarea [(ngModel)]="formData.notes" name="notes" rows="2"
                      placeholder="Any additional notes about this customer"></textarea>
          </div>

          <div class="form-group checkbox">
            <label>
              <input type="checkbox" [(ngModel)]="formData.isActive" name="isActive" />
              Active
            </label>
          </div>

          <div class="form-actions">
            <button type="submit" class="btn-primary" [disabled]="!customerForm.form.valid">
              {{ editingCustomer ? 'Save Changes' : 'Create Customer' }}
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
              <th>Code</th>
              <th>Email</th>
              <th>Phone</th>
              <th>Status</th>
              <th>Notes</th>
              <th>Created</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let c of customers">
              <td><strong>{{ c.name }}</strong></td>
              <td><code *ngIf="c.code">{{ c.code }}</code><span *ngIf="!c.code" class="muted">—</span></td>
              <td>{{ c.contactEmail || '—' }}</td>
              <td>{{ c.contactPhone || '—' }}</td>
              <td>
                <span class="badge" [class.badge-active]="c.isActive" [class.badge-inactive]="!c.isActive">
                  {{ c.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="notes-cell">{{ c.notes || '—' }}</td>
              <td>{{ c.createdAt | date:'mediumDate' }}</td>
              <td class="actions">
                <button class="btn-small btn-info" (click)="startEdit(c)">Edit</button>
                <button class="btn-small btn-danger" (click)="deleteCustomer(c.id, c.name)">Delete</button>
              </td>
            </tr>
            <tr *ngIf="customers.length === 0">
              <td colspan="8" class="no-data">No customers found. Add one above.</td>
            </tr>
          </tbody>
        </table>
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
  `]
})
export class CustomersComponent implements OnInit {
  customers: Customer[] = [];
  filterActive: boolean | null = null;

  showCreateForm = false;
  editingCustomer: Customer | null = null;
  error = '';
  success = '';

  formData: CreateCustomerRequest = this.emptyForm();

  constructor(private apiService: ApiService) {}

  ngOnInit() {
    this.loadCustomers();
  }

  loadCustomers() {
    this.apiService.getCustomers(this.filterActive ?? undefined).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.customers = response.data.customers;
        }
      },
      error: (err) => {
        this.error = 'Failed to load customers';
        console.error(err);
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
    this.showCreateForm = false;
    this.clearMessages();
    this.formData = {
      name: customer.name,
      code: customer.code ?? '',
      contactEmail: customer.contactEmail ?? '',
      contactPhone: customer.contactPhone ?? '',
      isActive: customer.isActive,
      notes: customer.notes ?? ''
    };
    setTimeout(() => {
      document.querySelector('.form-card')?.scrollIntoView({ behavior: 'smooth' });
    }, 50);
  }

  createCustomer() {
    this.clearMessages();
    this.apiService.createCustomer(this.formData).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `Customer "${response.data?.name}" created successfully.`;
          this.showCreateForm = false;
          this.formData = this.emptyForm();
          this.loadCustomers();
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to create customer';
        console.error(err);
      }
    });
  }

  updateCustomer() {
    if (!this.editingCustomer) return;
    this.clearMessages();

    const request = {
      name: this.formData.name,
      code: this.formData.code,
      contactEmail: this.formData.contactEmail,
      contactPhone: this.formData.contactPhone,
      isActive: this.formData.isActive,
      notes: this.formData.notes
    };

    this.apiService.updateCustomer(this.editingCustomer.id, request).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `Customer "${response.data?.name}" updated successfully.`;
          this.cancelEdit();
          this.loadCustomers();
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to update customer';
        console.error(err);
      }
    });
  }

  deleteCustomer(id: string, name: string) {
    if (!confirm(`Delete customer "${name}"? Templates linked to this customer will be unlinked.`)) return;
    this.clearMessages();

    this.apiService.deleteCustomer(id).subscribe({
      next: () => {
        this.success = `Customer "${name}" deleted.`;
        this.loadCustomers();
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to delete customer';
        console.error(err);
      }
    });
  }

  cancelEdit() {
    this.editingCustomer = null;
    this.showCreateForm = false;
    this.formData = this.emptyForm();
    this.clearMessages();
  }

  private clearMessages() {
    this.error = '';
    this.success = '';
  }

  private emptyForm(): CreateCustomerRequest {
    return { name: '', code: '', contactEmail: '', contactPhone: '', isActive: true, notes: '' };
  }
}

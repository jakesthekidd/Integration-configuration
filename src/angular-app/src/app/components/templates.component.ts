import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { FieldMappingTemplate, CreateTemplateRequest, UpdateTemplateRequest } from '../models/template.model';
import { TmsSystem } from '../models/tms-system.model';
import { Customer } from '../models/customer.model';

@Component({
  selector: 'app-templates',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container">
      <h2>Template Management</h2>

      <div class="filters">
        <label>
          Filter by TMS System:
          <select [(ngModel)]="selectedTmsSystemId" (change)="onTmsFilterChange()">
            <option value="">All Systems</option>
            <option *ngFor="let tms of tmsSystems" [value]="tms.id">
              {{ tms.displayName }}
            </option>
          </select>
        </label>
        <button class="btn-primary" (click)="toggleCreateForm()">
          {{ showCreateForm ? 'Cancel' : 'New Template' }}
        </button>
      </div>

      <!-- Create Form -->
      <div *ngIf="showCreateForm" class="form-container">
        <h3>Create Template</h3>
        <form (ngSubmit)="createTemplate()" #createForm="ngForm">
          <div class="form-row">
            <div class="form-group">
              <label>Name <span class="required">*</span></label>
              <input type="text" [(ngModel)]="newTemplate.name" name="name" required
                     placeholder="e.g., McLeod to WFAI Transformation" />
            </div>
            <div class="form-group">
              <label>TMS System <span class="required">*</span></label>
              <select [(ngModel)]="newTemplate.tmsSystemId" name="tmsSystemId" required>
                <option value="">Select TMS System</option>
                <option *ngFor="let tms of tmsSystems" [value]="tms.id">
                  {{ tms.displayName }}
                </option>
              </select>
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Customer</label>
              <select [(ngModel)]="newTemplate.customerId" name="customerId">
                <option value="">— Generic (no customer) —</option>
                <!-- <option *ngFor="let c of customers" [value]="c.id">
                  {{ c.name }}{{ c.code ? ' (' + c.code + ')' : '' }}
                </option> -->
              </select>
              <small>Optionally scope this template to a specific customer.</small>
            </div>
            <div class="form-group">
              <label>Description</label>
              <textarea [(ngModel)]="newTemplate.description" name="description" rows="2"
                        placeholder="Describe what this template transforms and how"></textarea>
            </div>
          </div>
          <div class="form-group">
            <label>Sample Input JSON</label>
            <textarea [(ngModel)]="newTemplate.sampleInputJson" name="sampleInputJson" rows="5"
                      class="json-textarea"
                      placeholder='{ "field": "value", ... }'></textarea>
            <small>Paste a representative source JSON payload. Used to auto-suggest source field paths in the Field Mappings screen and to pre-populate the Test Transformation screen.</small>
          </div>
          <div class="form-actions">
            <button type="submit" class="btn-primary" [disabled]="!createForm.form.valid">
              Create
            </button>
            <button type="button" class="btn-secondary" (click)="toggleCreateForm()">
              Cancel
            </button>
          </div>
        </form>
      </div>

      <!-- Edit Form -->
      <div *ngIf="editingTemplate" class="form-container">
        <h3>Edit Template</h3>
        <form (ngSubmit)="updateTemplate()" #editForm="ngForm">
          <div class="form-row">
            <div class="form-group">
              <label>Name <span class="required">*</span></label>
              <input type="text" [(ngModel)]="editRequest.name" name="editName" required
                     placeholder="Template name" />
            </div>
            <div class="form-group">
              <label>TMS System</label>
              <input type="text" [value]="getTmsSystemName(editingTemplate.tmsSystemId)" readonly />
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Customer</label>
              <select [(ngModel)]="editRequest.customerId" name="editCustomerId">
                <option value="">— Generic (no customer) —</option>
                <!-- <option *ngFor="let c of customers" [value]="c.id">
                  {{ c.name }}{{ c.code ? ' (' + c.code + ')' : '' }}
                </option> -->
              </select>
            </div>
            <div class="form-group">
              <label>Description</label>
              <textarea [(ngModel)]="editRequest.description" name="editDescription" rows="2"
                        placeholder="Template description"></textarea>
            </div>
          </div>
          <div class="form-group">
            <label>Sample Input JSON</label>
            <textarea [(ngModel)]="editRequest.sampleInputJson" name="editSampleInputJson" rows="5"
                      class="json-textarea"
                      placeholder='{ "field": "value", ... }'></textarea>
            <small>Paste a representative source JSON payload. Used to auto-suggest source field paths in the Field Mappings screen and to pre-populate the Test Transformation screen.</small>
          </div>
          <div class="form-actions">
            <button type="submit" class="btn-primary" [disabled]="!editForm.form.valid">
              Save Changes
            </button>
            <button type="button" class="btn-secondary" (click)="cancelEdit()">
              Cancel
            </button>
          </div>
        </form>
      </div>

      <div *ngIf="error" class="error">{{ error }}</div>
      <div *ngIf="success" class="success">{{ success }}</div>

      <!-- Templates Table -->
      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>TMS System</th>
              <th>Customer</th>
              <th>Version</th>
              <th>Status</th>
              <th>Description</th>
              <th>Created</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let template of templates">
              <td>
                <strong>{{ template.name }}</strong>
                <br /><small class="muted">{{ template.templateId }}</small>
              </td>
              <td>{{ getTmsSystemName(template.tmsSystemId) }}</td>
              <!-- <td>{{ getCustomerName(template.customerId) }}</td> -->
              <td>
                <span class="badge badge-version">v{{ template.version }}</span>
              </td>
              <td>
                <span class="badge" [ngClass]="getStatusClass(template.status)">
                  {{ template.status }}
                </span>
              </td>
              <td class="description-cell">{{ template.description || '—' }}</td>
              <td>{{ formatDate(template.createdAt) }}</td>
              <td class="actions-cell">
                <button class="btn-small btn-info" (click)="startEdit(template)"
                        title="Edit template name and description">
                  Edit
                </button>
                <button class="btn-small btn-duplicate" (click)="duplicateTemplate(template)"
                        title="Create a copy of this template with all its field mappings">
                  Duplicate
                </button>
                <button class="btn-small btn-publish"
                        *ngIf="template.status === 'Draft'"
                        (click)="publishTemplate(template)"
                        title="Publish this template to make it available for transformations">
                  Publish
                </button>
                <button class="btn-small btn-archive"
                        *ngIf="template.status === 'Published'"
                        (click)="archiveTemplate(template)"
                        title="Archive this template">
                  Archive
                </button>
                <button class="btn-small btn-danger"
                        (click)="deleteTemplate(template)"
                        title="Delete this template version">
                  Delete
                </button>
              </td>
            </tr>
            <tr *ngIf="templates.length === 0">
              <td colspan="8" class="no-data">
                No templates found.
                <span *ngIf="selectedTmsSystemId">Try clearing the TMS filter.</span>
                <span *ngIf="!selectedTmsSystemId">Click "New Template" to create one.</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="total">{{ templates.length }} template(s) shown</div>
    </div>
  `,
  styles: [`
    .container {
      max-width: 1200px;
      margin: 0 auto;
    }

    h2 {
      color: #2c3e50;
      margin-bottom: 20px;
    }

    .filters {
      display: flex;
      gap: 15px;
      align-items: center;
      margin-bottom: 20px;
      padding: 15px;
      background: #f8f9fa;
      border-radius: 4px;
    }

    .filters label {
      display: flex;
      align-items: center;
      gap: 10px;
      font-weight: 500;
    }

    .filters select {
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
      min-width: 200px;
    }

    .form-container {
      background: #f8f9fa;
      padding: 20px;
      border-radius: 4px;
      margin-bottom: 20px;
      border-left: 4px solid #3498db;
    }

    .form-container h3 {
      margin-top: 0;
      color: #2c3e50;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 15px;
    }

    .form-group {
      margin-bottom: 15px;
    }

    .form-group label {
      display: block;
      font-weight: 500;
      margin-bottom: 5px;
      color: #555;
    }

    .form-group input[type="text"],
    .form-group select,
    .form-group textarea {
      width: 100%;
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-family: inherit;
      font-size: 14px;
      box-sizing: border-box;
    }

    .form-group input[readonly] {
      background: #e9ecef;
      cursor: not-allowed;
    }

    .json-textarea {
      font-family: 'Courier New', monospace;
      font-size: 12px;
    }

    .required {
      color: #e74c3c;
    }

    .form-actions {
      display: flex;
      gap: 10px;
      margin-top: 20px;
    }

    .btn-primary, .btn-secondary, .btn-small, .btn-danger, .btn-publish, .btn-archive, .btn-info {
      padding: 8px 16px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
    }

    .btn-primary {
      background: #3498db;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background: #2980b9;
    }

    .btn-primary:disabled {
      background: #95a5a6;
      cursor: not-allowed;
    }

    .btn-secondary {
      background: #95a5a6;
      color: white;
    }

    .btn-secondary:hover {
      background: #7f8c8d;
    }

    .btn-small {
      padding: 4px 10px;
      font-size: 12px;
      margin-right: 4px;
    }

    .btn-info {
      background: #3498db;
      color: white;
    }

    .btn-info:hover {
      background: #2980b9;
    }

    .btn-duplicate {
      background: #8e44ad;
      color: white;
    }

    .btn-duplicate:hover {
      background: #7d3c98;
    }

    .btn-publish {
      background: #27ae60;
      color: white;
    }

    .btn-publish:hover {
      background: #219a52;
    }

    .btn-archive {
      background: #f39c12;
      color: white;
    }

    .btn-archive:hover {
      background: #e67e22;
    }

    .btn-danger {
      background: #e74c3c;
      color: white;
    }

    .btn-danger:hover {
      background: #c0392b;
    }

    .error {
      background: #fee;
      color: #c33;
      padding: 10px 15px;
      border-radius: 4px;
      margin-bottom: 15px;
      border-left: 4px solid #e74c3c;
    }

    .success {
      background: #efe;
      color: #2d6a2d;
      padding: 10px 15px;
      border-radius: 4px;
      margin-bottom: 15px;
      border-left: 4px solid #27ae60;
    }

    .table-container {
      overflow-x: auto;
    }

    table {
      width: 100%;
      border-collapse: collapse;
      background: white;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    th, td {
      padding: 12px;
      text-align: left;
      border-bottom: 1px solid #ddd;
    }

    th {
      background: #34495e;
      color: white;
      font-weight: 500;
      white-space: nowrap;
    }

    tbody tr:hover {
      background: #f5f5f5;
    }

    .description-cell {
      max-width: 280px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      color: #555;
      font-size: 13px;
    }

    .actions-cell {
      white-space: nowrap;
    }

    .muted {
      color: #999;
      font-size: 11px;
      font-family: 'Courier New', monospace;
    }

    .badge {
      display: inline-block;
      padding: 3px 8px;
      border-radius: 3px;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .badge-version {
      background: #ecf0f1;
      color: #555;
    }

    .badge-draft {
      background: #f39c12;
      color: white;
    }

    .badge-published {
      background: #27ae60;
      color: white;
    }

    .badge-archived {
      background: #95a5a6;
      color: white;
    }

    .no-data {
      text-align: center;
      color: #999;
      font-style: italic;
      padding: 30px;
    }

    .total {
      margin-top: 10px;
      color: #666;
      font-size: 13px;
      text-align: right;
    }
  `]
})
export class TemplatesComponent implements OnInit {
  templates: FieldMappingTemplate[] = [];
  tmsSystems: TmsSystem[] = [];
  customers: Customer[] = [];
  selectedTmsSystemId: string = '';
  showCreateForm: boolean = false;
  editingTemplate: FieldMappingTemplate | null = null;
  error: string = '';
  success: string = '';

  newTemplate: CreateTemplateRequest = this.getEmptyCreateRequest();
  editRequest: UpdateTemplateRequest = { name: '', description: '', customerId: '' };

  constructor(private apiService: ApiService) {}

  ngOnInit() {
    this.loadTmsSystems();
    this.loadCustomers();
    this.loadTemplates();
  }

  loadCustomers() {
    this.apiService.getCustomers(true).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.customers = response.data.customers;
        }
      },
      error: (err) => console.error('Failed to load customers', err)
    });
  }

  loadTmsSystems() {
    this.apiService.getTmsSystems(true).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.tmsSystems = response.data.systems;
        }
      },
      error: (err) => {
        console.error('Failed to load TMS systems', err);
      }
    });
  }

  loadTemplates() {
    this.apiService.getTemplates(this.selectedTmsSystemId || undefined).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.templates = response.data.templates;
        }
      },
      error: (err) => {
        this.error = 'Failed to load templates';
        console.error(err);
      }
    });
  }

  onTmsFilterChange() {
    this.loadTemplates();
  }

  toggleCreateForm() {
    this.showCreateForm = !this.showCreateForm;
    if (!this.showCreateForm) {
      this.newTemplate = this.getEmptyCreateRequest();
    }
    this.editingTemplate = null;
    this.clearMessages();
  }

  createTemplate() {
    this.clearMessages();

    this.apiService.createTemplate(this.newTemplate).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `Template "${response.data?.name}" created successfully as Draft.`;
          this.showCreateForm = false;
          this.newTemplate = this.getEmptyCreateRequest();
          this.loadTemplates();
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to create template';
        console.error(err);
      }
    });
  }

  startEdit(template: FieldMappingTemplate) {
    this.editingTemplate = template;
    this.showCreateForm = false;
    this.editRequest = {
      name: template.name,
      description: template.description,
      customerId: template.customerId ?? '',
      sampleInputJson: template.sampleInputJson ?? ''
    };
    this.clearMessages();

    setTimeout(() => {
      document.querySelector('.form-container')?.scrollIntoView({ behavior: 'smooth' });
    }, 100);
  }

  updateTemplate() {
    if (!this.editingTemplate) return;
    this.clearMessages();

    this.apiService.updateTemplate(this.editingTemplate.templateId, this.editRequest).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `Template updated to version ${response.data?.version}.`;
          this.cancelEdit();
          this.loadTemplates();
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to update template';
        console.error(err);
      }
    });
  }

  cancelEdit() {
    this.editingTemplate = null;
    this.editRequest = { name: '', description: '', customerId: '', sampleInputJson: '' };
  }

  duplicateTemplate(template: FieldMappingTemplate) {
    this.clearMessages();

    this.apiService.duplicateTemplate(template.templateId).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `Template "${response.data?.name}" created as a copy of "${template.name}".`;
          this.loadTemplates();
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to duplicate template';
        console.error(err);
      }
    });
  }

  publishTemplate(template: FieldMappingTemplate) {
    if (!confirm(`Publish template "${template.name}"? It will become available for transformations.`)) {
      return;
    }
    this.clearMessages();

    const publishRequest: UpdateTemplateRequest = {
      name: template.name,
      description: template.description,
      status: 'Published'
    };

    this.apiService.updateTemplate(template.templateId, publishRequest).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `Template "${template.name}" published successfully.`;
          this.loadTemplates();
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to publish template';
        console.error(err);
      }
    });
  }

  archiveTemplate(template: FieldMappingTemplate) {
    if (!confirm(`Archive template "${template.name}"? It will no longer be available for new transformations.`)) {
      return;
    }
    this.clearMessages();

    const archiveRequest: UpdateTemplateRequest = {
      name: template.name,
      description: template.description,
      status: 'Archived'
    };

    this.apiService.updateTemplate(template.templateId, archiveRequest).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `Template "${template.name}" archived.`;
          this.loadTemplates();
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to archive template';
        console.error(err);
      }
    });
  }

  deleteTemplate(template: FieldMappingTemplate) {
    if (!confirm(`Delete template "${template.name}" (v${template.version})? This cannot be undone.`)) {
      return;
    }
    this.clearMessages();

    this.apiService.deleteTemplate(template.templateId, template.version).subscribe({
      next: () => {
        this.success = `Template "${template.name}" (v${template.version}) deleted.`;
        this.loadTemplates();
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to delete template';
        console.error(err);
      }
    });
  }

  // getCustomerName(customerId?: string): string {
  //   if (!customerId) return '—';
  //   // const c = this.customers.find(x => x.id === customerId);
  //   return c ? c.name : customerId;
  // }

  getTmsSystemName(tmsSystemId: string): string {
    const tms = this.tmsSystems.find(t => t.id === tmsSystemId);
    return tms ? tms.displayName : tmsSystemId;
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'published': return 'badge-published';
      case 'archived':  return 'badge-archived';
      default:          return 'badge-draft';
    }
  }

  formatDate(dateValue: Date | string): string {
    if (!dateValue) return '—';
    const d = new Date(dateValue);
    return isNaN(d.getTime()) ? String(dateValue) : d.toLocaleDateString();
  }

  private clearMessages() {
    this.error = '';
    this.success = '';
  }

  private getEmptyCreateRequest(): CreateTemplateRequest {
    return { name: '', description: '', tmsSystemId: '', customerId: '', sampleInputJson: '' };
  }
}

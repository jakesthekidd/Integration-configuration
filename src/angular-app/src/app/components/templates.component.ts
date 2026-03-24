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
              <label>Description</label>
              <textarea [(ngModel)]="newTemplate.description" name="description" rows="2"
                        placeholder="Describe what this template transforms and how"></textarea>
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Source Schema (JSON)</label>
              <textarea [(ngModel)]="newTemplate.sourceSchema" name="sourceSchema" rows="5"
                        class="json-textarea"
                        placeholder='{ "field": "value", ... }'></textarea>
              <div class="drop-zone" (click)="fileInputCreateSource.click()" (dragover)="$event.preventDefault()" (drop)="onFileDropSourceSchema($event, false)">
                <p>Drag & Drop JSON file here or <strong>Click to Upload</strong></p>
                <input type="file" #fileInputCreateSource (change)="handleFileUploadSourceSchema($event, false)" accept=".json" style="display:none">
              </div>
              <small>Used to auto-suggest source field paths in the Field Mappings screen.</small>
            </div>
            <div class="form-group">
              <label>Target Schema (JSON)</label>
              <textarea [(ngModel)]="newTemplate.targetSchema" name="targetSchema" rows="5"
                        class="json-textarea"
                        placeholder='{ "field": "value", ... }'></textarea>
              <div class="drop-zone" (click)="fileInputCreateTarget.click()" (dragover)="$event.preventDefault()" (drop)="onFileDropTargetSchema($event, false)">
                <p>Drag & Drop JSON file here or <strong>Click to Upload</strong></p>
                <input type="file" #fileInputCreateTarget (change)="handleFileUploadTargetSchema($event, false)" accept=".json" style="display:none">
              </div>
            </div>
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
              <label>Description</label>
              <textarea [(ngModel)]="editRequest.description" name="editDescription" rows="2"
                        placeholder="Template description"></textarea>
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Edit Version</label>
              <select [(ngModel)]="selectedVersion" name="editVersion" (change)="onVersionSelect()">
                <option *ngFor="let v of templateVersions" [value]="v.version">
                  Version {{ v.version }} ({{ v.status }})
                </option>
              </select>
            </div>
            <div class="form-group"></div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>Source Schema (JSON)</label>
              <textarea [(ngModel)]="editRequest.sourceSchema" name="editSourceSchema" rows="5"
                        class="json-textarea"
                       placeholder='{ "field": "value", ... }'></textarea>
              <div class="drop-zone" (click)="fileInputEditSource.click()" (dragover)="$event.preventDefault()" (drop)="onFileDropSourceSchema($event, true)">
                <p>Drag & Drop JSON file here or <strong>Click to Upload</strong></p>
                <input type="file" #fileInputEditSource (change)="handleFileUploadSourceSchema($event, true)" accept=".json" style="display:none">
              </div>
              <small>Used to auto-suggest source field paths.</small>
            </div>
            <div class="form-group">
              <label>Target Schema (JSON)</label>
              <textarea [(ngModel)]="editRequest.targetSchema" name="editTargetSchema" rows="5"
                        class="json-textarea"
                       placeholder='{ "field": "value", ... }'></textarea>
              <div class="drop-zone" (click)="fileInputEditTarget.click()" (dragover)="$event.preventDefault()" (drop)="onFileDropTargetSchema($event, true)">
                <p>Drag & Drop JSON file here or <strong>Click to Upload</strong></p>
                <input type="file" #fileInputEditTarget (change)="handleFileUploadTargetSchema($event, true)" accept=".json" style="display:none">
              </div>
            </div>
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
                <br /><small class="muted">{{ template.id }}</small>
              </td>
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
                <button class="btn-small btn-reactivate"
                        *ngIf="template.status === 'Archived'"
                        (click)="reactivateTemplate(template)"
                        title="Reactivate this archived template">
                  Reactivate
                </button>
                <button class="btn-small btn-danger"
                        (click)="deleteTemplate(template)"
                        title="Delete this template">
                  Delete
                </button>
              </td>
            </tr>
            <tr *ngIf="templates.length === 0">
              <td colspan="6" class="no-data">
                No templates found. Click "New Template" to create one.
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
    
    .btn-reactivate {
      background: #34495e;
      color: white;
    }
    
    .btn-reactivate:hover {
      background: #2c3e50;
    }

    .drop-zone {
      border: 2px dashed #3498db;
      border-radius: 4px;
      padding: 15px;
      text-align: center;
      background: #fff;
      cursor: pointer;
      transition: background 0.2s;
      margin-top: 5px;
    }

    .drop-zone:hover, .drop-zone.dragover {
      background: #ebf5fb;
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
  templateVersions: any[] = []; // Placeholder for versions list
  selectedVersion: number | null = null;
  showCreateForm: boolean = false;
  editingTemplate: FieldMappingTemplate | null = null;
  error: string = '';
  success: string = '';

  newTemplate: CreateTemplateRequest = this.getEmptyCreateRequest();
  editRequest: UpdateTemplateRequest = { name: '', description: '', sourceSchema: '', targetSchema: '' };

  constructor(private apiService: ApiService) { }

  ngOnInit() {
    this.loadTemplates();
  }

  loadTemplates() {
    this.apiService.getTemplates().subscribe({
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
    this.selectedVersion = template.version;
    this.editRequest = {
      name: template.name,
      description: template.description,
      sourceSchema: template.sourceSchema ?? '',
      targetSchema: template.targetSchema ?? ''
    };
    this.clearMessages();

    // Load available versions
    this.loadTemplateVersions(template.id);

    setTimeout(() => {
      document.querySelector('.form-container')?.scrollIntoView({ behavior: 'smooth' });
    }, 100);
  }

  loadTemplateVersions(templateId: string) {
    this.apiService.getTemplateVersions(templateId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.templateVersions = response.data;
        }
      }
    });
  }

  onVersionSelect() {
    if (!this.editingTemplate || !this.selectedVersion) return;
    this.apiService.getTemplateById(this.editingTemplate.id, this.selectedVersion).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.editRequest.sourceSchema = response.data.sourceSchema ?? '';
          this.editRequest.targetSchema = response.data.targetSchema ?? '';
          // Optionally update other fields if they vary by version
        }
      }
    });
  }

  reactivateTemplate(template: FieldMappingTemplate) {
    if (!confirm(`Reactivate template "${template.name}"?`)) return;
    this.clearMessages();
    this.apiService.reactivateTemplate(template.id).subscribe({
      next: () => {
        this.success = `Template "${template.name}" reactivated.`;
        this.loadTemplates();
      },
      error: (err) => this.error = err.error?.message || 'Failed to reactivate'
    });
  }

  handleFileUploadSourceSchema(event: any, isEdit: boolean = false) {
    const file = event.target.files?.[0];
    if (!file) return;
    this.readSchemaFile(file, isEdit, 'source');
  }

  onFileDropSourceSchema(event: DragEvent, isEdit: boolean = false) {
    event.preventDefault();
    const file = event.dataTransfer?.files?.[0];
    if (!file) return;
    this.readSchemaFile(file, isEdit, 'source');
  }

  handleFileUploadTargetSchema(event: any, isEdit: boolean = false) {
    const file = event.target.files?.[0];
    if (!file) return;
    this.readSchemaFile(file, isEdit, 'target');
  }

  onFileDropTargetSchema(event: DragEvent, isEdit: boolean = false) {
    event.preventDefault();
    const file = event.dataTransfer?.files?.[0];
    if (!file) return;
    this.readSchemaFile(file, isEdit, 'target');
  }

  private readSchemaFile(file: File, isEdit: boolean, type: 'source' | 'target') {
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const content = e.target?.result as string;
        JSON.parse(content); // Validate JSON
        if (isEdit) {
          if (type === 'source') {
            this.editRequest.sourceSchema = content;
          } else {
            this.editRequest.targetSchema = content;
          }
        } else {
          if (type === 'source') {
            this.newTemplate.sourceSchema = content;
          } else {
            this.newTemplate.targetSchema = content;
          }
        }
      } catch (err) {
        this.error = `Invalid JSON file for ${type} schema`;
      }
    };
    reader.readAsText(file);
  }

  updateTemplate() {
    if (!this.editingTemplate) return;
    this.clearMessages();

    this.apiService.updateTemplate(this.editingTemplate.id, this.editRequest).subscribe({
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
    this.editRequest = { name: '', description: '', sourceSchema: '', targetSchema: '' };
  }

  duplicateTemplate(template: FieldMappingTemplate) {
    this.clearMessages();

    this.apiService.duplicateTemplate(template.id).subscribe({
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

    this.apiService.updateTemplate(template.id, publishRequest).subscribe({
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

    this.apiService.updateTemplate(template.id, archiveRequest).subscribe({
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

    this.apiService.deleteTemplate(template.id, template.version).subscribe({
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

  // getTmsSystemName removed

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'published': return 'badge-published';
      case 'archived': return 'badge-archived';
      default: return 'badge-draft';
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
    return { name: '', description: '', sourceSchema: '', targetSchema: '' };
  }
}

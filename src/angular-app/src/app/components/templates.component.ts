import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { FieldMappingTemplate, CreateTemplateRequest, UpdateTemplateRequest } from '../models/template.model';
import { FieldMappingsComponent } from './field-mappings.component';

type Screen = 'list' | 'detail' | 'version';

@Component({
  selector: 'app-templates',
  standalone: true,
  imports: [CommonModule, FormsModule, FieldMappingsComponent],
  template: `
    <div class="container">

      <!-- ==================== SCREEN: LIST ==================== -->
      <ng-container *ngIf="currentScreen === 'list'">
        <div class="page-header">
          <div>
            <h2>Templates</h2>
            <p class="page-subtitle">Manage transformation templates and their versions</p>
          </div>
          <button class="btn-primary" (click)="showCreateForm = !showCreateForm">
            {{ showCreateForm ? 'Cancel' : '＋ New Template' }}
          </button>
        </div>

        <div *ngIf="error" class="error">{{ error }}</div>
        <div *ngIf="success" class="success">{{ success }}</div>

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
                          placeholder="Describe what this template transforms"></textarea>
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Source Schema (JSON)</label>
                <textarea [(ngModel)]="newTemplate.sourceSchema" name="sourceSchema" rows="5"
                          class="json-textarea" placeholder='{ "field": "value", ... }'></textarea>
                <div class="drop-zone" (click)="fileInputCreateSource.click()" (dragover)="$event.preventDefault()" (drop)="onFileDropSourceSchema($event, false)">
                  <p>Drag &amp; Drop JSON file or <strong>Click to Upload</strong></p>
                  <input type="file" #fileInputCreateSource (change)="handleFileUploadSourceSchema($event, false)" accept=".json" style="display:none">
                </div>
              </div>
              <div class="form-group">
                <label>Target Schema (JSON)</label>
                <textarea [(ngModel)]="newTemplate.targetSchema" name="targetSchema" rows="5"
                          class="json-textarea" placeholder='{ "field": "value", ... }'></textarea>
                <div class="drop-zone" (click)="fileInputCreateTarget.click()" (dragover)="$event.preventDefault()" (drop)="onFileDropTargetSchema($event, false)">
                  <p>Drag &amp; Drop JSON file or <strong>Click to Upload</strong></p>
                  <input type="file" #fileInputCreateTarget (change)="handleFileUploadTargetSchema($event, false)" accept=".json" style="display:none">
                </div>
              </div>
            </div>
            <div class="form-actions">
              <button type="submit" class="btn-primary" [disabled]="!createForm.form.valid">Create</button>
              <button type="button" class="btn-secondary" (click)="showCreateForm = false">Cancel</button>
            </div>
          </form>
        </div>

        <!-- Templates Table -->
        <div class="table-container">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Status</th>
                <th>Latest Version</th>
                <th>Description</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let template of templates" class="clickable-row" (click)="openDetail(template)">
                <td>
                  <strong>{{ template.name }}</strong>
                  <br /><small class="muted">{{ template.id }}</small>
                </td>
                <td>
                  <span class="badge" [ngClass]="getStatusClass(template.status)">{{ template.status }}</span>
                </td>
                <td>
                  <span class="badge badge-version">v{{ template.version }}</span>
                  &nbsp;
                  <span class="badge" [ngClass]="getVersionStatusClass(template.latestVersionStatus)">
                    {{ template.latestVersionStatus || 'Draft' }}
                  </span>
                </td>
                <td class="description-cell">{{ template.description || '—' }}</td>
                <td>{{ formatDate(template.createdAt) }}</td>
                <td class="actions-cell" (click)="$event.stopPropagation()">
                  <button class="btn-small btn-danger" (click)="deleteTemplate(template)" title="Delete">Delete</button>
                </td>
              </tr>
              <tr *ngIf="templates.length === 0">
                <td colspan="6" class="no-data">No templates found. Click "New Template" to get started.</td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="total">{{ templates.length }} template(s) shown</div>
      </ng-container>

      <!-- ==================== SCREEN: DETAIL ==================== -->
      <ng-container *ngIf="currentScreen === 'detail' && selectedTemplate">
        <div class="page-header">
          <div class="breadcrumb">
            <button class="btn-link" (click)="goToList()">Templates</button>
            <span class="breadcrumb-sep">›</span>
            <span>{{ selectedTemplate.name }}</span>
          </div>
          <div class="header-actions">
            <button class="btn-small btn-info" (click)="startEdit(selectedTemplate)">Edit</button>
            <button class="btn-small btn-duplicate" (click)="duplicateTemplate(selectedTemplate)">Duplicate</button>
            <button class="btn-small btn-archive" *ngIf="selectedTemplate.status !== 'Archived'" (click)="archiveTemplate(selectedTemplate)">Archive</button>
            <button class="btn-small btn-reactivate" *ngIf="selectedTemplate.status === 'Archived'" (click)="reactivateTemplate(selectedTemplate)">Reactivate</button>
          </div>
        </div>

        <div *ngIf="error" class="error">{{ error }}</div>
        <div *ngIf="success" class="success">{{ success }}</div>

        <!-- Edit Form -->
        <div *ngIf="editingTemplate" class="form-container">
          <h3>Edit Template</h3>
          <form (ngSubmit)="updateTemplate()" #editForm="ngForm">
            <div class="form-row">
              <div class="form-group">
                <label>Name <span class="required">*</span></label>
                <input type="text" [(ngModel)]="editRequest.name" name="editName" required placeholder="Template name" />
              </div>
              <div class="form-group">
                <label>Description</label>
                <textarea [(ngModel)]="editRequest.description" name="editDescription" rows="2"></textarea>
              </div>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Source Schema (JSON)</label>
                <textarea [(ngModel)]="editRequest.sourceSchema" name="editSourceSchema" rows="5"
                          class="json-textarea" placeholder='{ "field": "value", ... }'></textarea>
                <div class="drop-zone" (click)="fileInputEditSource.click()" (dragover)="$event.preventDefault()" (drop)="onFileDropSourceSchema($event, true)">
                  <p>Drag &amp; Drop JSON file or <strong>Click to Upload</strong></p>
                  <input type="file" #fileInputEditSource (change)="handleFileUploadSourceSchema($event, true)" accept=".json" style="display:none">
                </div>
                <small>Used to auto-suggest source field paths in the Field Mappings screen.</small>
              </div>
              <div class="form-group">
                <label>Target Schema (JSON)</label>
                <textarea [(ngModel)]="editRequest.targetSchema" name="editTargetSchema" rows="5"
                          class="json-textarea" placeholder='{ "field": "value", ... }'></textarea>
                <div class="drop-zone" (click)="fileInputEditTarget.click()" (dragover)="$event.preventDefault()" (drop)="onFileDropTargetSchema($event, true)">
                  <p>Drag &amp; Drop JSON file or <strong>Click to Upload</strong></p>
                  <input type="file" #fileInputEditTarget (change)="handleFileUploadTargetSchema($event, true)" accept=".json" style="display:none">
                </div>
              </div>
            </div>
            <div class="form-actions">
              <button type="submit" class="btn-primary" [disabled]="!editForm.form.valid">Save Changes</button>
              <button type="button" class="btn-secondary" (click)="cancelEdit()">Cancel</button>
            </div>
          </form>
        </div>

        <!-- Template Info Card -->
        <div class="detail-card">
          <div class="detail-grid">
            <div class="detail-item">
              <label>ID</label>
              <span class="muted">{{ selectedTemplate.id }}</span>
            </div>
            <div class="detail-item">
              <label>Status</label>
              <span class="badge" [ngClass]="getStatusClass(selectedTemplate.status)">{{ selectedTemplate.status }}</span>
            </div>
            <div class="detail-item">
              <label>Description</label>
              <span>{{ selectedTemplate.description || '—' }}</span>
            </div>
            <div class="detail-item">
              <label>Created</label>
              <span>{{ formatDate(selectedTemplate.createdAt) }}</span>
            </div>
          </div>
        </div>

        <!-- Versions Section -->
        <div class="section-header">
          <h3>Versions</h3>
        </div>

        <div class="table-container">
          <table>
            <thead>
              <tr>
                <th>Version</th>
                <th>Status</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let v of templateVersions" class="clickable-row" (click)="openVersion(v)">
                <td><span class="badge badge-version">v{{ v.version }}</span></td>
                <td>
                  <span class="badge" [ngClass]="getVersionStatusClass(v.status)">{{ v.status }}</span>
                </td>
                <td>{{ formatDate(v.createdAt) }}</td>
                <td class="actions-cell" (click)="$event.stopPropagation()">
                  <!-- Draft Actions -->
                  <ng-container *ngIf="v.status === 'Draft' && selectedTemplate?.status !== 'Archived'">
                    <button class="btn-small btn-danger" 
                            *ngIf="templateVersions.length > 1"
                            (click)="deleteVersion(v)" 
                            title="Delete this draft">Delete</button>
                  </ng-container>

                  <!-- Other Actions -->
                  <button class="btn-small btn-secondary"
                          *ngIf="v.status !== 'Draft' && !hasAnyDraft() && selectedTemplate?.status !== 'Archived'"
                          (click)="createNewVersion(selectedTemplate!, v.version)"
                          title="Create a new draft based on this version">
                    Fork
                  </button>
                  
                  <button class="btn-small btn-info" (click)="openVersion(v)">View Mappings</button>
                </td>
              </tr>
              <tr *ngIf="templateVersions.length === 0">
                <td colspan="4" class="no-data">No versions found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </ng-container>

      <!-- ==================== SCREEN: VERSION ==================== -->
      <ng-container *ngIf="currentScreen === 'version' && selectedTemplate && selectedVersionObj">
        <div class="page-header">
          <div class="breadcrumb">
            <button class="btn-link" (click)="goToList()">Templates</button>
            <span class="breadcrumb-sep">›</span>
            <button class="btn-link" (click)="goToDetail()">{{ selectedTemplate.name }}</button>
            <span class="breadcrumb-sep">›</span>
            <span>v{{ selectedVersionObj.version }}</span>
          </div>
          <div class="header-actions">
            <span class="badge" [ngClass]="getVersionStatusClass(selectedVersionObj.status)">
              {{ selectedVersionObj.status }}
            </span>
            <button class="btn-small btn-publish"
                    *ngIf="selectedVersionObj.status === 'Draft' && selectedTemplate?.status !== 'Archived'"
                    (click)="publishVersion(selectedVersionObj)">
              Publish
            </button>
          </div>
        </div>

        <div *ngIf="error" class="error">{{ error }}</div>
        <div *ngIf="success" class="success">{{ success }}</div>

        <!-- Read-only notice -->
        <div *ngIf="isMappingReadonly()" class="readonly-notice">
          <span *ngIf="selectedTemplate.status === 'Archived'">🔒 This template is archived. Mappings are read-only.</span>
          <span *ngIf="selectedTemplate.status !== 'Archived' && selectedVersionObj.status !== 'Draft'">🔒 This version is published. Mappings are read-only.</span>
        </div>

        <!-- Field Mappings Sub-Component -->
        <app-field-mappings
          [templateId]="selectedTemplate.id"
          [templateVersionId]="selectedVersionObj.id"
          [templateName]="selectedTemplate.name + ' v' + selectedVersionObj.version"
          [sampleInputJson]="selectedTemplate.sampleInputJson"
          [isReadonly]="isMappingReadonly()">
        </app-field-mappings>
      </ng-container>

    </div>
  `,
  styles: [`
    .container {
      max-width: 1200px;
      margin: 0 auto;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .page-header h2 {
      margin: 0;
      color: #2c3e50;
    }

    .page-subtitle {
      margin: 4px 0 0 0;
      color: #7f8c8d;
      font-size: 13px;
    }

    .header-actions {
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .breadcrumb {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 15px;
    }

    .breadcrumb-sep {
      color: #bdc3c7;
    }

    .btn-link {
      background: none;
      border: none;
      color: #3498db;
      cursor: pointer;
      font-size: 15px;
      padding: 0;
      font-weight: 500;
    }

    .btn-link:hover {
      text-decoration: underline;
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin: 24px 0 12px 0;
    }

    .section-header h3 {
      margin: 0;
      color: #2c3e50;
    }

    .detail-card {
      background: #f8f9fa;
      border: 1px solid #e9ecef;
      border-radius: 6px;
      padding: 16px 20px;
      margin-bottom: 16px;
    }

    .detail-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }

    .detail-item label {
      display: block;
      font-size: 11px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: #95a5a6;
      font-weight: 600;
      margin-bottom: 4px;
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
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 12px;
      padding: 4px 10px;
      font-weight: 500;
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

    .btn-reactivate {
      background: #34495e;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 12px;
      padding: 4px 10px;
      font-weight: 500;
    }

    .btn-reactivate:hover {
      background: #2c3e50;
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

    .clickable-row {
      cursor: pointer;
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

    .readonly-notice {
      background: #fff8e1;
      border-left: 4px solid #f39c12;
      padding: 10px 15px;
      border-radius: 4px;
      margin-bottom: 16px;
      font-size: 13px;
      color: #7d5a00;
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
  // --- Screen State ---
  currentScreen: Screen = 'list';
  selectedTemplate: FieldMappingTemplate | null = null;
  selectedVersionObj: any = null;

  // --- List Screen ---
  templates: FieldMappingTemplate[] = [];
  showCreateForm: boolean = false;
  newTemplate: CreateTemplateRequest = this.getEmptyCreateRequest();

  // --- Detail Screen ---
  templateVersions: any[] = [];
  editingTemplate: FieldMappingTemplate | null = null;
  editRequest: UpdateTemplateRequest = this.getEmptyEditRequest();

  // --- Shared ---
  error: string = '';
  success: string = '';

  constructor(private apiService: ApiService) { }

  ngOnInit() {
    this.loadTemplates();
  }

  // ===== Navigation =====

  goToList() {
    this.currentScreen = 'list';
    this.selectedTemplate = null;
    this.selectedVersionObj = null;
    this.editingTemplate = null;
    this.clearMessages();
    this.loadTemplates();
  }

  openDetail(template: FieldMappingTemplate) {
    this.selectedTemplate = template;
    this.currentScreen = 'detail';
    this.clearMessages();
    this.loadTemplateVersions(template.id);
  }

  goToDetail() {
    if (!this.selectedTemplate) return;
    this.currentScreen = 'detail';
    this.selectedVersionObj = null;
    this.clearMessages();
    this.loadTemplateVersions(this.selectedTemplate.id);
  }

  openVersion(version: any) {
    this.selectedVersionObj = version;
    this.currentScreen = 'version';
    this.clearMessages();
  }

  // ===== Data Loading =====

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

  loadTemplateVersions(templateId: string) {
    this.apiService.getTemplateVersions(templateId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.templateVersions = response.data;
        }
      },
      error: (err) => {
        console.error('Failed to load versions', err);
      }
    });
  }

  // ===== Actions =====

  createTemplate() {
    this.clearMessages();
    this.apiService.createTemplate(this.newTemplate).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `Template "${response.data?.name}" created successfully.`;
          this.showCreateForm = false;
          this.newTemplate = this.getEmptyCreateRequest();
          this.loadTemplates();
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to create template';
      }
    });
  }

  startEdit(template: FieldMappingTemplate) {
    this.editingTemplate = template;
    this.editRequest = {
      name: template.name,
      description: template.description,
      sourceSchema: template.sourceSchema ?? '',
      targetSchema: template.targetSchema ?? ''
    };
    this.clearMessages();
  }

  cancelEdit() {
    this.editingTemplate = null;
    this.editRequest = this.getEmptyEditRequest();
  }

  updateTemplate() {
    if (!this.editingTemplate) return;
    this.clearMessages();
    this.apiService.updateTemplate(this.editingTemplate.id, this.editRequest).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `Template updated.`;
          this.cancelEdit();
          // Refresh selected template data
          this.apiService.getTemplateById(this.editingTemplate?.id ?? this.selectedTemplate!.id).subscribe({
            next: (r) => {
              if (r.success && r.data) {
                this.selectedTemplate = r.data;
              }
            }
          });
          this.loadTemplates();
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to update template';
      }
    });
  }

  duplicateTemplate(template: FieldMappingTemplate) {
    this.clearMessages();
    this.apiService.duplicateTemplate(template.id).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `Template "${response.data?.name}" created as a copy.`;
          this.loadTemplates();
        }
      },
      error: (err) => this.error = err.error?.message || 'Failed to duplicate template'
    });
  }

  hasAnyDraft(): boolean {
    return this.templateVersions.some(v => v.status === 'Draft');
  }

  createNewVersion(template: FieldMappingTemplate, baseVersion?: number) {
    const msg = baseVersion
      ? `Create a new draft version for "${template.name}" based on v${baseVersion}?`
      : `Create a new draft version for "${template.name}" based on the latest published version?`;

    if (!confirm(msg)) return;

    this.clearMessages();
    this.apiService.createTemplateVersion(template.id, baseVersion).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `New draft version created.`;
          this.loadTemplateVersions(template.id);
        }
      },
      error: (err) => this.error = err.error?.message || 'Failed to create new version'
    });
  }

  isMappingReadonly(): boolean {
    if (!this.selectedTemplate || !this.selectedVersionObj) return true;
    // Lock if template is archived OR the version is not a Draft (i.e. Published = superseded or current published)
    return this.selectedTemplate.status === 'Archived' || this.selectedVersionObj.status !== 'Draft';
  }

  publishVersion(version: any) {
    if (!this.selectedTemplate) return;
    if (this.selectedTemplate.status === 'Archived') {
      this.error = 'Cannot publish a version of an archived template.';
      return;
    }
    if (!confirm(`Publish version ${version.version} of "${this.selectedTemplate.name}"?`)) return;
    this.clearMessages();
    this.apiService.publishTemplateVersion(this.selectedTemplate.id, version.version).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `Version ${version.version} published successfully.`;
          version.status = 'Published';
          this.selectedVersionObj = { ...version, status: 'Published' };
          this.loadTemplateVersions(this.selectedTemplate!.id);
          this.loadTemplates();
        }
      },
      error: (err) => this.error = err.error?.message || 'Failed to publish version'
    });
  }

  deleteVersion(version: any) {
    if (!this.selectedTemplate) return;
    if (!confirm(`Are you sure you want to delete version ${version.version} draft? This will also delete all its field mappings.`)) return;

    this.clearMessages();
    this.apiService.deleteTemplateVersion(this.selectedTemplate.id, version.version).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `Version ${version.version} deleted.`;
          this.loadTemplateVersions(this.selectedTemplate!.id);
        }
      },
      error: (err) => this.error = err.error?.message || 'Failed to delete version'
    });
  }

  archiveTemplate(template: FieldMappingTemplate) {
    if (!confirm(`Archive template "${template.name}"?`)) return;
    this.clearMessages();
    this.apiService.updateTemplate(template.id, { name: template.name, description: template.description, status: 'Archived' }).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = `Template "${template.name}" archived.`;
          if (this.selectedTemplate?.id === template.id) {
            this.selectedTemplate = { ...template, status: 'Archived' };
          }
          this.loadTemplates();
        }
      },
      error: (err) => this.error = err.error?.message || 'Failed to archive template'
    });
  }

  reactivateTemplate(template: FieldMappingTemplate) {
    if (!confirm(`Reactivate template "${template.name}"?`)) return;
    this.clearMessages();
    this.apiService.reactivateTemplate(template.id).subscribe({
      next: () => {
        this.success = `Template "${template.name}" reactivated.`;
        if (this.selectedTemplate?.id === template.id) {
          this.selectedTemplate = { ...template, status: 'Active' };
        }
        this.loadTemplates();
      },
      error: (err) => this.error = err.error?.message || 'Failed to reactivate'
    });
  }

  deleteTemplate(template: FieldMappingTemplate) {
    if (!confirm(`Delete template "${template.name}"? This cannot be undone.`)) return;
    this.clearMessages();
    this.apiService.deleteTemplate(template.id, template.version).subscribe({
      next: () => {
        this.success = `Template "${template.name}" deleted.`;
        this.loadTemplates();
      },
      error: (err) => this.error = err.error?.message || 'Failed to delete template'
    });
  }

  // ===== File Upload =====

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
        JSON.parse(content);
        if (isEdit) {
          if (type === 'source') this.editRequest.sourceSchema = content;
          else this.editRequest.targetSchema = content;
        } else {
          if (type === 'source') this.newTemplate.sourceSchema = content;
          else this.newTemplate.targetSchema = content;
        }
      } catch {
        this.error = `Invalid JSON file for ${type} schema`;
      }
    };
    reader.readAsText(file);
  }

  // ===== Helpers =====

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'active': return 'badge-published';
      case 'archived': return 'badge-archived';
      default: return 'badge-draft';
    }
  }

  getVersionStatusClass(status?: string): string {
    switch (status?.toLowerCase()) {
      case 'published': return 'badge-published';
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

  private getEmptyEditRequest(): UpdateTemplateRequest {
    return { name: '', description: '', sourceSchema: '', targetSchema: '' };
  }
}

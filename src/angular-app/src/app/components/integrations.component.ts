import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { GeneralService } from '../services/general.service';
import { ApiClient, CreateApiClientRequest, UpdateApiClientRequest } from '../models/api-client.model';
import { TemplateVersionResponse } from '../models/template.model';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-integrations',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container">
      <div *ngIf="error" class="error">{{ error }}</div>

      <!-- ==================== SCREEN: LIST ==================== -->
      <ng-container *ngIf="!selectedClient">
        <div class="page-header">
          <div>
            <h2>Integrations</h2>
            <p class="page-subtitle">Manage API clients and their template assignments</p>
          </div>
          <button class="btn-primary" (click)="showCreateForm = !showCreateForm">
            {{ showCreateForm ? 'Cancel' : '＋ New API Client' }}
          </button>
        </div>

        <div *ngIf="showCreateForm" class="form-container">
          <h3>Create API Client</h3>
          <form (ngSubmit)="createClient()" #createForm="ngForm">
            <div class="form-row">
              <div class="form-group">
                <label for="clientName">Name <span class="required">*</span></label>
                <input
                  id="clientName"
                  type="text"
                  [(ngModel)]="newClient.name"
                  name="name"
                  required
                  placeholder="e.g., MyApp Integration"
                />
              </div>
              <div class="form-group">
                <label for="clientDescription">Description</label>
                <textarea
                  id="clientDescription"
                  [(ngModel)]="newClient.description"
                  name="description"
                  rows="2"
                  placeholder="Describe this API client"
                ></textarea>
              </div>
            </div>
            <div class="form-actions">
              <button type="submit" class="btn-primary" [disabled]="!createForm.form.valid">Create</button>
              <button type="button" class="btn-secondary" (click)="showCreateForm = false">Cancel</button>
            </div>
          </form>
        </div>

        <div class="table-container">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Status</th>
                <th>Description</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let client of clients" class="clickable-row" (click)="selectClient(client)">
                <td>
                  <strong>{{ client.name }}</strong>
                  <br /><small class="muted">{{ client.id }}</small>
                </td>
                <td>
                  <span class="badge" [class.badge-active]="client.isActive" [class.badge-inactive]="!client.isActive">
                    {{ client.isActive ? 'Active' : 'Inactive' }}
                  </span>
                </td>
                <td class="description-cell">{{ client.description || '—' }}</td>
                <td class="actions-cell" (click)="$event.stopPropagation()">
                  <button class="btn-small btn-info" (click)="selectClient(client)">View</button>
                  <button class="btn-small btn-danger" (click)="deleteClient(client.id)">Delete</button>
                </td>
              </tr>
              <tr *ngIf="clients.length === 0 && !loading">
                <td colspan="4" class="no-data">No API clients found. Click "New API Client" to get started.</td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="total">{{ clients.length }} client(s) shown</div>
      </ng-container>

      <!-- ==================== SCREEN: DETAIL ==================== -->
      <ng-container *ngIf="selectedClient">
        <div class="page-header">
          <div class="breadcrumb">
            <button class="btn-link" (click)="selectedClient = null">Integrations</button>
            <span class="breadcrumb-sep">›</span>
            <span>{{ selectedClient.name }}</span>
          </div>
          <div class="header-actions">
            <button class="btn-small btn-info" (click)="startEdit()">Edit</button>
            <button *ngIf="selectedClient.isActive" class="btn-small btn-archive" (click)="toggleClientActive(false)">
              Deactivate
            </button>
            <button
              *ngIf="!selectedClient.isActive"
              class="btn-small btn-reactivate"
              (click)="toggleClientActive(true)"
            >
              Reactivate
            </button>
          </div>
        </div>

        <!-- Edit Form -->
        <div *ngIf="editingClient" class="form-container">
          <h3>Edit API Client</h3>
          <form (ngSubmit)="saveClientEdit()" #editClientForm="ngForm">
            <div class="form-row">
              <div class="form-group">
                <label for="editClientName">Name <span class="required">*</span></label>
                <input
                  id="editClientName"
                  type="text"
                  [(ngModel)]="editRequest.name"
                  name="editClientName"
                  required
                  placeholder="Client name"
                />
              </div>
              <div class="form-group">
                <label for="editClientDescription">Description</label>
                <textarea
                  id="editClientDescription"
                  [(ngModel)]="editRequest.description"
                  name="editClientDescription"
                  rows="2"
                  placeholder="Optional description"
                ></textarea>
              </div>
            </div>
            <div class="form-actions">
              <button type="submit" class="btn-primary" [disabled]="!editClientForm.form.valid">Save Changes</button>
              <button type="button" class="btn-secondary" (click)="cancelEdit()">Cancel</button>
            </div>
          </form>
        </div>

        <!-- Client Info Card -->
        <div class="detail-card">
          <div class="detail-grid">
            <div class="detail-item">
              <span class="detail-label">ID</span>
              <span class="muted">{{ selectedClient.id }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Status</span>
              <span
                class="badge"
                style="width: fit-content"
                [class.badge-active]="selectedClient.isActive"
                [class.badge-inactive]="!selectedClient.isActive"
              >
                {{ selectedClient.isActive ? 'Active' : 'Inactive' }}
              </span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Description</span>
              <span>{{ selectedClient.description || '—' }}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Created</span>
              <span>{{ formatDate(selectedClient.createdAt) }}</span>
            </div>
          </div>
        </div>

        <div class="assignment-actions">
          <h4>Assign Template Version</h4>
          <div class="assignment-form">
            <select [(ngModel)]="selectedTemplateId" name="templateSelect" class="form-control">
              <option [ngValue]="null">Select a template version...</option>
              <ng-container *ngFor="let group of availableTemplatesGrouped">
                <optgroup *ngIf="getFilteredVersions(group.versions).length > 0" [label]="group.name">
                  <ng-container *ngFor="let version of getFilteredVersions(group.versions)">
                    <option [value]="version.id">V{{ version.version }} ({{ version.status }})</option>
                  </ng-container>
                </optgroup>
              </ng-container>
            </select>
            <button (click)="assignTemplate()" class="btn-primary" [disabled]="!selectedTemplateId">Assign</button>
          </div>
        </div>

        <div class="section-header">
          <h3>Assigned Templates</h3>
        </div>

        <div class="table-container">
          <table>
            <thead>
              <tr>
                <th>Template</th>
                <th>Template Status</th>
                <th>Version</th>
                <th>Version Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let template of assignedTemplates">
                <td>
                  <strong>{{ template.templateName }}</strong>
                </td>
                <td>
                  <span class="badge" [ngClass]="getTemplateStatusClass(template.templateStatus)">
                    {{ template.templateStatus }}
                  </span>
                </td>
                <td>
                  <span class="badge badge-version">v{{ template.version }}</span>
                </td>
                <td>
                  <span class="badge" [ngClass]="getVersionStatusClass(template.status)">{{ template.status }}</span>
                </td>
                <td class="actions-cell">
                  <button
                    (click)="selectedApiDetails = template"
                    class="btn-small btn-info"
                    [disabled]="template.templateStatus !== 'Active'"
                    [title]="template.templateStatus !== 'Active' ? 'Only active templates can show API details' : ''"
                  >
                    API Access Details
                  </button>
                  <button (click)="removeTemplate(template.id)" class="btn-small btn-danger">Remove</button>
                </td>
              </tr>
              <tr *ngIf="assignedTemplates.length === 0">
                <td colspan="5" class="no-data">No templates assigned to this client.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </ng-container>

      <!-- API Details Modal Overlay -->
      <div
        class="modal-overlay"
        *ngIf="selectedApiDetails"
        (click)="selectedApiDetails = null"
        tabindex="0"
        role="button"
        (keydown.enter)="selectedApiDetails = null"
        aria-label="Close modal"
      >
        <div
          class="modal-container"
          (click)="$event.stopPropagation()"
          tabindex="0"
          role="document"
          (keydown.enter)="$event.stopPropagation()"
        >
          <div class="modal-header">
            <h4>API Details — {{ selectedApiDetails.templateName }} V{{ selectedApiDetails.version }}</h4>
            <button class="btn-close-modal" (click)="selectedApiDetails = null" aria-label="Close API Details">
              &times;
            </button>
          </div>

          <div class="modal-body">
            <div class="snippet-section">
              <h5>Required Header</h5>
              <div class="snippet-box">
                <button class="btn-copy" (click)="copySnippet('x-client-id: ' + selectedClient!.id)">Copy</button>
                <pre><code>x-client-id: {{ selectedClient!.id }}</code></pre>
              </div>
            </div>

            <div class="snippet-section">
              <h5>Transformation API</h5>
              <div class="http-route"><strong>POST</strong> {{ apiUrl }}/transform</div>
              <div class="snippet-box">
                <button class="btn-copy" (click)="copySnippet(getTransformPayload(selectedApiDetails))">
                  Copy JSON
                </button>
                <pre><code>{{ getTransformPayload(selectedApiDetails) }}</code></pre>
              </div>
            </div>

            <div class="snippet-section">
              <h5>Preview / Validate API</h5>
              <div class="http-route"><strong>POST</strong> {{ apiUrl }}/transform/preview</div>
              <div class="snippet-box">
                <button class="btn-copy" (click)="copySnippet(getTransformPayload(selectedApiDetails))">
                  Copy JSON
                </button>
                <pre><code>{{ getTransformPayload(selectedApiDetails) }}</code></pre>
              </div>
            </div>
          </div>
        </div>
      </div>

      <p *ngIf="loading">Loading...</p>
    </div>
  `,
  styles: [
    `
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

      .breadcrumb {
        display: flex;
        align-items: center;
        gap: 15px;
        margin-bottom: 25px;
        padding: 14px 22px;
        background: #f8fafc;
        border-radius: 12px;
        font-size: 17px;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
        border-left: 8px solid #3498db;
      }

      .breadcrumb button.btn-link {
        color: #3498db;
        text-decoration: none;
        font-weight: 800;
        padding: 0;
        border: none;
        background: transparent;
        cursor: pointer;
        transition: all 0.2s ease;
      }

      .breadcrumb button.btn-link:hover {
        color: #1d4e89;
        transform: scale(1.02);
      }

      .breadcrumb .breadcrumb-sep {
        color: #cbd5e1;
        font-weight: 200;
        font-size: 22px;
      }

      .breadcrumb span:last-child {
        color: #1e293b;
        font-weight: 800;
        padding: 4px 12px;
        border-radius: 6px;
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

      .form-group input,
      .form-group textarea,
      .form-control {
        width: 100%;
        padding: 8px;
        border: 1px solid #ddd;
        border-radius: 4px;
        font-family: inherit;
        font-size: 14px;
        box-sizing: border-box;
      }

      .required {
        color: #e74c3c;
      }

      .form-actions {
        display: flex;
        gap: 10px;
        margin-top: 20px;
      }

      .assignment-actions {
        background: #f8f9fa;
        padding: 16px 20px;
        border-radius: 4px;
        margin-bottom: 20px;
        border-left: 4px solid #3498db;
      }

      .assignment-actions h4 {
        margin: 0 0 12px 0;
        color: #2c3e50;
        font-size: 14px;
        font-weight: 600;
      }

      .assignment-form {
        display: flex;
        gap: 10px;
        align-items: center;
      }

      .assignment-form select {
        flex: 1;
      }

      .btn-primary,
      .btn-secondary,
      .btn-small,
      .btn-danger,
      .btn-info,
      .btn-archive,
      .btn-reactivate {
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

      .btn-info:hover:not(:disabled) {
        background: #2980b9;
      }

      .btn-info:disabled {
        background: #cbd5e1;
        cursor: not-allowed;
        color: #64748b;
      }

      .btn-danger {
        background: #e74c3c;
        color: white;
      }

      .btn-danger:hover {
        background: #c0392b;
      }

      .btn-archive {
        background: #f39c12;
        color: white;
      }

      .btn-archive:hover {
        background: #e67e22;
      }

      .btn-reactivate {
        background: #34495e;
        color: white;
      }

      .btn-reactivate:hover {
        background: #2c3e50;
      }

      .header-actions {
        display: flex;
        gap: 8px;
        align-items: center;
      }

      .detail-card {
        background: white;
        border-radius: 6px;
        padding: 20px;
        margin-bottom: 20px;
        box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
      }

      .detail-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 16px 32px;
      }

      .detail-item {
        display: flex;
        flex-direction: column;
        gap: 4px;
      }

      .detail-label {
        font-size: 11px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: #95a5a6;
      }

      .muted {
        color: #7f8c8d;
        font-size: 13px;
      }

      .error {
        background: #fee;
        color: #c33;
        padding: 10px 15px;
        border-radius: 4px;
        margin-bottom: 15px;
        border-left: 4px solid #e74c3c;
      }

      .table-container {
        overflow-x: auto;
      }

      table {
        width: 100%;
        border-collapse: collapse;
        background: white;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      }

      th,
      td {
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

      .badge-active {
        background: #27ae60;
        color: white;
      }

      .badge-inactive {
        background: #95a5a6;
        color: white;
      }

      .badge-draft {
        background: #f39c12;
        color: white;
      }

      .badge-published {
        background: #27ae60;
        color: white;
      }

      .badge-superseded {
        background: #8e44ad;
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

      /* Modal */
      .modal-overlay {
        position: fixed;
        top: 0;
        left: 0;
        width: 100vw;
        height: 100vh;
        background: rgba(0, 0, 0, 0.5);
        display: flex;
        justify-content: center;
        align-items: center;
        z-index: 1050;
      }

      .modal-container {
        background: #fff;
        width: 100%;
        max-width: 700px;
        border-radius: 8px;
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        display: flex;
        flex-direction: column;
        max-height: 90vh;
      }

      .modal-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 16px 20px;
        background: #34495e;
        border-radius: 8px 8px 0 0;
      }

      .modal-header h4 {
        margin: 0;
        font-size: 1.1rem;
        color: white;
      }

      .btn-close-modal {
        background: transparent;
        border: none;
        font-size: 1.5rem;
        line-height: 1;
        cursor: pointer;
        opacity: 0.7;
        padding: 0;
        margin: 0;
        color: white;
      }

      .btn-close-modal:hover {
        opacity: 1;
      }

      .modal-body {
        padding: 20px;
        overflow-y: auto;
      }

      .snippet-section {
        margin-bottom: 20px;
      }

      .snippet-section h5 {
        margin: 0 0 10px 0;
        font-size: 11px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.5px;
        color: #95a5a6;
      }

      .http-route {
        background: #e9ecef;
        padding: 8px 12px;
        border-radius: 4px;
        font-family: monospace;
        font-size: 14px;
        margin-bottom: 10px;
        display: inline-block;
      }

      .snippet-box {
        position: relative;
        background: #2c3e50;
        color: #f8f9fa;
        border-radius: 6px;
        padding: 15px;
        overflow-x: auto;
      }

      .snippet-box pre {
        margin: 0;
        font-family: 'Courier New', Courier, monospace;
        font-size: 13px;
      }

      .btn-copy {
        position: absolute;
        top: 10px;
        right: 10px;
        background: #495057;
        color: white;
        border: none;
        padding: 4px 8px;
        border-radius: 4px;
        font-size: 11px;
        cursor: pointer;
        opacity: 0.8;
      }

      .btn-copy:hover {
        opacity: 1;
        background: #6c757d;
      }
    `,
  ],
})
export class IntegrationsComponent implements OnInit {
  clients: ApiClient[] = [];
  loading = false;
  error: string | null = null;
  showCreateForm = false;
  newClient: CreateApiClientRequest = {
    name: '',
    description: '',
    isActive: true,
  };

  selectedClient: ApiClient | null = null;
  editingClient = false;
  editRequest: UpdateApiClientRequest = { name: '', description: '', isActive: true };
  assignedTemplates: TemplateVersionResponse[] = [];
  availableTemplatesGrouped: { id: string; name: string; versions: TemplateVersionResponse[] }[] = [];
  selectedTemplateId: string | null = null;
  selectedApiDetails: TemplateVersionResponse | null = null;

  get apiUrl(): string {
    return environment.apiUrl;
  }

  constructor(
    private apiService: ApiService,
    private generalService: GeneralService,
  ) {}

  @HostListener('window:keydown.escape', ['$event'])
  handleKeyDown(_event: KeyboardEvent) {
    if (this.selectedApiDetails) {
      this.selectedApiDetails = null;
    }
  }

  ngOnInit() {
    this.loadClients();
    this.loadAvailableTemplates();
  }

  loadClients() {
    this.loading = true;
    this.apiService.getApiClients().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.clients = response.data.apiClients;
        }
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load API clients';
        this.loading = false;
        console.error(err);
      },
    });
  }

  loadAvailableTemplates() {
    this.apiService.getTemplates().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const templates = response.data.templates;
          this.availableTemplatesGrouped = [];

          templates.forEach((t) => {
            if (t.status === 'Archived') return;

            this.apiService.getTemplateVersions(t.id).subscribe({
              next: (vResponse) => {
                if (vResponse.success && vResponse.data) {
                  // Only allow Published or Superseded statuses
                  const validVersions = vResponse.data.filter(
                    (v) => v.status === 'Published' || v.status === 'Superseded',
                  );
                  if (validVersions.length > 0) {
                    this.availableTemplatesGrouped.push({
                      id: t.id,
                      name: t.name,
                      versions: validVersions,
                    });
                    this.availableTemplatesGrouped.sort((a, b) => a.name.localeCompare(b.name));
                  }
                }
              },
            });
          });
        }
      },
    });
  }

  selectClient(client: ApiClient) {
    this.selectedClient = client;
    this.editingClient = false;
    this.loadAssignedTemplates(client.id);
  }

  startEdit() {
    if (!this.selectedClient) return;
    this.editRequest = {
      name: this.selectedClient.name,
      description: this.selectedClient.description ?? '',
      isActive: this.selectedClient.isActive,
    };
    this.editingClient = true;
  }

  cancelEdit() {
    this.editingClient = false;
  }

  saveClientEdit() {
    if (!this.selectedClient) return;
    this.apiService.updateApiClient(this.selectedClient.id, this.editRequest).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.selectedClient = response.data;
          this.editingClient = false;
          this.generalService.success('API client updated successfully');
          this.loadClients();
        }
      },
      error: (err) => {
        const msg = typeof err.error === 'string' ? err.error : err.error?.message || 'Failed to update API client';
        this.generalService.error(msg);
      },
    });
  }

  loadAssignedTemplates(clientId: string) {
    this.apiService.getAssignedTemplates(clientId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.assignedTemplates = response.data.sort((a, b) =>
            (a.templateName ?? '').localeCompare(b.templateName ?? ''),
          );
        }
      },
    });
  }

  createClient() {
    this.apiService.createApiClient(this.newClient).subscribe({
      next: (response) => {
        if (response.success) {
          this.showCreateForm = false;
          this.newClient = { name: '', description: '', isActive: true };
          this.loadClients();
        }
      },
      error: (err) => {
        const msg = typeof err.error === 'string' ? err.error : err.error?.message || 'Failed to create API client';
        this.generalService.error(msg);
      },
    });
  }

  deleteClient(id: string) {
    this.generalService
      .confirm({
        title: 'Delete API Client',
        text: 'Are you sure you want to delete this API client?',
        confirmText: 'Yes, Delete',
        confirmColor: '#dc3545',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.apiService.deleteApiClient(id).subscribe({
          next: () => {
            this.generalService.success('API client deleted');
            if (this.selectedClient?.id === id) this.selectedClient = null;
            this.loadClients();
          },
        });
      });
  }

  toggleClientActive(activate: boolean) {
    if (!this.selectedClient) return;
    const client = this.selectedClient;
    const request = {
      name: client.name,
      description: client.description,
      isActive: activate,
    };
    this.apiService.updateApiClient(client.id, request).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.selectedClient = { ...client, isActive: activate };
          this.generalService.success(activate ? 'API client reactivated' : 'API client deactivated');
          this.loadClients();
        }
      },
      error: () => this.generalService.error('Failed to update API client status'),
    });
  }

  assignTemplate() {
    if (!this.selectedClient || !this.selectedTemplateId) return;

    this.apiService.assignTemplate(this.selectedClient.id, this.selectedTemplateId).subscribe({
      next: () => {
        this.generalService.success('Template assigned successfully');
        this.selectedTemplateId = null;
        this.loadAssignedTemplates(this.selectedClient!.id);
      },
      error: (err) => {
        const msg = typeof err.error === 'string' ? err.error : err.error?.message || 'Failed to assign template';
        this.generalService.error(msg);
      },
    });
  }

  removeTemplate(templateVersionId: string) {
    if (!this.selectedClient) return;

    this.apiService.removeTemplate(this.selectedClient.id, templateVersionId).subscribe({
      next: () => {
        this.generalService.success('Template assignment removed');
        if (this.selectedApiDetails?.id === templateVersionId) {
          this.selectedApiDetails = null;
        }
        this.loadAssignedTemplates(this.selectedClient!.id);
      },
    });
  }

  getFilteredVersions(versions: TemplateVersionResponse[]): TemplateVersionResponse[] {
    if (!this.assignedTemplates || this.assignedTemplates.length === 0) return versions;
    return versions.filter((v) => !this.assignedTemplates.some((at) => at.id === v.id));
  }

  getVersionStatusClass(status: string | undefined): string {
    switch (status) {
      case 'Draft':
        return 'badge-draft';
      case 'Published':
        return 'badge-published';
      case 'Superseded':
        return 'badge-superseded';
      case 'Archived':
        return 'badge-archived';
      default:
        return '';
    }
  }

  getTemplateStatusClass(status: string | undefined): string {
    switch (status) {
      case 'Active':
        return 'badge-active';
      case 'Draft':
        return 'badge-draft';
      case 'Archived':
        return 'badge-archived';
      default:
        return '';
    }
  }

  formatDate(dateStr: string | undefined): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'numeric',
      day: 'numeric',
    });
  }

  getTransformPayload(template: TemplateVersionResponse): string {
    const payload = {
      templateId: template.templateId,
      version: template.version,
      sourceJson: '{ "your": "data" }',
    };
    return JSON.stringify(payload, null, 2);
  }

  copySnippet(text: string) {
    navigator.clipboard.writeText(text).then(
      () => this.generalService.success('Copied to clipboard!'),
      () => this.generalService.error('Failed to copy to clipboard'),
    );
  }
}

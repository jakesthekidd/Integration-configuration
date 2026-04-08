import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { GeneralService } from '../services/general.service';
import { ApiClient, CreateApiClientRequest } from '../models/api-client.model';
import { TemplateVersionResponse } from '../models/template.model';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-integrations',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container">
      <h2>Integrations</h2>

      <div *ngIf="error" class="error">{{ error }}</div>

      <div class="split-view">
        <!-- Screen 1: Master List View -->
        <div class="list-panel" *ngIf="!selectedClient">
          <div class="actions">
            <button (click)="showCreateForm = !showCreateForm" class="btn btn-primary">
              {{ showCreateForm ? 'Cancel' : 'Create New API Client' }}
            </button>
          </div>

          <div *ngIf="showCreateForm" class="form-container">
            <h3>Create API Client</h3>
            <form (ngSubmit)="createClient()">
              <div class="form-group">
                <label for="clientName">Name:</label>
                <input id="clientName" type="text" [(ngModel)]="newClient.name" name="name" required />
              </div>
              <div class="form-group">
                <label for="clientDescription">Description:</label>
                <textarea id="clientDescription" [(ngModel)]="newClient.description" name="description"></textarea>
              </div>
              <button type="submit" class="btn btn-success">Create</button>
            </form>
          </div>

          <h3>API Clients</h3>
          <table class="data-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let client of clients" (click)="selectClient(client)">
                <td>{{ client.name }}</td>
                <td>
                  <span [class.active]="client.isActive" [class.inactive]="!client.isActive">
                    {{ client.isActive ? 'Active' : 'Inactive' }}
                  </span>
                </td>
                <td>
                  <button (click)="deleteClient(client.id); $event.stopPropagation()" class="btn btn-danger btn-sm">
                    Delete
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
          <p *ngIf="clients.length === 0 && !loading">No API clients found.</p>
        </div>

        <!-- Screen 2: Details View -->
        <div class="detail-panel" *ngIf="selectedClient">
          <div
            class="details-header"
            style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px;"
          >
            <h3>Assigned Templates for {{ selectedClient.name }}</h3>
            <button (click)="selectedClient = null" class="btn btn-secondary">Back to Clients</button>
          </div>

          <div class="assignment-actions">
            <h4>Assign New Template</h4>
            <div class="assignment-form">
              <select [(ngModel)]="selectedTemplateId" name="templateSelect" class="form-control">
                <option [ngValue]="null">Select a template version...</option>
                <optgroup *ngFor="let group of availableTemplatesGrouped" [label]="group.name">
                  <option *ngFor="let version of group.versions" [value]="version.id">
                    V{{ version.version }} ({{ version.status }})
                  </option>
                </optgroup>
              </select>
              <button (click)="assignTemplate()" class="btn btn-primary" [disabled]="!selectedTemplateId">
                Assign
              </button>
            </div>
          </div>

          <table class="data-table" *ngIf="assignedTemplates.length > 0">
            <thead>
              <tr>
                <th>Template</th>
                <th>Version</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let template of assignedTemplates">
                <td>{{ template.templateName }}</td>
                <td>V{{ template.version }}</td>
                <td>{{ template.status }}</td>
                <td>
                  <button
                    (click)="selectedApiDetails = template"
                    class="btn btn-secondary btn-sm"
                    style="margin-right: 8px;"
                  >
                    API Details
                  </button>
                  <button (click)="removeTemplate(template.id)" class="btn btn-danger btn-sm">Remove</button>
                </td>
              </tr>
            </tbody>
          </table>
          <p *ngIf="assignedTemplates.length === 0">No templates assigned to this client.</p>
        </div>
      </div>

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
            <h4>API Details for {{ selectedApiDetails.templateName }} V{{ selectedApiDetails.version }}</h4>
            <button class="btn-close-modal" (click)="selectedApiDetails = null" aria-label="Close API Details">
              &times;
            </button>
          </div>

          <div class="modal-body">
            <div class="snippet-section">
              <h5>Required Header</h5>
              <div class="snippet-box">
                <button class="btn-copy" (click)="copySnippet('x-client-id: ' + selectedClient!.id)">
                  Copy
                </button>
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
        padding: 20px;
        max-width: 1400px;
        margin: 0 auto;
      }
      .actions {
        margin-bottom: 20px;
      }
      .form-container {
        background: #f8f9fa;
        padding: 20px;
        border-radius: 8px;
        margin-bottom: 20px;
        border: 1px solid #dee2e6;
      }
      .form-group {
        margin-bottom: 15px;
      }
      .form-group label {
        display: block;
        margin-bottom: 5px;
        font-weight: bold;
      }
      .form-group input,
      .form-group textarea,
      .form-control {
        width: 100%;
        padding: 8px;
        border: 1px solid #ced4da;
        border-radius: 4px;
      }
      .btn {
        padding: 8px 16px;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 14px;
      }
      .btn-primary {
        background: #007bff;
        color: white;
      }
      .btn-secondary {
        background: #6c757d;
        color: white;
      }
      .btn-success {
        background: #28a745;
        color: white;
      }
      .btn-danger {
        background: #dc3545;
        color: white;
      }
      .btn-sm {
        padding: 4px 8px;
        font-size: 12px;
      }
      .split-view {
        display: flex;
        gap: 20px;
        margin-top: 20px;
      }
      .list-panel {
        flex: 1;
        min-width: 400px;
      }
      .detail-panel {
        flex: 1.5;
        background: #fff;
        padding: 20px;
        border: 1px solid #dee2e6;
        border-radius: 8px;
      }
      .detail-panel.empty {
        display: flex;
        align-items: center;
        justify-content: center;
        color: #6c757d;
        font-style: italic;
      }
      .data-table {
        width: 100%;
        border-collapse: collapse;
      }
      .data-table th,
      .data-table td {
        padding: 12px;
        text-align: left;
        border-bottom: 1px solid #dee2e6;
      }
      .data-table th {
        background: #f8f9fa;
      }
      .data-table tr:hover {
        background: #f1f3f5;
        cursor: pointer;
      }
      .data-table tr.selected {
        background: #e7f1ff;
      }
      .active {
        color: #28a745;
        font-weight: bold;
      }
      .inactive {
        color: #dc3545;
      }
      .error {
        color: #721c24;
        background: #f8d7da;
        border: 1px solid #f5c6cb;
        padding: 10px;
        border-radius: 4px;
        margin-bottom: 20px;
      }
      .assignment-actions {
        background: #f8f9fa;
        padding: 15px;
        border-radius: 4px;
        margin-bottom: 20px;
      }
      .assignment-form {
        display: flex;
        gap: 10px;
        align-items: center;
      }
      .assignment-form select {
        flex: 1;
      }
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
        border-bottom: 1px solid #dee2e6;
      }
      .modal-header h4 {
        margin: 0;
        font-size: 1.25rem;
      }
      .btn-close-modal {
        background: transparent;
        border: none;
        font-size: 1.5rem;
        line-height: 1;
        cursor: pointer;
        opacity: 0.5;
        padding: 0;
        margin: 0;
      }
      .btn-close-modal:hover {
        opacity: 0.8;
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
        font-size: 15px;
        color: #495057;
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
        background: #212529;
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
  ) { }

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
    this.loadAssignedTemplates(client.id);
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

  assignTemplate() {
    if (!this.selectedClient || !this.selectedTemplateId) return;

    this.apiService.assignTemplate(this.selectedClient.id, this.selectedTemplateId).subscribe({
      next: () => {
        this.generalService.success('Template assigned successfully');
        this.selectedTemplateId = null;
        this.loadAssignedTemplates(this.selectedClient!.id);
      },
      error: (err) => {
        const msg = err.error?.message || 'Failed to assign template';
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

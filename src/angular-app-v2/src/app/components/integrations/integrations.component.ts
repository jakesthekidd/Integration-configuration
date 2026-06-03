import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { MenuItem } from 'primeng/api';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { ApiClient, CreateApiClientRequest, UpdateApiClientRequest } from '../../models/api-client.model';
import { TemplateVersionResponse } from '../../models/template.model';
import { environment } from '../../../environments/environment';
import { DataTableComponent, DataTableColumn } from '../../design-system/data-table.component';
import { RowActionsComponent } from '../../design-system/row-actions.component';
import { StatusTagComponent } from '../../design-system/status-tag.component';
import { SectionHeaderComponent } from '../../design-system/section-header.component';

@Component({
    selector: 'app-integrations',
    imports: [
      CommonModule,
      FormsModule,
      ButtonModule,
      DataTableComponent,
      RowActionsComponent,
      StatusTagComponent,
      SectionHeaderComponent,
    ],
    templateUrl: './integrations.component.html',
    styleUrl: './integrations.component.scss'
})
export class IntegrationsComponent implements OnInit {
  clients: ApiClient[] = [];

  /** Column metadata for the unified data table. */
  clientColumns: DataTableColumn[] = [
    { field: 'name', header: 'Name' },
    { field: 'isActive', header: 'Status', width: '9rem' },
    { field: 'description', header: 'Description' },
    { field: '', header: '', sortable: false, width: '4rem', align: 'center' },
  ];

  menuFor(client: ApiClient): MenuItem[] {
    return [
      { label: 'View', icon: 'pi pi-eye', command: () => this.selectClient(client) },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        styleClass: 'menu-item-danger',
        command: () => this.deleteClient(client.id),
      },
    ];
  }
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
  handleKeyDown(_event: Event) {
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

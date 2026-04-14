import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { FieldMappingTemplate, CreateTemplateRequest, UpdateTemplateRequest } from '../../models/template.model';
import { FieldMappingsComponent } from '../field-mappings/field-mappings.component';

type Screen = 'list' | 'detail' | 'version';

@Component({
  selector: 'app-templates',
  standalone: true,
  imports: [CommonModule, FormsModule, FieldMappingsComponent],
  templateUrl: './templates.component.html',
  styleUrl: './templates.component.scss',
})
export class TemplatesComponent implements OnInit {
  // --- Screen State ---
  currentScreen: Screen = 'list';
  selectedTemplate: FieldMappingTemplate | null = null;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  selectedVersionObj: any = null;

  // --- List Screen ---
  templates: FieldMappingTemplate[] = [];
  showCreateForm: boolean = false;
  newTemplate: CreateTemplateRequest = this.getEmptyCreateRequest();

  // --- Detail Screen ---
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  templateVersions: any[] = [];
  editingTemplate: FieldMappingTemplate | null = null;
  editRequest: UpdateTemplateRequest = this.getEmptyEditRequest();
  showDuplicateDropdown: boolean = false;
  duplicatedTemplateId: string | null = null;

  // --- Shared ---
  isInitialLoading: boolean = false;
  error: string = '';
  success: string = '';

  constructor(
    private apiService: ApiService,
    private generalService: GeneralService,
  ) {}

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

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  openVersion(version: any) {
    this.selectedVersionObj = version;
    this.currentScreen = 'version';
    this.clearMessages();
  }

  viewDuplicated() {
    if (!this.duplicatedTemplateId) return;
    this.apiService.getTemplateById(this.duplicatedTemplateId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.openDetail(response.data);
          this.duplicatedTemplateId = null;
        }
      },
    });
  }

  toggleDuplicateDropdown(event: Event) {
    event.stopPropagation();
    this.showDuplicateDropdown = !this.showDuplicateDropdown;
  }

  @HostListener('document:click')
  closeDropdowns() {
    this.showDuplicateDropdown = false;
  }

  // ===== Data Loading =====

  loadTemplates() {
    this.isInitialLoading = true;
    this.apiService.getTemplates().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.templates = response.data.templates;
        }
        this.isInitialLoading = false;
      },
      error: (err) => {
        this.error = 'Failed to load templates';
        this.isInitialLoading = false;
        console.error(err);
      },
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
      },
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
      },
    });
  }

  startEdit(template: FieldMappingTemplate) {
    this.editingTemplate = template;
    this.editRequest = {
      name: template.name,
      description: template.description,
      sourceSchema: template.sourceSchema ?? '',
      targetSchema: template.targetSchema ?? '',
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
            },
          });
          this.loadTemplates();
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to update template';
      },
    });
  }

  duplicateTemplate(template: FieldMappingTemplate, includeAllVersions: boolean = true) {
    this.clearMessages();
    this.showDuplicateDropdown = false;
    this.apiService.duplicateTemplate(template.id, { includeAllVersions }).subscribe({
      next: (response) => {
        if (response.success) {
          const mode = includeAllVersions ? 'with all versions' : 'with last version';
          this.success = `Template "${response.data?.name}" created as a copy (${mode}).`;
          this.duplicatedTemplateId = response.data?.id ?? null;
          this.loadTemplates();
        }
      },
      error: (err) => (this.error = err.error?.message || 'Failed to duplicate template'),
    });
  }

  hasAnyDraft(): boolean {
    return this.templateVersions.some((v) => v.status === 'Draft');
  }

  createNewVersion(template: FieldMappingTemplate, baseVersion?: number) {
    const msg = baseVersion
      ? `Create a new version for "${template.name}" based on v${baseVersion}?`
      : `Create a new version for "${template.name}" based on the latest published version?`;

    this.generalService
      .confirm({
        title: 'Create New Version',
        text: msg,
        confirmText: 'Yes, Create',
        icon: 'question',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.clearMessages();
        this.apiService.createTemplateVersion(template.id, baseVersion).subscribe({
          next: (response) => {
            if (response.success) {
              this.generalService.success('New draft version created.');
              this.loadTemplateVersions(template.id);
            }
          },
          error: (err) => (this.error = err.error?.message || 'Failed to create new version'),
        });
      });
  }

  isMappingReadonly(): boolean {
    if (!this.selectedTemplate || !this.selectedVersionObj) return true;
    // Lock if template is archived OR the version is not a Draft (i.e. Published = superseded or current published)
    return this.selectedTemplate.status === 'Archived' || this.selectedVersionObj.status !== 'Draft';
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  publishVersion(version: any) {
    if (!this.selectedTemplate) return;
    if (this.selectedTemplate.status === 'Archived') {
      this.generalService.error('Cannot publish a version of an archived template.');
      return;
    }

    this.generalService
      .confirm({
        title: 'Publish Version',
        text: `Publish version ${version.version} of "${this.selectedTemplate.name}"?`,
        confirmText: 'Yes, Publish',
        confirmColor: '#27ae60',
        icon: 'question',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.clearMessages();
        this.apiService.publishTemplateVersion(this.selectedTemplate!.id, version.version).subscribe({
          next: (response) => {
            if (response.success) {
              this.generalService.success('Version ' + version.version + ' published successfully.');
              version.status = 'Published';
              this.selectedVersionObj = { ...version, status: 'Published' };
              this.loadTemplateVersions(this.selectedTemplate!.id);
              this.loadTemplates();
            }
          },
          error: (err) => (this.error = err.error?.message || 'Failed to publish version'),
        });
      });
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  deleteVersion(version: any) {
    if (!this.selectedTemplate) return;

    this.generalService
      .confirm({
        title: 'Delete Version Draft',
        text: `Are you sure you want to delete version ${version.version} draft? This will also delete all its field mappings.`,
        confirmText: 'Yes, Delete',
        confirmColor: '#e74c3c',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.clearMessages();
        this.apiService.deleteTemplateVersion(this.selectedTemplate!.id, version.version).subscribe({
          next: (response) => {
            if (response.success) {
              this.generalService.success('Version ' + version.version + ' deleted.');
              this.loadTemplateVersions(this.selectedTemplate!.id);
            }
          },
          error: (err) => (this.error = err.error?.message || 'Failed to delete version'),
        });
      });
  }

  archiveTemplate(template: FieldMappingTemplate) {
    this.generalService
      .confirm({
        title: 'Archive Template',
        text: `Archive template "${template.name}"?`,
        confirmText: 'Yes, Archive',
        confirmColor: '#e74c3c',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.clearMessages();
        this.apiService
          .updateTemplate(template.id, { name: template.name, description: template.description, status: 'Archived' })
          .subscribe({
            next: (response) => {
              if (response.success) {
                this.generalService.success('Template "' + template.name + '" archived.');
                if (this.selectedTemplate?.id === template.id) {
                  this.selectedTemplate = { ...template, status: 'Archived' };
                }
                this.loadTemplates();
              }
            },
            error: (err) => (this.error = err.error?.message || 'Failed to archive template'),
          });
      });
  }

  reactivateTemplate(template: FieldMappingTemplate) {
    this.generalService
      .confirm({
        title: 'Reactivate Template',
        text: `Reactivate template "${template.name}"?`,
        confirmText: 'Yes, Reactivate',
        confirmColor: '#27ae60',
        icon: 'question',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.clearMessages();
        this.apiService.reactivateTemplate(template.id).subscribe({
          next: () => {
            this.generalService.success('Template "' + template.name + '" reactivated.');
            if (this.selectedTemplate?.id === template.id) {
              this.selectedTemplate = { ...template, status: 'Active' };
            }
            this.loadTemplates();
          },
          error: (err) => (this.error = err.error?.message || 'Failed to reactivate'),
        });
      });
  }

  deleteTemplate(template: FieldMappingTemplate) {
    this.generalService
      .confirm({
        title: 'Delete Template',
        text: `Delete template "${template.name}"? This cannot be undone.`,
        confirmText: 'Yes, Delete',
        confirmColor: '#e74c3c',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.clearMessages();
        this.apiService.deleteTemplate(template.id, template.version).subscribe({
          next: () => {
            this.generalService.success('Template "' + template.name + '" deleted.');
            this.loadTemplates();
          },
          error: (err) => (this.error = err.error?.message || 'Failed to delete template'),
        });
      });
  }

  // ===== File Upload =====

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
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

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
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
      case 'active':
        return 'badge-published';
      case 'archived':
        return 'badge-archived';
      default:
        return 'badge-draft';
    }
  }

  getVersionStatusClass(status?: string): string {
    switch (status?.toLowerCase()) {
      case 'published':
        return 'badge-published';
      default:
        return 'badge-draft';
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
    this.duplicatedTemplateId = null;
  }

  private getEmptyCreateRequest(): CreateTemplateRequest {
    return { name: '', description: '', sourceSchema: '', targetSchema: '' };
  }

  private getEmptyEditRequest(): UpdateTemplateRequest {
    return { name: '', description: '', sourceSchema: '', targetSchema: '' };
  }
}

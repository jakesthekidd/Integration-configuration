import { Component, OnInit, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { FieldMapping, CreateFieldMappingRequest, TransformationTypes } from '../models/field-mapping.model';

@Component({
  selector: 'app-field-mappings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container mappings-subcontainer">
      <div class="filters" style="justify-content: space-between;">
        <h3>Field Mappings for {{ templateName }}</h3>
        <button class="btn-primary" *ngIf="!isReadonly" (click)="showCreateForm = !showCreateForm">
          {{ showCreateForm ? 'Cancel' : 'Add New Mapping' }}
        </button>
      </div>

      <div *ngIf="showCreateForm || editingMapping" class="form-container">
        <h3>{{ editingMapping ? 'Edit Field Mapping' : 'Create Field Mapping' }}</h3>
        <form (ngSubmit)="editingMapping ? updateMapping() : createMapping()" #mappingForm="ngForm">
          <div class="form-group">
            <label>Template:</label>
            <input type="text" [value]="templateName" readonly class="readonly-input" />
          </div>

          <div class="form-group">
            <label>Source Path: <span class="required">*</span></label>
            <input type="text" [(ngModel)]="newMapping.sourcePath" name="sourcePath" required
                   list="sourcePathList"
                   placeholder="e.g., customer.name" autocomplete="off" />
            <datalist id="sourcePathList">
              <option *ngFor="let p of sourcePaths" [value]="p"></option>
            </datalist>
          </div>

          <div class="form-group">
            <label>Target Path: <span class="required">*</span></label>
            <input type="text" [(ngModel)]="newMapping.targetPath" name="targetPath" required
                   list="targetPathList"
                   placeholder="e.g., CustomerName" autocomplete="off" />
            <datalist id="targetPathList">
              <option *ngFor="let p of targetPaths" [value]="p"></option>
            </datalist>
          </div>

          <div class="form-group">
            <label>Transformation Type: <span class="required">*</span></label>
            <select [(ngModel)]="newMapping.transformationType" name="transformationType" required>
              <option *ngFor="let type of transformationTypes" [value]="type">{{type}}</option>
            </select>
          </div>

          <div class="form-group">
            <label>Transformation Config:</label>
            <textarea [(ngModel)]="newMapping.transformationConfig" name="transformationConfig" rows="3"
                      placeholder='{"separator": ", ", "fields": ["field1", "field2"]}'></textarea>
            <small>JSON configuration for the transformation</small>
          </div>

          <div class="form-group">
            <label>Execution Order: <span class="required">*</span></label>
            <input type="number" [(ngModel)]="newMapping.executionOrder" name="executionOrder" required min="0" />
          </div>

          <div class="form-group checkbox">
            <label>
              <input type="checkbox" [(ngModel)]="newMapping.isRequired" name="isRequired" />
              Required Field
            </label>
          </div>

          <div class="form-group">
            <label>Default Value:</label>
            <input type="text" [(ngModel)]="newMapping.defaultValue" name="defaultValue"
                   placeholder="Value to use if source is empty" />
          </div>

          <div class="form-group">
            <label>Validation Rules:</label>
            <textarea [(ngModel)]="newMapping.validationRules" name="validationRules" rows="2"
                      placeholder='{"pattern": "^[A-Z0-9]+$", "maxLength": 50}'></textarea>
            <small>JSON validation rules for the field</small>
          </div>

          <div class="form-actions">
            <button type="submit" class="btn-primary" [disabled]="!mappingForm.form.valid">
              {{ editingMapping ? 'Update' : 'Create' }}
            </button>
            <button type="button" class="btn-secondary" (click)="cancelEdit()">
              {{ editingMapping ? 'Cancel' : 'Reset' }}
            </button>
          </div>
        </form>
      </div>

      <div *ngIf="error" class="error">{{ error }}</div>
      <div *ngIf="success" class="success">{{ success }}</div>

      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>Source Path</th>
              <th>Target Path</th>
              <th>Type</th>
              <th>Order</th>
              <th>Required</th>
              <th>Default</th>
              <th *ngIf="!isReadonly">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let mapping of mappings">
              <td><code>{{ mapping.sourcePath }}</code></td>
              <td><code>{{ mapping.targetPath }}</code></td>
              <td><span class="badge">{{ mapping.transformationType }}</span></td>
              <td>{{ mapping.executionOrder }}</td>
              <td>{{ mapping.isRequired ? 'Yes' : 'No' }}</td>
              <td>{{ mapping.defaultValue || '-' }}</td>
              <td *ngIf="!isReadonly">
                <button class="btn-small btn-info" (click)="startEdit(mapping)">Edit</button>
                <button class="btn-small btn-danger" (click)="deleteMapping(mapping.id)">Delete</button>
              </td>
            </tr>
            <tr *ngIf="mappings.length === 0">
              <td colspan="8" class="no-data">No field mappings found</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  `,
  styles: [`
    .container.mappings-subcontainer {
      max-width: 100%;
      padding: 0;
      margin: 0;
    }
    
    .readonly-input {
      background-color: #e9ecef !important;
      cursor: not-allowed;
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
    }

    .filters select {
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
    }

    .form-container {
      background: #f8f9fa;
      padding: 20px;
      border-radius: 4px;
      margin-bottom: 20px;
    }

    .form-container h3 {
      margin-top: 0;
      color: #2c3e50;
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
    .form-group input[type="number"],
    .form-group select,
    .form-group textarea {
      width: 100%;
      padding: 8px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-family: inherit;
    }

    .form-group textarea {
      font-family: 'Courier New', monospace;
      font-size: 12px;
    }

    .form-group small {
      display: block;
      color: #666;
      font-size: 12px;
      margin-top: 4px;
    }

    .form-group.checkbox {
      display: flex;
      align-items: center;
    }

    .form-group.checkbox label {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 0;
    }

    .form-group.checkbox input[type="checkbox"] {
      width: auto;
    }

    .required {
      color: #e74c3c;
    }

    .form-actions {
      display: flex;
      gap: 10px;
      margin-top: 20px;
    }

    .btn-primary, .btn-secondary, .btn-small, .btn-danger {
      padding: 8px 16px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
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

    .btn-danger {
      background: #e74c3c;
      color: white;
    }

    .btn-danger:hover {
      background: #c0392b;
    }

    .btn-small {
      padding: 4px 12px;
      font-size: 12px;
      margin-right: 5px;
    }

    .btn-info {
      background: #3498db;
      color: white;
    }

    .btn-info:hover {
      background: #2980b9;
    }

    .error {
      background: #fee;
      color: #c33;
      padding: 10px;
      border-radius: 4px;
      margin-bottom: 15px;
    }

    .success {
      background: #efe;
      color: #3c3;
      padding: 10px;
      border-radius: 4px;
      margin-bottom: 15px;
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
    }

    tbody tr:hover {
      background: #f5f5f5;
    }

    code {
      background: #f4f4f4;
      padding: 2px 6px;
      border-radius: 3px;
      font-family: 'Courier New', monospace;
      font-size: 12px;
    }

    .badge {
      display: inline-block;
      padding: 4px 8px;
      background: #3498db;
      color: white;
      border-radius: 3px;
      font-size: 11px;
      font-weight: 500;
    }

    .no-data {
      text-align: center;
      color: #999;
      font-style: italic;
    }
  `]
})
export class FieldMappingsComponent implements OnInit, OnChanges {
  @Input() templateId!: string;
  @Input() templateVersionId!: string;
  @Input() templateName: string = '';
  @Input() sampleInputJson?: string;
  @Input() sourceSchema?: string;
  @Input() isReadonly: boolean = false;

  mappings: FieldMapping[] = [];
  transformationTypes = TransformationTypes;
  showCreateForm: boolean = false;
  editingMapping: FieldMapping | null = null;
  error: string = '';
  success: string = '';
  sourcePaths: string[] = [];
  targetPaths: string[] = [];

  newMapping: CreateFieldMappingRequest = this.getEmptyMapping();

  constructor(private apiService: ApiService) { }

  ngOnInit() {
    this.refreshMappingData();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['templateVersionId'] && !changes['templateVersionId'].firstChange) {
      this.refreshMappingData();
      this.cancelEdit();
    }
  }

  refreshMappingData() {
    if (this.templateId && this.templateVersionId) {
      this.loadMappings();
      this.loadSourcePathsFromSourceSchema();
    }
  }

  loadMappings() {
    this.apiService.getFieldMappings(this.templateId, this.templateVersionId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.mappings = response.data.mappings;
          this.refreshPathSuggestions();
        }
      },
      error: (err) => {
        this.error = 'Failed to load field mappings';
        console.error(err);
      }
    });
  }

  private refreshPathSuggestions(): void {
    this.sourcePaths = [...new Set(this.mappings.map(m => m.sourcePath).filter(Boolean))].sort();
    this.targetPaths = [...new Set(this.mappings.map(m => m.targetPath).filter(Boolean))].sort();
  }

  private loadSourcePathsFromSampleJson(): void {
    if (!this.sampleInputJson) return;

    this.apiService.parseJson(this.sampleInputJson).subscribe({
      next: (response) => {
        if (response.success && response.data?.fields) {
          const parsedPaths: string[] = Object.keys(response.data.fields);
            this.sourcePaths = [...new Set([...this.sourcePaths, ...parsedPaths])].sort();
        }
      },
      error: (err) => console.warn('Could not parse sample JSON for path suggestions', err)
    });
    }

    private loadSourcePathsFromSourceSchema(): void {
        if (!this.sourceSchema) return;

        this.apiService.parseJson(this.sourceSchema).subscribe({
            next: (response) => {
                if (response.success && response.data?.fields) {
                    const parsedPaths: string[] = Object.keys(response.data.fields);
                    this.sourcePaths = [...new Set([...this.sourcePaths, ...parsedPaths])].sort();
                    console.log(this.sourcePaths);
                }
            },
            error: (err) => console.warn('Could not parse sample JSON for path suggestions', err)
        });
    }

  createMapping() {
    this.error = '';
    this.success = '';

    // Always stamp the current Input values in case newMapping was initialized before inputs were set
    this.newMapping.templateId = this.templateId;
    this.newMapping.templateVersionId = this.templateVersionId;

    this.apiService.createFieldMapping(this.newMapping).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = 'Field mapping created successfully';
          this.showCreateForm = false;
          this.resetForm();
          this.loadMappings();
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to create field mapping';
        console.error(err);
      }
    });
  }

  deleteMapping(id: string) {
    if (!confirm('Are you sure you want to delete this field mapping?')) {
      return;
    }

    this.error = '';
    this.success = '';

    this.apiService.deleteFieldMapping(id).subscribe({
      next: () => {
        this.success = 'Field mapping deleted successfully';
        this.loadMappings();
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to delete field mapping';
        console.error(err);
      }
    });
  }

  startEdit(mapping: FieldMapping) {
    this.editingMapping = mapping;
    this.showCreateForm = false;
    this.error = '';
    this.success = '';
    this.refreshPathSuggestions();

    // Populate form with mapping data
    this.newMapping = {
      templateId: this.templateId,
      templateVersionId: this.templateVersionId,
      sourcePath: mapping.sourcePath,
      targetPath: mapping.targetPath,
      transformationType: mapping.transformationType,
      transformationConfig: mapping.transformationConfig || '',
      executionOrder: mapping.executionOrder,
      isRequired: mapping.isRequired,
      defaultValue: mapping.defaultValue || '',
      validationRules: mapping.validationRules || ''
    };

    // Scroll to form
    setTimeout(() => {
      document.querySelector('.form-container')?.scrollIntoView({ behavior: 'smooth' });
    }, 100);
  }

  updateMapping() {
    if (!this.editingMapping) return;

    this.error = '';
    this.success = '';

    const updateRequest = {
      sourcePath: this.newMapping.sourcePath,
      targetPath: this.newMapping.targetPath,
      transformationType: this.newMapping.transformationType,
      transformationConfig: this.newMapping.transformationConfig,
      executionOrder: this.newMapping.executionOrder,
      isRequired: this.newMapping.isRequired,
      defaultValue: this.newMapping.defaultValue,
      validationRules: this.newMapping.validationRules
    };

    this.apiService.updateFieldMapping(this.editingMapping.id, updateRequest).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = 'Field mapping updated successfully';
          this.cancelEdit();
          this.loadMappings();
        }
      },
      error: (err) => {
        this.error = err.error?.message || 'Failed to update field mapping';
        console.error(err);
      }
    });
  }

  cancelEdit() {
    this.editingMapping = null;
    this.showCreateForm = false;
    this.resetForm();
  }

  resetForm() {
    this.newMapping = this.getEmptyMapping();
  }

  private getEmptyMapping(): CreateFieldMappingRequest {
    return {
      templateId: this.templateId || '',
      templateVersionId: this.templateVersionId || '',
      sourcePath: '',
      targetPath: '',
      transformationType: 'Direct',
      transformationConfig: '',
      executionOrder: 0,
      isRequired: false,
      defaultValue: '',
      validationRules: ''
    };
  }
}

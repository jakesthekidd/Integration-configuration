import { Component, OnInit, Input, OnChanges, SimpleChanges } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { MenuItem } from 'primeng/api';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { FieldMapping, CreateFieldMappingRequest, TransformationTypes } from '../../models/field-mapping.model';
import { LookupTable } from '../../models/lookup-table.model';
import { DataTableComponent, DataTableColumn } from '../../design-system/data-table.component';
import { RowActionsComponent } from '../../design-system/row-actions.component';

@Component({
  selector: 'app-field-mappings',
  imports: [
    FormsModule,
    ButtonModule,
    DataTableComponent,
    RowActionsComponent,
  ],
  templateUrl: './field-mappings.component.html',
  styleUrl: './field-mappings.component.scss',
})
export class FieldMappingsComponent implements OnInit, OnChanges {
  @Input() templateId!: string;
  @Input() templateVersionId!: string;
  @Input() templateName: string = '';
  @Input() sampleInputJson?: string;
  @Input() sourceSchema?: string;
  @Input() targetSchema?: string;
  @Input() isReadonly: boolean = false;

  /** Column metadata for the unified data table. */
  fmColumns: DataTableColumn[] = [
    { field: 'sourcePath', header: 'Source Path' },
    { field: 'targetPath', header: 'Target Path' },
    { field: 'transformationType', header: 'Type', width: '9rem' },
    { field: 'isRequired', header: 'Required', width: '7rem', align: 'center' },
    { field: 'defaultValue', header: 'Default', width: '9rem' },
    { field: '', header: '', sortable: false, width: '4rem', align: 'center' },
  ];

  menuFor(mapping: FieldMapping): MenuItem[] {
    return [
      { label: 'Edit', icon: 'pi pi-pencil', command: () => this.startEdit(mapping) },
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        styleClass: 'menu-item-danger',
        command: () => this.deleteMapping(mapping.id),
      },
    ];
  }

  mappings: FieldMapping[] = [];
  transformationTypes = TransformationTypes;
  showCreateForm: boolean = false;
  editingMapping: FieldMapping | null = null;
  error: string = '';
  success: string = '';
  sourcePaths: string[] = [];
  targetPaths: string[] = [];
  lookupTables: LookupTable[] = [];

  newMapping: CreateFieldMappingRequest = this.getEmptyMapping();

  constructor(
    private apiService: ApiService,
    private generalService: GeneralService,
  ) {}

  ngOnInit() {
    this.refreshMappingData();
    this.loadLookupTables();
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
      this.loadTargetPathsFromTargetSchema();
    }
  }

  loadMappings() {
    this.refreshPathSuggestions();
    this.apiService.getFieldMappings(this.templateId, this.templateVersionId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.mappings = response.data.mappings;
        }
      },
      error: (err) => {
        this.error = 'Failed to load field mappings';
        console.error(err);
      },
    });
  }

  private refreshPathSuggestions(): void {
    this.sourcePaths = [...new Set(this.mappings.map((m) => m.sourcePath).filter(Boolean))].sort();
    this.targetPaths = [...new Set(this.mappings.map((m) => m.targetPath).filter(Boolean))].sort();
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
      error: (err) => console.warn('Could not parse sample JSON for path suggestions', err),
    });
  }

  private loadSourcePathsFromSourceSchema(): void {
    if (!this.sourceSchema) return;

    this.apiService.parseJson(this.sourceSchema).subscribe({
      next: (response) => {
        if (response.success && response.data?.fields) {
          const parsedPaths: string[] = Object.keys(response.data.fields);
          this.sourcePaths = [...new Set([...this.sourcePaths, ...parsedPaths])].sort();
        }
      },
      error: (err) => console.warn('Could not parse Source Schema JSON for path suggestions', err),
    });
  }

  private loadTargetPathsFromTargetSchema(): void {
    if (!this.targetSchema) return;

    this.apiService.parseJson(this.targetSchema).subscribe({
      next: (response) => {
        if (response.success && response.data?.fields) {
          const parsedPaths: string[] = Object.keys(response.data.fields);
          this.targetPaths = [...new Set([...this.targetPaths, ...parsedPaths])].sort();
        }
      },
      error: (err) => console.warn('Could not parse Target Schema JSON for path suggestions', err),
    });
  }

  private isDuplicateTargetPath(targetPathVal: string, id?: string): boolean {
    const value = targetPathVal?.trim().toLowerCase();
    return this.mappings.some((m) => {
      const samePath = m.targetPath?.trim().toLowerCase() === value;
      const differentItem = !id || m.id !== id;

      return samePath && differentItem;
    });
  }

  createMapping() {
    this.error = '';
    this.success = '';

    this.newMapping.templateId = this.templateId;
    this.newMapping.templateVersionId = this.templateVersionId;

    if (this.isDuplicateTargetPath(this.newMapping.targetPath)) {
      this.error = 'Duplicate target path! Failed to create field mapping.';
      return;
    }

    const payload = this.buildPayload(this.newMapping);

    this.apiService.createFieldMapping(payload).subscribe({
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
      },
    });
  }

  deleteMapping(id: string) {
    this.generalService
      .confirm({
        title: 'Delete Field Mapping',
        text: 'Are you sure you want to delete this field mapping?',
        confirmText: 'Yes, Delete',
        confirmColor: '#e74c3c',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;

        this.error = '';
        this.success = '';

        this.apiService.deleteFieldMapping(id).subscribe({
          next: () => {
            this.generalService.success('Field mapping deleted successfully');
            this.loadMappings();
          },
          error: (err) => {
            this.error = err.error?.message || 'Failed to delete field mapping';
            console.error(err);
          },
        });
      });
  }

  startEdit(mapping: FieldMapping) {
    this.editingMapping = mapping;
    this.showCreateForm = false;
    this.error = '';
    this.success = '';
    this.refreshPathSuggestions();

    let lookupTableId = '';

    try {
      const config = mapping.transformationConfig ? JSON.parse(mapping.transformationConfig) : null;

      lookupTableId = config?.LookupTableId || '';
    } catch {
      lookupTableId = '';
    }

    this.newMapping = {
      templateId: this.templateId,
      templateVersionId: this.templateVersionId,
      sourcePath: mapping.sourcePath,
      targetPath: mapping.targetPath,
      transformationType: mapping.transformationType,
      transformationConfig: mapping.transformationConfig || '',
      isRequired: mapping.isRequired,
      defaultValue: mapping.defaultValue || '',
      validationRules: mapping.validationRules || '',
      lookupTableId,
    };

    setTimeout(() => {
      document.querySelector('.form-container')?.scrollIntoView({ behavior: 'smooth' });
    }, 100);
  }

  updateMapping() {
    if (!this.editingMapping) return;

    this.error = '';
    this.success = '';

    const updateRequest = this.buildPayload(this.newMapping);

    if (this.isDuplicateTargetPath(updateRequest.targetPath, this.editingMapping.id)) {
      this.error = 'Duplicate target path!  Failed to update field mapping.';
      return;
    }

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
      },
    });
  }

  cancelEdit() {
    if (this.editingMapping) {
      this.showCreateForm = false;
    }

    this.editingMapping = null;
    this.resetForm();
  }

  resetForm() {
    this.error = '';
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
      isRequired: false,
      defaultValue: '',
      validationRules: '',
      lookupTableId: '',
    };
  }
  loadLookupTables() {
    this.apiService.getLookupTables().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.lookupTables = response.data.lookupTables;
        }
      },
      error: (err) => {
        this.error = 'Failed to load lookup tables';
        console.error(err);
      },
    });
  }

  onTransformationTypeChange() {
    if (this.newMapping.transformationType !== 'Lookup') {
      this.newMapping.lookupTableId = '';
    }
  }

  private buildPayload(mapping: CreateFieldMappingRequest) {
    let transformationConfig: string = mapping.transformationConfig ?? '';

    if (mapping.transformationType === 'Lookup' && mapping.lookupTableId) {
      transformationConfig = JSON.stringify({
        LookupTableId: mapping.lookupTableId,
      });
    } else {
      transformationConfig = mapping.transformationConfig ?? '';
    }

    return {
      ...mapping,
      transformationConfig,
      lookupTableId: undefined,
    };
  }
}

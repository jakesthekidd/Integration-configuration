import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of, throwError } from 'rxjs';

import { FieldMappingsComponent } from './field-mappings.component';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { FieldMapping } from '../../models/field-mapping.model';

describe('FieldMappingsComponent', () => {
  let component: FieldMappingsComponent;
  let fixture: ComponentFixture<FieldMappingsComponent>;
  let apiServiceSpy: jasmine.SpyObj<ApiService>;
  let generalServiceSpy: jasmine.SpyObj<GeneralService>;

  const mockMapping: FieldMapping = {
    id: 'mapping-1',
    templateId: 'tmpl-1',
    sourcePath: 'source.field',
    targetPath: 'target.field',
    transformationType: 'Direct',
    transformationConfig: '',
    isRequired: false,
    defaultValue: '',
    validationRules: '',
    createdAt: new Date('2024-01-01'),
    updatedAt: new Date('2024-01-01'),
  };

  const dismissedResult = { isConfirmed: false, isDenied: false, isDismissed: true } as any;

  beforeEach(async () => {
    apiServiceSpy = jasmine.createSpyObj('ApiService', [
      'getFieldMappings', 'createFieldMapping', 'updateFieldMapping', 'deleteFieldMapping',
      'getLookupTables', 'parseJson',
    ]);
    generalServiceSpy = jasmine.createSpyObj('GeneralService', ['confirm', 'success', 'error']);

    apiServiceSpy.getFieldMappings.and.returnValue(
      of({ success: true, data: { mappings: [mockMapping], totalCount: 1 }, message: '' })
    );
    apiServiceSpy.getLookupTables.and.returnValue(
      of({ success: true, data: { lookupTables: [], totalCount: 0 }, message: '' })
    );
    apiServiceSpy.createFieldMapping.and.returnValue(
      of({ success: true, data: mockMapping, message: '' })
    );
    apiServiceSpy.updateFieldMapping.and.returnValue(
      of({ success: true, data: mockMapping, message: '' })
    );
    apiServiceSpy.deleteFieldMapping.and.returnValue(of(undefined as any));
    generalServiceSpy.confirm.and.returnValue(Promise.resolve({ isConfirmed: true, isDenied: false, isDismissed: false, value: true } as any));
    generalServiceSpy.success.and.returnValue(Promise.resolve(dismissedResult));
    generalServiceSpy.error.and.returnValue(Promise.resolve(dismissedResult));

    await TestBed.configureTestingModule({
      imports: [FieldMappingsComponent],
      providers: [
        { provide: ApiService, useValue: apiServiceSpy },
        { provide: GeneralService, useValue: generalServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(FieldMappingsComponent);
    component = fixture.componentInstance;
    // Set inputs BEFORE detectChanges so ngOnInit can call loadMappings
    component.templateId = 'tmpl-1';
    component.templateVersionId = 'version-id';
  });

  it('should create the component', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('when templateVersionId is set, ngOnInit calls loadMappings()', () => {
    fixture.detectChanges();
    expect(apiServiceSpy.getFieldMappings).toHaveBeenCalled();
  });

  it('loadMappings() populates mappings', () => {
    fixture.detectChanges();
    expect(component.mappings).toEqual([mockMapping]);
  });

  it('startEdit(mapping) sets editingMapping', () => {
    fixture.detectChanges();
    component.startEdit(mockMapping);
    expect(component.editingMapping).toEqual(mockMapping);
  });

  it('cancelEdit() clears editingMapping', () => {
    fixture.detectChanges();
    component.startEdit(mockMapping);
    component.cancelEdit();
    expect(component.editingMapping).toBeNull();
  });

  it('resetForm() clears the new mapping form', () => {
    fixture.detectChanges();
    component.newMapping.sourcePath = 'some.path';
    component.newMapping.targetPath = 'other.path';
    component.resetForm();
    expect(component.newMapping.sourcePath).toBe('');
    expect(component.newMapping.targetPath).toBe('');
  });
});

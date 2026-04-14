import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of, throwError } from 'rxjs';
import { BrowserModule } from '@angular/platform-browser';

import { TransformationTestComponent } from './transformation-test.component';
import { ApiService } from '../../services/api.service';
import { FieldMappingTemplate } from '../../models/template.model';
import { ApiClient } from '../../models/api-client.model';

describe('TransformationTestComponent', () => {
  let component: TransformationTestComponent;
  let fixture: ComponentFixture<TransformationTestComponent>;
  let apiServiceSpy: jasmine.SpyObj<ApiService>;

  const mockTemplates: FieldMappingTemplate[] = [
    {
      id: 'tmpl-1',
      name: 'Active Template',
      description: 'desc',
      version: 1,
      status: 'Active',
      createdAt: new Date('2024-01-01'),
      updatedAt: new Date('2024-01-01'),
    },
    {
      id: 'tmpl-2',
      name: 'Draft Template',
      description: 'desc',
      version: 1,
      status: 'Draft',
      createdAt: new Date('2024-01-01'),
      updatedAt: new Date('2024-01-01'),
    },
  ];

  const mockClients: ApiClient[] = [
    { id: 'client-1', name: 'Active Client', isActive: true, createdAt: '2024-01-01', updatedAt: '2024-01-01' },
    { id: 'client-2', name: 'Inactive Client', isActive: false, createdAt: '2024-01-01', updatedAt: '2024-01-01' },
  ];

  beforeEach(async () => {
    apiServiceSpy = jasmine.createSpyObj('ApiService', ['getTemplates', 'getApiClients', 'transformJsonWithTemplate']);

    apiServiceSpy.getTemplates.and.returnValue(
      of({ success: true, data: { templates: mockTemplates, totalCount: mockTemplates.length }, message: '' }),
    );
    apiServiceSpy.getApiClients.and.returnValue(
      of({ success: true, data: { apiClients: mockClients, totalCount: mockClients.length }, message: '' }),
    );
    apiServiceSpy.transformJsonWithTemplate.and.returnValue(
      of({
        success: true,
        data: { success: true, outputJson: '{}', errors: [], warnings: [], executionTimeMs: 10 },
        message: '',
      }),
    );

    await TestBed.configureTestingModule({
      imports: [TransformationTestComponent, BrowserModule],
      providers: [{ provide: ApiService, useValue: apiServiceSpy }],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(TransformationTestComponent);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('ngOnInit calls loadTemplates() and loadApiClients()', () => {
    fixture.detectChanges();
    expect(apiServiceSpy.getTemplates).toHaveBeenCalled();
    expect(apiServiceSpy.getApiClients).toHaveBeenCalled();
  });

  it('loadTemplates() filters for Active templates only', () => {
    fixture.detectChanges();
    expect(component.templates.length).toBe(1);
    expect(component.templates[0].status).toBe('Active');
  });

  it('loadApiClients() filters for active clients only', () => {
    fixture.detectChanges();
    expect(component.apiClients.length).toBe(1);
    expect(component.apiClients[0].isActive).toBeTrue();
  });

  it('canTransform() returns false when no template/client/sourceJson selected', () => {
    fixture.detectChanges();
    component.selectedTemplateId = '';
    component.selectedClientId = '';
    component.sourceJson = '';
    expect(component.canTransform()).toBeFalse();
  });

  it('canTransform() returns false when only template is set', () => {
    fixture.detectChanges();
    component.selectedTemplateId = 'tmpl-1';
    component.selectedClientId = '';
    component.sourceJson = '';
    expect(component.canTransform()).toBeFalse();
  });

  it('canTransform() returns true when all three are present', () => {
    fixture.detectChanges();
    component.selectedTemplateId = 'tmpl-1';
    component.selectedClientId = 'client-1';
    component.sourceJson = '{"key":"value"}';
    expect(component.canTransform()).toBeTrue();
  });

  it('clearAll() resets all state fields', () => {
    fixture.detectChanges();
    component.sourceJson = '{"key":"value"}';
    component.transformedJson = '{"result":"value"}';
    component.fileName = 'test.json';
    component.error = 'some error';
    component.success = 'some success';
    component.clearAll();
    expect(component.sourceJson).toBe('');
    expect(component.transformedJson).toBe('');
    expect(component.fileName).toBe('');
    expect(component.error).toBe('');
    expect(component.success).toBe('');
    expect(component.transformResult).toBeNull();
    expect(component.mappingIssues).toEqual([]);
    expect(component.showAnnotatedView).toBeFalse();
  });

  it('getJsonSize() returns a human-readable size string', () => {
    fixture.detectChanges();
    const smallJson = '{}';
    const sizeStr = component.getJsonSize(smallJson);
    expect(sizeStr).toContain('bytes');

    // Create a string larger than 1024 bytes
    const largeJson = JSON.stringify({ data: 'x'.repeat(2000) });
    const largeSizeStr = component.getJsonSize(largeJson);
    expect(largeSizeStr).toContain('KB');
  });
});

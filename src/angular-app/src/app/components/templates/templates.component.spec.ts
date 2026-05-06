import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of } from 'rxjs';

import { TemplatesComponent } from './templates.component';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { FieldMappingTemplate } from '../../models/template.model';
import { Partner } from '../../models/partner.model';

describe('TemplatesComponent', () => {
  let component: TemplatesComponent;
  let fixture: ComponentFixture<TemplatesComponent>;
  let apiServiceSpy: jasmine.SpyObj<ApiService>;
  let generalServiceSpy: jasmine.SpyObj<GeneralService>;

  const mockTemplate: FieldMappingTemplate = {
    id: 'tmpl-1',
    name: 'Template One',
    description: 'desc',
    version: 1,
    status: 'Active',
    createdAt: new Date('2024-01-01'),
    updatedAt: new Date('2024-01-01'),
    sourcePartnerId: 'partner-1',
    targetPartnerId: 'partner-2',
  };

  const mockPartners: Partner[] = [
    { id: 'partner-1', name: 'Partner A', createdAt: new Date('2024-01-01'), updatedAt: new Date('2024-01-01') },
    { id: 'partner-2', name: 'Partner B', createdAt: new Date('2024-01-01'), updatedAt: new Date('2024-01-01') },
  ];

  const dismissedResult = { isConfirmed: false, isDenied: false, isDismissed: true } as any;

  beforeEach(async () => {
    apiServiceSpy = jasmine.createSpyObj('ApiService', [
      'getTemplates',
      'getPartners',
      'createTemplate',
      'updateTemplate',
      'deleteTemplate',
      'archiveTemplate',
      'reactivateTemplate',
      'getTemplateVersions',
      'createTemplateVersion',
      'publishTemplateVersion',
      'deleteTemplateVersion',
      'duplicateTemplate',
      'getTemplateById',
    ]);
    generalServiceSpy = jasmine.createSpyObj('GeneralService', ['confirm', 'success', 'error']);

    apiServiceSpy.getTemplates.and.returnValue(
      of({ success: true, data: { templates: [mockTemplate], totalCount: 1 }, message: '' }),
    );
    apiServiceSpy.getPartners.and.returnValue(
      of({ success: true, data: { partners: mockPartners, totalCount: 2, page: 1, pageSize: 1000 }, message: '' }),
    );
    apiServiceSpy.getTemplateVersions.and.returnValue(of({ success: true, data: [], message: '' }));
    apiServiceSpy.createTemplate.and.returnValue(of({ success: true, data: mockTemplate, message: '' }));
    apiServiceSpy.updateTemplate.and.returnValue(of({ success: true, data: mockTemplate, message: '' }));
    apiServiceSpy.deleteTemplate.and.returnValue(of(undefined as any));
    apiServiceSpy.getTemplateById.and.returnValue(of({ success: true, data: mockTemplate, message: '' }));
    generalServiceSpy.confirm.and.returnValue(
      Promise.resolve({ isConfirmed: true, isDenied: false, isDismissed: false, value: true } as any),
    );
    generalServiceSpy.success.and.returnValue(Promise.resolve(dismissedResult));
    generalServiceSpy.error.and.returnValue(Promise.resolve(dismissedResult));

    await TestBed.configureTestingModule({
      imports: [TemplatesComponent],
      providers: [
        { provide: ApiService, useValue: apiServiceSpy },
        { provide: GeneralService, useValue: generalServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(TemplatesComponent);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('ngOnInit calls loadTemplates()', () => {
    fixture.detectChanges();
    expect(apiServiceSpy.getTemplates).toHaveBeenCalled();
  });

  it('ngOnInit calls loadPartners()', () => {
    fixture.detectChanges();
    expect(apiServiceSpy.getPartners).toHaveBeenCalled();
  });

  it('loadTemplates() populates templates', () => {
    fixture.detectChanges();
    expect(component.templates).toEqual([mockTemplate]);
  });

  it('loadPartners() populates partners list', () => {
    fixture.detectChanges();
    expect(component.partners).toEqual(mockPartners);
    expect(component.partners.length).toBe(2);
  });

  it('openDetail(template) sets selectedTemplate and changes currentScreen to detail', () => {
    fixture.detectChanges();
    component.openDetail(mockTemplate);
    expect(component.selectedTemplate).toEqual(mockTemplate);
    expect(component.currentScreen).toBe('detail');
  });

  it('goToList() sets currentScreen to list', () => {
    fixture.detectChanges();
    component.currentScreen = 'detail';
    component.goToList();
    expect(component.currentScreen).toBe('list');
  });

  it('startEdit(template) sets editingTemplate', () => {
    fixture.detectChanges();
    component.startEdit(mockTemplate);
    expect(component.editingTemplate).toEqual(mockTemplate);
  });

  it('startEdit(template) copies sourcePartnerId and targetPartnerId into editRequest', () => {
    fixture.detectChanges();
    component.startEdit(mockTemplate);
    expect(component.editRequest.sourcePartnerId).toBe('partner-1');
    expect(component.editRequest.targetPartnerId).toBe('partner-2');
  });

  it('startEdit(template) sets undefined partner IDs when template has none', () => {
    fixture.detectChanges();
    const templateWithNoPartners: FieldMappingTemplate = {
      ...mockTemplate,
      sourcePartnerId: undefined,
      targetPartnerId: undefined,
    };
    component.startEdit(templateWithNoPartners);
    expect(component.editRequest.sourcePartnerId).toBeUndefined();
    expect(component.editRequest.targetPartnerId).toBeUndefined();
  });

  it('cancelEdit() clears editingTemplate', () => {
    fixture.detectChanges();
    component.startEdit(mockTemplate);
    component.cancelEdit();
    expect(component.editingTemplate).toBeNull();
  });

  it('getStatusClass() returns correct CSS class strings', () => {
    fixture.detectChanges();
    expect(component.getStatusClass('Active')).toBe('badge-published');
    expect(component.getStatusClass('Archived')).toBe('badge-archived');
    expect(component.getStatusClass('Draft')).toBe('badge-draft');
    expect(component.getStatusClass('Unknown')).toBe('badge-draft');
  });

  describe('status filter', () => {
    it('defaults to Active and passes "Active" to the API on initial load', () => {
      fixture.detectChanges();
      expect(component.statusFilter).toBe('Active');
      expect(apiServiceSpy.getTemplates).toHaveBeenCalledWith(undefined, 1, 10, 'Active');
    });

    it('setStatusFilter("Archived") reloads with the new filter and resets to page 1', () => {
      fixture.detectChanges();
      apiServiceSpy.getTemplates.calls.reset();
      component.currentPage = 4;

      component.setStatusFilter('Archived');

      expect(component.statusFilter).toBe('Archived');
      expect(component.currentPage).toBe(1);
      expect(apiServiceSpy.getTemplates).toHaveBeenCalledWith(undefined, 1, 10, 'Archived');
    });

    it('setStatusFilter("All") sends undefined status so the API returns every template', () => {
      fixture.detectChanges();
      apiServiceSpy.getTemplates.calls.reset();

      component.setStatusFilter('All');

      expect(component.statusFilter).toBe('All');
      expect(apiServiceSpy.getTemplates).toHaveBeenCalledWith(undefined, 1, 10, undefined);
    });

    it('setStatusFilter() does nothing when the filter has not changed', () => {
      fixture.detectChanges();
      apiServiceSpy.getTemplates.calls.reset();

      component.setStatusFilter('Active');

      expect(apiServiceSpy.getTemplates).not.toHaveBeenCalled();
    });

    it('isArchived() identifies archived templates regardless of casing', () => {
      fixture.detectChanges();
      expect(component.isArchived({ ...mockTemplate, status: 'Archived' })).toBeTrue();
      expect(component.isArchived({ ...mockTemplate, status: 'ARCHIVED' })).toBeTrue();
      expect(component.isArchived({ ...mockTemplate, status: 'Active' })).toBeFalse();
    });
  });
});

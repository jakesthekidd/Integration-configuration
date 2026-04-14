import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of, throwError } from 'rxjs';

import { IntegrationsComponent } from './integrations.component';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { ApiClient } from '../../models/api-client.model';

describe('IntegrationsComponent', () => {
  let component: IntegrationsComponent;
  let fixture: ComponentFixture<IntegrationsComponent>;
  let apiServiceSpy: jasmine.SpyObj<ApiService>;
  let generalServiceSpy: jasmine.SpyObj<GeneralService>;

  const mockClient: ApiClient = {
    id: 'client-1',
    name: 'Test Client',
    description: 'desc',
    isActive: true,
    createdAt: '2024-01-01',
    updatedAt: '2024-01-01',
  };

  const dismissedResult = { isConfirmed: false, isDenied: false, isDismissed: true } as any;

  beforeEach(async () => {
    apiServiceSpy = jasmine.createSpyObj('ApiService', [
      'getApiClients',
      'createApiClient',
      'updateApiClient',
      'deleteApiClient',
      'getAssignedTemplates',
      'getTemplates',
      'getTemplateVersions',
      'assignTemplate',
      'removeTemplate',
      'toggleApiClientActive',
    ]);
    generalServiceSpy = jasmine.createSpyObj('GeneralService', ['confirm', 'success', 'error']);

    apiServiceSpy.getApiClients.and.returnValue(
      of({ success: true, data: { apiClients: [mockClient], totalCount: 1 }, message: '' }),
    );
    apiServiceSpy.getTemplates.and.returnValue(
      of({ success: true, data: { templates: [], totalCount: 0 }, message: '' }),
    );
    apiServiceSpy.getAssignedTemplates.and.returnValue(of({ success: true, data: [], message: '' }));
    apiServiceSpy.createApiClient.and.returnValue(of({ success: true, data: mockClient, message: '' }));
    apiServiceSpy.updateApiClient.and.returnValue(of({ success: true, data: mockClient, message: '' }));
    apiServiceSpy.deleteApiClient.and.returnValue(of(undefined as any));
    generalServiceSpy.confirm.and.returnValue(
      Promise.resolve({ isConfirmed: true, isDenied: false, isDismissed: false, value: true } as any),
    );
    generalServiceSpy.success.and.returnValue(Promise.resolve(dismissedResult));
    generalServiceSpy.error.and.returnValue(Promise.resolve(dismissedResult));

    await TestBed.configureTestingModule({
      imports: [IntegrationsComponent],
      providers: [
        { provide: ApiService, useValue: apiServiceSpy },
        { provide: GeneralService, useValue: generalServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(IntegrationsComponent);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('ngOnInit calls loadClients()', () => {
    fixture.detectChanges();
    expect(apiServiceSpy.getApiClients).toHaveBeenCalled();
  });

  it('loadClients() populates clients', () => {
    fixture.detectChanges();
    expect(component.clients).toEqual([mockClient]);
  });

  it('selectClient(client) sets selectedClient', () => {
    fixture.detectChanges();
    component.selectClient(mockClient);
    expect(component.selectedClient).toEqual(mockClient);
  });

  it('startEdit() sets editingClient to true', () => {
    fixture.detectChanges();
    component.selectedClient = mockClient;
    component.startEdit();
    expect(component.editingClient).toBeTrue();
  });

  it('cancelEdit() sets editingClient to false', () => {
    fixture.detectChanges();
    component.selectedClient = mockClient;
    component.startEdit();
    component.cancelEdit();
    expect(component.editingClient).toBeFalse();
  });

  it('getVersionStatusClass() returns correct CSS strings', () => {
    fixture.detectChanges();
    expect(component.getVersionStatusClass('Draft')).toBe('badge-draft');
    expect(component.getVersionStatusClass('Published')).toBe('badge-published');
    expect(component.getVersionStatusClass('Superseded')).toBe('badge-superseded');
    expect(component.getVersionStatusClass('Archived')).toBe('badge-archived');
    expect(component.getVersionStatusClass(undefined)).toBe('');
  });
});

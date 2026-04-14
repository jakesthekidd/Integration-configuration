import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of } from 'rxjs';

import { LookupTablesComponent } from './lookup-tables.component';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { LookupTable } from '../../models/lookup-table.model';

describe('LookupTablesComponent', () => {
  let component: LookupTablesComponent;
  let fixture: ComponentFixture<LookupTablesComponent>;
  let apiServiceSpy: jasmine.SpyObj<ApiService>;
  let generalServiceSpy: jasmine.SpyObj<GeneralService>;

  const mockSystems: import('../../models/tms-system.model').TmsSystem[] = [
    {
      id: 'tms-1',
      name: 'sys1',
      displayName: 'System 1',
      description: 'desc',
      version: '1.0',
      isActive: true,
      createdAt: new Date('2024-01-01'),
      updatedAt: new Date('2024-01-01'),
    },
  ];
  const mockLookupTable: LookupTable = {
    id: 'lt-1',
    name: 'LookupA',
    tmsSystemId: 'tms-1',
    fieldName: 'field1',
    description: '',
    mappings: '{"key1":"val1"}',
    defaultValue: '',
    isCaseSensitive: true,
    createdAt: new Date('2024-01-01'),
    updatedAt: new Date('2024-01-01'),
  };
  const dismissedResult = { isConfirmed: false, isDenied: false, isDismissed: true } as any;

  beforeEach(async () => {
    apiServiceSpy = jasmine.createSpyObj('ApiService', [
      'getLookupTables',
      'createLookupTable',
      'deleteLookupTable',
      'getTmsSystems',
    ]);
    generalServiceSpy = jasmine.createSpyObj('GeneralService', ['confirm', 'success', 'error']);

    apiServiceSpy.getTmsSystems.and.returnValue(
      of({ success: true, data: { systems: mockSystems, totalCount: 1 }, message: '' }),
    );
    apiServiceSpy.getLookupTables.and.returnValue(
      of({ success: true, data: { lookupTables: [mockLookupTable], totalCount: 1 }, message: '' }),
    );
    apiServiceSpy.createLookupTable.and.returnValue(of({ success: true, data: mockLookupTable, message: '' }));
    apiServiceSpy.deleteLookupTable.and.returnValue(of(undefined as any));
    generalServiceSpy.confirm.and.returnValue(
      Promise.resolve({ isConfirmed: true, isDenied: false, isDismissed: false, value: true } as any),
    );
    generalServiceSpy.success.and.returnValue(Promise.resolve(dismissedResult));
    generalServiceSpy.error.and.returnValue(Promise.resolve(dismissedResult));

    await TestBed.configureTestingModule({
      imports: [LookupTablesComponent],
      providers: [
        { provide: ApiService, useValue: apiServiceSpy },
        { provide: GeneralService, useValue: generalServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(LookupTablesComponent);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('ngOnInit calls loadTmsSystems() and loadLookupTables()', () => {
    fixture.detectChanges();
    expect(apiServiceSpy.getTmsSystems).toHaveBeenCalled();
    expect(apiServiceSpy.getLookupTables).toHaveBeenCalled();
  });

  it('loadLookupTables() populates lookupTables on success', () => {
    fixture.detectChanges();
    expect(component.lookupTables).toEqual([mockLookupTable]);
  });

  it('viewMappings(lookup) sets selectedLookup', () => {
    fixture.detectChanges();
    component.viewMappings(mockLookupTable);
    expect(component.selectedLookup).toEqual(mockLookupTable);
  });

  it('closeModal() clears selectedLookup', () => {
    fixture.detectChanges();
    component.viewMappings(mockLookupTable);
    component.closeModal();
    expect(component.selectedLookup).toBeNull();
  });

  it('isValidJSON() returns true for valid JSON', () => {
    fixture.detectChanges();
    expect(component.isValidJSON('{"key":"value"}')).toBeTrue();
    expect(component.isValidJSON('[1,2,3]')).toBeTrue();
    expect(component.isValidJSON('"hello"')).toBeTrue();
  });

  it('isValidJSON() returns false for invalid JSON', () => {
    fixture.detectChanges();
    expect(component.isValidJSON('{invalid}')).toBeFalse();
    expect(component.isValidJSON('')).toBeFalse();
  });
});

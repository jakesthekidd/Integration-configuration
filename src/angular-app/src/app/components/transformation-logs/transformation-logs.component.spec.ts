import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of, throwError } from 'rxjs';

import { TransformationLogsComponent } from './transformation-logs.component';
import { ApiService } from '../../services/api.service';
import { TransformationLogSummary } from '../../models/transformation-log.model';

describe('TransformationLogsComponent', () => {
  let component: TransformationLogsComponent;
  let fixture: ComponentFixture<TransformationLogsComponent>;
  let apiServiceSpy: jasmine.SpyObj<ApiService>;

  const mockLogs: TransformationLogSummary[] = [
    {
      id: 'log-1',
      templateId: 'tmpl-1',
      timestamp: '2024-01-01T00:00:00Z',
      status: 'Success',
      durationMs: 100,
      recordCount: 5,
      hasErrors: false,
      hasOutput: true,
    },
    {
      id: 'log-2',
      templateId: 'tmpl-1',
      timestamp: '2024-01-02T00:00:00Z',
      status: 'Error',
      durationMs: 50,
      recordCount: 0,
      hasErrors: true,
      hasOutput: false,
    },
    {
      id: 'log-3',
      templateId: 'tmpl-1',
      timestamp: '2024-01-03T00:00:00Z',
      status: 'PartialSuccess',
      durationMs: 75,
      recordCount: 3,
      hasErrors: false,
      hasOutput: true,
    },
  ];

  beforeEach(async () => {
    apiServiceSpy = jasmine.createSpyObj('ApiService', [
      'getTransformationLogs', 'getTransformationLogById'
    ]);

    apiServiceSpy.getTransformationLogs.and.returnValue(
      of({ success: true, data: { logs: mockLogs, totalCount: mockLogs.length }, message: '' })
    );
    apiServiceSpy.getTransformationLogById.and.returnValue(
      of({ success: true, data: { ...mockLogs[0], inputData: '{}', outputData: '{}' }, message: '' })
    );

    await TestBed.configureTestingModule({
      imports: [TransformationLogsComponent],
      providers: [
        { provide: ApiService, useValue: apiServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(TransformationLogsComponent);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('ngOnInit calls load()', () => {
    fixture.detectChanges();
    expect(apiServiceSpy.getTransformationLogs).toHaveBeenCalled();
  });

  it('load() populates logs on success', () => {
    fixture.detectChanges();
    expect(component.logs).toEqual(mockLogs);
  });

  it('load() sets error on failure', () => {
    apiServiceSpy.getTransformationLogs.and.returnValue(throwError(() => new Error('network error')));
    fixture.detectChanges();
    expect(component.error).toBe('Failed to load transformation logs');
  });

  it('total getter returns correct count', () => {
    fixture.detectChanges();
    expect(component.total).toBe(mockLogs.length);
  });

  it('counts getter groups by status', () => {
    fixture.detectChanges();
    const counts = component.counts;
    expect(counts['Success']).toBe(1);
    expect(counts['Error']).toBe(1);
    expect(counts['PartialSuccess']).toBe(1);
  });

  it('statusClass() returns correct ngClass object', () => {
    fixture.detectChanges();
    const successClass = component.statusClass('Success');
    expect(successClass['badge-success']).toBeTrue();
    expect(successClass['badge-error']).toBeFalse();

    const errorClass = component.statusClass('Error');
    expect(errorClass['badge-error']).toBeTrue();
    expect(errorClass['badge-success']).toBeFalse();

    const warningClass = component.statusClass('Warning');
    expect(warningClass['badge-warning']).toBeTrue();

    const partialClass = component.statusClass('PartialSuccess');
    expect(partialClass['badge-partial']).toBeTrue();
  });

  it('statusLabel() returns human-readable status label', () => {
    fixture.detectChanges();
    expect(component.statusLabel('Success')).toBe('Success');
    expect(component.statusLabel('Error')).toBe('Error');
    expect(component.statusLabel('PartialSuccess')).toBe('Partial');
    expect(component.statusLabel('Warning')).toBe('Warning');
  });
});

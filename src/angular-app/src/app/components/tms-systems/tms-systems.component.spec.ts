import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of, throwError } from 'rxjs';

import { TmsSystemsComponent } from './tms-systems.component';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';

describe('TmsSystemsComponent', () => {
  let component: TmsSystemsComponent;
  let fixture: ComponentFixture<TmsSystemsComponent>;
  let apiServiceSpy: jasmine.SpyObj<ApiService>;
  let generalServiceSpy: jasmine.SpyObj<GeneralService>;

  const mockSystems: import('../../models/tms-system.model').TmsSystem[] = [
    {
      id: '1',
      name: 'sys1',
      displayName: 'System 1',
      description: 'desc',
      version: '1.0',
      isActive: true,
      createdAt: new Date('2024-01-01'),
      updatedAt: new Date('2024-01-01'),
    },
  ];

  const successConfirmResult = { isConfirmed: true, isDenied: false, isDismissed: false, value: true } as any;
  const dismissedResult = { isConfirmed: false, isDenied: false, isDismissed: true } as any;

  beforeEach(async () => {
    apiServiceSpy = jasmine.createSpyObj('ApiService', ['getTmsSystems', 'createTmsSystem', 'deleteTmsSystem']);
    generalServiceSpy = jasmine.createSpyObj('GeneralService', ['confirm', 'success', 'error']);

    apiServiceSpy.getTmsSystems.and.returnValue(
      of({ success: true, data: { systems: mockSystems, totalCount: 1 }, message: '' }),
    );
    apiServiceSpy.createTmsSystem.and.returnValue(of({ success: true, data: mockSystems[0], message: '' }));
    apiServiceSpy.deleteTmsSystem.and.returnValue(of(undefined as any));
    generalServiceSpy.confirm.and.returnValue(Promise.resolve(successConfirmResult));
    generalServiceSpy.success.and.returnValue(Promise.resolve(dismissedResult));
    generalServiceSpy.error.and.returnValue(Promise.resolve(dismissedResult));

    await TestBed.configureTestingModule({
      imports: [TmsSystemsComponent],
      providers: [
        { provide: ApiService, useValue: apiServiceSpy },
        { provide: GeneralService, useValue: generalServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(TmsSystemsComponent);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('ngOnInit should call loadSystems which calls apiService.getTmsSystems', () => {
    fixture.detectChanges();
    expect(apiServiceSpy.getTmsSystems).toHaveBeenCalled();
  });

  it('loadSystems() populates systems on success', () => {
    fixture.detectChanges();
    expect(component.systems).toEqual(mockSystems);
  });

  it('loadSystems() sets error on failure', () => {
    apiServiceSpy.getTmsSystems.and.returnValue(throwError(() => new Error('network')));
    fixture.detectChanges();
    expect(component.error).toBe('Failed to load TMS systems');
  });

  it('createSystem() calls apiService.createTmsSystem and reloads', () => {
    fixture.detectChanges();
    apiServiceSpy.getTmsSystems.calls.reset();
    component.createSystem();
    expect(apiServiceSpy.createTmsSystem).toHaveBeenCalled();
    expect(apiServiceSpy.getTmsSystems).toHaveBeenCalled();
  });

  it('deleteSystem() calls generalService.confirm, then apiService.deleteTmsSystem when confirmed', async () => {
    fixture.detectChanges();
    component.deleteSystem('1');
    await fixture.whenStable();
    expect(generalServiceSpy.confirm).toHaveBeenCalled();
    expect(apiServiceSpy.deleteTmsSystem).toHaveBeenCalledWith('1');
  });
});

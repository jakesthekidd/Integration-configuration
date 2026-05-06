import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of, throwError } from 'rxjs';

import { PatnersComponent } from './patners.component';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { Partner } from '../../models/partner.model';

describe('PatnersComponent', () => {
  let component: PatnersComponent;
  let fixture: ComponentFixture<PatnersComponent>;
  let apiServiceSpy: jasmine.SpyObj<ApiService>;
  let generalServiceSpy: jasmine.SpyObj<GeneralService>;

  const mockPartner: Partner = {
    id: 'partner-1',
    name: 'Test Partner',
    description: 'A test partner',
    createdAt: new Date('2024-01-01T00:00:00Z'),
    updatedAt: new Date('2024-01-01T00:00:00Z'),
    isDeleted: false,
    revision: 1,
  };

  const dismissedResult = { isConfirmed: false, isDenied: false, isDismissed: true } as any;
  const confirmedResult = { isConfirmed: true, isDenied: false, isDismissed: false, value: true } as any;

  beforeEach(async () => {
    apiServiceSpy = jasmine.createSpyObj('ApiService', ['getPartners', 'createPartner', 'deletePartner']);
    generalServiceSpy = jasmine.createSpyObj('GeneralService', ['confirm', 'success', 'error']);

    apiServiceSpy.getPartners.and.returnValue(
      of({ success: true, data: { partners: [mockPartner], totalCount: 1, page: 1, pageSize: 10 }, message: '' }),
    );
    apiServiceSpy.createPartner.and.returnValue(of({ success: true, data: mockPartner, message: '' }));
    apiServiceSpy.deletePartner.and.returnValue(of(undefined as any));

    generalServiceSpy.confirm.and.returnValue(Promise.resolve(confirmedResult));
    generalServiceSpy.success.and.returnValue(Promise.resolve(dismissedResult));
    generalServiceSpy.error.and.returnValue(Promise.resolve(dismissedResult));

    await TestBed.configureTestingModule({
      imports: [PatnersComponent],
      providers: [
        { provide: ApiService, useValue: apiServiceSpy },
        { provide: GeneralService, useValue: generalServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(PatnersComponent);
    component = fixture.componentInstance;
  });


  it('should create the component', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });


  it('ngOnInit should call loadPartners', () => {
    fixture.detectChanges();
    expect(apiServiceSpy.getPartners).toHaveBeenCalled();
  });


  it('loadPartners() populates partners on success', () => {
    fixture.detectChanges();
    expect(component.partners).toEqual([mockPartner]);
    expect(component.loading).toBeFalse();
    expect(component.error).toBeNull();
  });

  it('loadPartners() sets error on failure', () => {
    apiServiceSpy.getPartners.and.returnValue(throwError(() => new Error('Network error')));
    fixture.detectChanges();
    expect(component.error).toBe('Failed to load partners.');
    expect(component.loading).toBeFalse();
    expect(component.partners).toEqual([]);
  });


  it('createPartner() does nothing when name is blank', () => {
    fixture.detectChanges();
    component.newPartner = { name: '   ', description: '' };
    component.createPartner();
    expect(apiServiceSpy.createPartner).not.toHaveBeenCalled();
  });

  it('createPartner() resets form and reloads on success', fakeAsync(() => {
    fixture.detectChanges();
    component.showCreateForm = true;
    component.newPartner = { name: 'New Partner', description: 'desc' };
    component.createPartner();
    tick();
    expect(component.showCreateForm).toBeFalse();
    expect(component.newPartner).toEqual({ name: '', description: '' });
    expect(apiServiceSpy.getPartners).toHaveBeenCalledTimes(2); // ngOnInit + after create
    expect(generalServiceSpy.success).toHaveBeenCalledWith('Partner created successfully.');
  }));

  it('createPartner() clears creating flag after success', fakeAsync(() => {
    fixture.detectChanges();
    component.newPartner = { name: 'New Partner', description: '' };
    component.createPartner();
    tick();
    expect(component.creating).toBeFalse();
  }));

  it('createPartner() shows error and clears creating flag on failure', fakeAsync(() => {
    const errResponse = { error: { message: 'Partner name already exists.' } };
    apiServiceSpy.createPartner.and.returnValue(throwError(() => errResponse));
    fixture.detectChanges();
    component.newPartner = { name: 'Duplicate Partner', description: '' };
    component.createPartner();
    tick();
    expect(generalServiceSpy.error).toHaveBeenCalledWith('Partner name already exists.');
    expect(component.creating).toBeFalse();
  }));

  it('createPartner() falls back to generic error message when none provided', fakeAsync(() => {
    apiServiceSpy.createPartner.and.returnValue(throwError(() => ({})));
    fixture.detectChanges();
    component.newPartner = { name: 'Some Partner', description: '' };
    component.createPartner();
    tick();
    expect(generalServiceSpy.error).toHaveBeenCalledWith('Failed to create partner.');
  }));


  it('deletePartner() calls deletePartner API and reloads on confirm', fakeAsync(() => {
    fixture.detectChanges();
    component.deletePartner('partner-1', 'Test Partner');
    tick();
    expect(apiServiceSpy.deletePartner).toHaveBeenCalledWith('partner-1');
    expect(generalServiceSpy.success).toHaveBeenCalledWith('Partner deleted successfully.');
    expect(apiServiceSpy.getPartners).toHaveBeenCalledTimes(2); // ngOnInit + after delete
  }));

  it('deletePartner() does not call API when dialog is dismissed', fakeAsync(() => {
    generalServiceSpy.confirm.and.returnValue(Promise.resolve(dismissedResult));
    fixture.detectChanges();
    component.deletePartner('partner-1', 'Test Partner');
    tick();
    expect(apiServiceSpy.deletePartner).not.toHaveBeenCalled();
  }));

  it('deletePartner() sets error and clears deleting flag on failure', fakeAsync(() => {
    apiServiceSpy.deletePartner.and.returnValue(throwError(() => new Error('Server error')));
    fixture.detectChanges();
    component.deletePartner('partner-1', 'Test Partner');
    tick();
    expect(component.error).toBe('Failed to delete partner.');
    expect(component.deleting['partner-1']).toBeUndefined();
  }));


  it('cancelCreate() hides form and resets newPartner', () => {
    fixture.detectChanges();
    component.showCreateForm = true;
    component.newPartner = { name: 'Draft', description: 'draft desc' };
    component.error = 'some error';
    component.cancelCreate();
    expect(component.showCreateForm).toBeFalse();
    expect(component.newPartner).toEqual({ name: '', description: '' });
    expect(component.error).toBeNull();
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of, throwError } from 'rxjs';

import { CustomersComponent } from './customers.component';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { Customer } from '../../models/customer.model';

describe('CustomersComponent', () => {
  let component: CustomersComponent;
  let fixture: ComponentFixture<CustomersComponent>;
  let apiServiceSpy: jasmine.SpyObj<ApiService>;
  let generalServiceSpy: jasmine.SpyObj<GeneralService>;

  const mockCustomer: Customer = {
    customerId: 'cust-1',
    customerName: 'Test Customer',
    tmsName: 'TruckMate',
    lastSyncTime: '2024-01-01T00:00:00Z',
    enabled: true,
    outboundEnabled: false,
    credentials: {},
  };

  const dismissedResult = { isConfirmed: false, isDenied: false, isDismissed: true } as any;

  beforeEach(async () => {
    apiServiceSpy = jasmine.createSpyObj('ApiService', [
      'getCustomers', 'createCustomer', 'updateCustomer', 'deleteCustomer', 'setCustomerStatus'
    ]);
    generalServiceSpy = jasmine.createSpyObj('GeneralService', ['confirm', 'success', 'error']);

    apiServiceSpy.getCustomers.and.returnValue(of({ success: true, data: { customers: [mockCustomer], totalCount: 1 }, message: '' }));
    apiServiceSpy.createCustomer.and.returnValue(of({ success: true, data: mockCustomer, message: '' }));
    apiServiceSpy.updateCustomer.and.returnValue(of({ success: true, data: mockCustomer, message: '' }));
    apiServiceSpy.deleteCustomer.and.returnValue(of(undefined as any));
    apiServiceSpy.setCustomerStatus.and.returnValue(of({ success: true, data: mockCustomer, message: '' }));
    generalServiceSpy.confirm.and.returnValue(Promise.resolve({ isConfirmed: true, isDenied: false, isDismissed: false, value: true } as any));
    generalServiceSpy.success.and.returnValue(Promise.resolve(dismissedResult));
    generalServiceSpy.error.and.returnValue(Promise.resolve(dismissedResult));

    await TestBed.configureTestingModule({
      imports: [CustomersComponent],
      providers: [
        { provide: ApiService, useValue: apiServiceSpy },
        { provide: GeneralService, useValue: generalServiceSpy },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomersComponent);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('ngOnInit should call loadCustomers', () => {
    fixture.detectChanges();
    expect(apiServiceSpy.getCustomers).toHaveBeenCalled();
  });

  it('loadCustomers() populates customers on success', () => {
    fixture.detectChanges();
    expect(component.customers).toEqual([mockCustomer]);
  });

  it('toggleCreateForm() flips showCreateForm', () => {
    fixture.detectChanges();
    expect(component.showCreateForm).toBeFalse();
    component.toggleCreateForm();
    expect(component.showCreateForm).toBeTrue();
    component.toggleCreateForm();
    expect(component.showCreateForm).toBeFalse();
  });

  it('startEdit(customer) sets editingCustomer', () => {
    fixture.detectChanges();
    component.startEdit(mockCustomer);
    expect(component.editingCustomer).toEqual(mockCustomer);
  });

  it('cancelEdit() clears editingCustomer', () => {
    fixture.detectChanges();
    component.startEdit(mockCustomer);
    component.cancelEdit();
    expect(component.editingCustomer).toBeNull();
  });
});

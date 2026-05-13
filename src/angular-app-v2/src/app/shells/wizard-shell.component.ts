import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { StepsModule } from 'primeng/steps';
import { ButtonModule } from 'primeng/button';

import { CustomersComponent } from '../components/customers/customers.component';
import { ApiService } from '../services/api.service';
import { WizardStateService } from '../wizard/wizard-state.service';
import { StepSelectCustomerComponent } from '../wizard/steps/step-select-customer.component';
import { StepPickApplicationComponent } from '../wizard/steps/step-pick-application.component';
import { StepPickCapabilityComponent } from '../wizard/steps/step-pick-capability.component';
import { StepPickConnectionComponent } from '../wizard/steps/step-pick-connection.component';
import { StepAddCredentialsComponent } from '../wizard/steps/step-add-credentials.component';
import { StepPickTemplateComponent } from '../wizard/steps/step-pick-template.component';
import { StepCustomizeMappingComponent } from '../wizard/steps/step-customize-mapping.component';
import { StepReviewTestComponent } from '../wizard/steps/step-review-test.component';
import { StepPublishComponent } from '../wizard/steps/step-publish.component';
import { StepActivateComponent } from '../wizard/steps/step-activate.component';
import { credentialsAreValid } from '../constants/connection-credentials.constants';

type WizardMode = 'list' | 'new' | 'add-deployment' | 'edit';

const STEP_LABELS = [
  'Select Customer',
  'Application',
  'Capability',
  'Connection',
  'Credentials',
  'Mapping Template',
  'Customize Mapping',
  'Review & Test',
  'Publish',
  'Activate',
] as const;

@Component({
  selector: 'app-wizard-shell',
  imports: [
    CustomersComponent,
    StepSelectCustomerComponent,
    StepPickApplicationComponent,
    StepPickCapabilityComponent,
    StepPickConnectionComponent,
    StepAddCredentialsComponent,
    StepPickTemplateComponent,
    StepCustomizeMappingComponent,
    StepReviewTestComponent,
    StepPublishComponent,
    StepActivateComponent,
    StepsModule,
    ButtonModule,
  ],
  template: `
    @if (mode() === 'list') {
      <div class="customers-view">
        <div class="tf-section-header">
          <div>
            <h2>Customers</h2>
            <p>Pick a customer to manage their integrations.</p>
          </div>
        </div>
        <div class="customers-body">
          <app-customers></app-customers>
        </div>
      </div>
    } @else {
      <div class="wizard-view">
        <div class="breadcrumb-bar">
          <a (click)="navigateToCustomers()">All Customers</a>
          @if (customerCrumbLabel(); as label) {
            <span class="sep">/</span>
            <a (click)="navigateToCustomerDetail()">{{ label }}</a>
          }
          <span class="sep">/</span>
          <span class="current">{{ mode() === 'edit' ? 'Edit Deployment' : 'Set Up Wizard' }}</span>
        </div>

        <p-steps
          class="wizard-steps"
          [model]="stepItems()"
          [activeIndex]="currentStep()"
          [readonly]="!stepStripClickable()"
        ></p-steps>

        <div class="step-content-wrap">
          <div class="step-card">
            @switch (currentStep()) {
              @case (0) {
                <app-step-select-customer></app-step-select-customer>
              }
              @case (1) {
                <app-step-pick-application></app-step-pick-application>
              }
              @case (2) {
                <app-step-pick-capability></app-step-pick-capability>
              }
              @case (3) {
                <app-step-pick-connection></app-step-pick-connection>
              }
              @case (4) {
                <app-step-add-credentials></app-step-add-credentials>
              }
              @case (5) {
                <app-step-pick-template></app-step-pick-template>
              }
              @case (6) {
                <app-step-customize-mapping></app-step-customize-mapping>
              }
              @case (7) {
                <app-step-review-test></app-step-review-test>
              }
              @case (8) {
                <app-step-publish></app-step-publish>
              }
              @case (9) {
                <app-step-activate (finished)="finishWizard()"></app-step-activate>
              }
            }
          </div>
        </div>

        <div class="wizard-footer">
          <p-button
            label="Back"
            icon="pi pi-arrow-left"
            severity="secondary"
            [outlined]="true"
            [disabled]="!canGoBack()"
            (onClick)="goBack()"
          />
          <p-button
            label="Next"
            icon="pi pi-arrow-right"
            iconPos="right"
            severity="contrast"
            [disabled]="!canAdvance() || currentStep() === STEP_LABELS.length - 1"
            (onClick)="goNext()"
          />
        </div>
      </div>
    }
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        height: 100%;
        min-height: 0;
      }

      .muted {
        color: var(--tf-text-muted);
        font-size: 13px;
      }

      /* ----- Customers list view ----- */
      .customers-view {
        display: flex;
        flex-direction: column;
        flex: 1 1 auto;
        min-height: 0;
      }
      .customers-body {
        flex: 1 1 auto;
        min-height: 0;
        overflow: auto;
        padding: var(--tf-space-4) var(--tf-space-6);
      }

      /* ----- Wizard view ----- */
      .wizard-view {
        display: flex;
        flex-direction: column;
        flex: 1 1 auto;
        min-height: 0;
      }

      .breadcrumb-bar {
        flex-shrink: 0;
        background: var(--tf-blue-100);
        padding: 8px 20px;
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 13px;
      }
      .breadcrumb-bar a {
        color: var(--tf-blue-400);
        text-decoration: none;
        cursor: pointer;
      }
      .breadcrumb-bar a:hover {
        text-decoration: underline;
      }
      .breadcrumb-bar .sep {
        color: var(--tf-blue-900);
        font-weight: 700;
      }
      .breadcrumb-bar .current {
        color: var(--tf-blue-900);
        font-weight: 700;
      }

      .wizard-steps {
        flex-shrink: 0;
        display: block;
        background: var(--tf-surface-hover);
        border-bottom: 1px solid var(--tf-slate-400);
        padding: 4px 16px;
      }
      :host ::ng-deep .wizard-steps .p-steps,
      :host ::ng-deep .wizard-steps .p-steps-list {
        background: transparent !important;
        margin: 0 !important;
        padding: 0 !important;
      }
      :host ::ng-deep .wizard-steps .p-steps-item {
        padding: 0 !important;
      }
      :host ::ng-deep .wizard-steps .p-steps-item-link {
        background: transparent !important;
        padding: 2px 0 !important;
        flex-direction: row !important;
        gap: 6px !important;
      }
      :host ::ng-deep .wizard-steps .p-steps-item-number {
        width: 18px !important;
        height: 18px !important;
        min-width: 18px !important;
        font-size: 10px !important;
        line-height: 14px !important;
        font-weight: 700 !important;
        margin: 0 !important;
        border-width: 1px !important;
      }
      :host ::ng-deep .wizard-steps .p-steps-item-label {
        font-size: 11px !important;
        font-weight: 600 !important;
        margin: 0 !important;
        padding: 0 !important;
        white-space: nowrap;
      }

      .step-content-wrap {
        flex: 1 1 auto;
        min-height: 0;
        overflow: auto;
        padding: 24px;
        display: flex;
        justify-content: center;
        align-items: flex-start;
      }
      .step-card {
        background: white;
        border: 1px solid var(--tf-slate-400);
        border-radius: 8px;
        padding: 24px;
        width: 100%;
        max-width: 1264px;
      }

      .wizard-footer {
        flex-shrink: 0;
        background: var(--tf-blue-900);
        padding: 16px;
        display: flex;
        justify-content: space-between;
        align-items: center;
      }
    `,
  ],
})
export class WizardShellComponent implements OnInit {
  protected readonly STEP_LABELS = STEP_LABELS;

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = inject(ApiService);
  private state = inject(WizardStateService);

  mode = signal<WizardMode>('list');
  currentStep = signal<number>(0);

  /** Source customer id captured from /customer/:id/new — used for breadcrumb + finish nav. */
  contextCustomerId = signal<string>('');

  /** Step strip is clickable in edit mode (any step jumpable) and once core context is set. */
  stepStripClickable = computed<boolean>(() => this.mode() === 'edit');

  stepItems = computed<MenuItem[]>(() =>
    STEP_LABELS.map((label, index) => ({
      label,
      command: () => {
        if (this.stepStripClickable()) this.currentStep.set(index);
      },
    })),
  );

  canAdvance = computed<boolean>(() => {
    const d = this.state.draft();
    switch (this.currentStep()) {
      case 0:
        return !!d.customerId;
      case 1:
        return !!d.applicationId;
      case 2:
        return !!d.capabilityId;
      case 3:
        return !!d.connectionId;
      case 4:
        return credentialsAreValid(d.connectionId, d.credentials);
      case 5:
        return !!d.templateId;
      default:
        return true;
    }
  });

  canGoBack = computed<boolean>(() => {
    // In add-deployment mode, step 1 (Application) is the floor — can't go back to Select Customer.
    if (this.mode() === 'add-deployment') return this.currentStep() > 1;
    return this.currentStep() > 0;
  });

  /** Breadcrumb middle segment: the customer name when entered from a customer detail. */
  customerCrumbLabel = signal<string>('');

  ngOnInit() {
    const wizardMode = this.route.snapshot.data['wizardMode'] as WizardMode | undefined;
    const customerIdParam = this.route.snapshot.paramMap.get('id') ?? '';
    const deploymentIdParam = this.route.snapshot.paramMap.get('deploymentId') ?? '';

    if (!wizardMode) {
      this.mode.set('list');
      this.state.reset();
      return;
    }

    if (wizardMode === 'new') {
      this.state.reset();
      this.mode.set('new');
      this.currentStep.set(0);
      return;
    }

    if (wizardMode === 'add-deployment' && customerIdParam) {
      this.state.reset();
      this.state.patch({ customerId: customerIdParam });
      this.contextCustomerId.set(customerIdParam);
      this.mode.set('add-deployment');
      this.currentStep.set(1);
      this.loadCustomerName(customerIdParam);
      return;
    }

    if (wizardMode === 'edit' && deploymentIdParam) {
      this.mode.set('edit');
      this.loadDeployment(deploymentIdParam);
      return;
    }

    // Fallback: act as list.
    this.mode.set('list');
  }

  // ─── Navigation helpers ──────────────────────────────────────────────

  startNewCustomer() {
    this.router.navigate(['/wizard/new']);
  }

  navigateToCustomers() {
    this.router.navigate(['/wizard']);
  }

  navigateToCustomerDetail() {
    const id = this.contextCustomerId() || this.state.draft().customerId;
    if (id) this.router.navigate(['/wizard', 'customer', id]);
  }

  // ─── Step navigation ─────────────────────────────────────────────────

  goNext() {
    if (this.currentStep() < STEP_LABELS.length - 1 && this.canAdvance()) {
      this.currentStep.update((s) => s + 1);
    }
  }

  goBack() {
    if (this.canGoBack()) this.currentStep.update((s) => s - 1);
  }

  /** Called when activate emits finished — go back to where the user came from. */
  finishWizard() {
    const customerId = this.contextCustomerId() || this.state.draft().customerId;
    this.state.reset();
    if (customerId) {
      this.router.navigate(['/wizard', 'customer', customerId]);
    } else {
      this.router.navigate(['/wizard']);
    }
  }

  // ─── Loaders ─────────────────────────────────────────────────────────

  private loadCustomerName(id: string) {
    this.api.getCustomerById(id).subscribe((res) => {
      if (res.success && res.data) this.customerCrumbLabel.set(res.data.customerName);
    });
  }

  private loadDeployment(deploymentId: string) {
    this.api.getDeploymentById(deploymentId).subscribe((res) => {
      if (!res.success || !res.data) {
        // Bail back to customers list on missing deployment.
        this.router.navigate(['/wizard']);
        return;
      }
      const d = res.data;
      this.state.reset();
      this.state.patch({
        customerId: d.customerId,
        applicationId: d.applicationId,
        capabilityId: d.capabilityId,
        connectionId: d.connectionId,
        templateId: d.forkedFromTemplateId || '',
        templateVersion: d.forkedFromTemplateVersion,
        apiClientId: d.apiClientId || '',
      });
      this.contextCustomerId.set(d.customerId);
      this.loadCustomerName(d.customerId);
      // Jump straight to Customize Mapping — most common edit landing.
      this.currentStep.set(6);
    });
  }
}

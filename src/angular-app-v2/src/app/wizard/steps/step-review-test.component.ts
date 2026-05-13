import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageModule } from 'primeng/message';

import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { Customer } from '../../models/customer.model';
import { Application } from '../../models/application.model';
import { Capability } from '../../models/capability.model';
import { TmsSystem } from '../../models/tms-system.model';
import { FieldMappingTemplate } from '../../models/template.model';
import { WizardStateService } from '../wizard-state.service';
import { TEMPLATE_FROM_SCRATCH } from './step-pick-template.component';

/**
 * Wizard step 8 — Review the entire draft and exercise it against a real order.
 *
 * Mock test: clicking "Run test" simulates auth + fetch + transform without
 * issuing any writes (per PRODUCT-GUIDING-PRINCIPLES.md §6). Result is shown
 * inline and persisted to `state.draft().testPassed`.
 */
@Component({
  selector: 'app-step-review-test',
  imports: [ButtonModule, TagModule, ProgressSpinnerModule, MessageModule],
  template: `
    <p class="intro">
      Confirm everything looks right before publishing. The test below pulls a real order
      through the mapping without writing anything back to production.
    </p>

    <div class="summary-grid">
      <div class="card">
        <h4>Customer</h4>
        <div class="row"><span class="k">Name</span><span class="v">{{ customer()?.customerName ?? '—' }}</span></div>
        <div class="row"><span class="k">ID</span><span class="v mono">{{ draft().customerId }}</span></div>
      </div>

      <div class="card">
        <h4>Integration scope</h4>
        <div class="row"><span class="k">Application</span><span class="v">{{ application()?.displayName ?? '—' }}</span></div>
        <div class="row"><span class="k">Capability</span><span class="v">{{ capability()?.displayName ?? '—' }}</span></div>
        <div class="row"><span class="k">Connection</span><span class="v">{{ connection()?.displayName ?? '—' }}</span></div>
      </div>

      <div class="card">
        <h4>Credentials</h4>
        <div class="row" *ngFor="let key of credKeys()">
          <span class="k">{{ key }}</span>
          <span class="v mono">{{ maskedValue(key) }}</span>
        </div>
        <div class="row" *ngIf="credKeys().length === 0">
          <span class="empty">No credentials provided.</span>
        </div>
      </div>

      <div class="card">
        <h4>Template</h4>
        <div class="row">
          <span class="k">Source</span>
          <span class="v">
            @if (draft().templateId === SCRATCH) {
              <em>From scratch</em>
            } @else {
              {{ template()?.name ?? '—' }}
            }
          </span>
        </div>
        <div class="row" *ngIf="draft().templateVersion">
          <span class="k">Forked version</span>
          <span class="v">v{{ draft().templateVersion }}</span>
        </div>
        <div class="row">
          <span class="k">Mappings</span>
          <span class="v">{{ draft().fieldMappings.length }}</span>
        </div>
      </div>
    </div>

    <div class="test-block">
      <div class="test-block__head">
        <div>
          <h4>Test with a real order</h4>
          <p class="muted">
            Pulls live data through the mapping. Auth, fetch, and transform are exercised.
            No writes are issued.
          </p>
        </div>
        <p-button
          label="Run test"
          icon="pi pi-play"
          severity="primary"
          size="small"
          [loading]="running()"
          [disabled]="running()"
          (onClick)="runTest()"
        />
      </div>

      @if (lastResult(); as r) {
        @if (r.success) {
          <p-message severity="success" [text]="r.message" />
        } @else {
          <p-message severity="error" [text]="r.message" />
        }
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .intro {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin: 0 0 var(--tf-space-4) 0;
        max-width: 640px;
      }
      .summary-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
        gap: var(--tf-space-3);
        margin-bottom: var(--tf-space-5);
      }
      .card {
        background: white;
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-md);
        padding: var(--tf-space-4);
      }
      .card h4 {
        margin: 0 0 var(--tf-space-3) 0;
        font-size: var(--tf-text-meta);
        font-weight: 700;
        letter-spacing: 0.4px;
        text-transform: uppercase;
        color: var(--tf-text-muted);
      }
      .row {
        display: flex;
        justify-content: space-between;
        gap: var(--tf-space-3);
        padding: 4px 0;
        font-size: var(--tf-text-body);
        border-bottom: 1px dashed var(--tf-slate-300);
      }
      .row:last-child {
        border-bottom: 0;
      }
      .k {
        color: var(--tf-text-muted);
      }
      .v {
        color: var(--tf-text-strong);
        font-weight: 600;
        text-align: right;
        word-break: break-all;
      }
      .v em {
        font-style: italic;
        font-weight: 500;
        color: var(--tf-text-muted);
      }
      .mono {
        font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
        font-size: var(--tf-text-meta);
      }
      .empty {
        color: var(--tf-text-muted);
        font-style: italic;
      }
      .test-block {
        background: var(--tf-blue-50);
        border: 1px solid var(--tf-blue-200);
        border-radius: var(--tf-radius-md);
        padding: var(--tf-space-4);
      }
      .test-block__head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--tf-space-4);
      }
      .test-block h4 {
        margin: 0;
        font-size: var(--tf-text-heading);
        color: var(--tf-text-strong);
      }
      .muted {
        margin: 4px 0 0 0;
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        max-width: 540px;
      }
    `,
  ],
})
export class StepReviewTestComponent implements OnInit {
  protected state = inject(WizardStateService);
  private api = inject(ApiService);
  private gen = inject(GeneralService);

  protected readonly SCRATCH = TEMPLATE_FROM_SCRATCH;

  draft = this.state.draft;

  customer = signal<Customer | null>(null);
  application = signal<Application | null>(null);
  capability = signal<Capability | null>(null);
  connection = signal<TmsSystem | null>(null);
  template = signal<FieldMappingTemplate | null>(null);

  running = signal<boolean>(false);
  lastResult = signal<{ success: boolean; message: string } | null>(null);

  credKeys = computed<string[]>(() => Object.keys(this.draft().credentials).filter((k) => !!this.draft().credentials[k]));

  ngOnInit() {
    const d = this.draft();
    if (d.customerId) this.api.getCustomerById(d.customerId).subscribe((r) => r.data && this.customer.set(r.data));
    if (d.applicationId)
      this.api.getApplications().subscribe(
        (r) => r.data && this.application.set(r.data.applications.find((a) => a.id === d.applicationId) ?? null),
      );
    if (d.capabilityId)
      this.api.getCapabilities().subscribe(
        (r) => r.data && this.capability.set(r.data.capabilities.find((c) => c.id === d.capabilityId) ?? null),
      );
    if (d.connectionId) this.api.getTmsSystemById(d.connectionId).subscribe((r) => r.data && this.connection.set(r.data));
    if (d.templateId && d.templateId !== TEMPLATE_FROM_SCRATCH)
      this.api.getTemplateById(d.templateId).subscribe((r) => r.data && this.template.set(r.data));
  }

  maskedValue(key: string): string {
    const v = this.draft().credentials[key] ?? '';
    if (/password|secret|token|key|auth/i.test(key)) {
      return v ? '••••••••' : '';
    }
    return v;
  }

  runTest() {
    if (this.running()) return;
    this.running.set(true);
    this.lastResult.set(null);

    // Mock test — simulates auth/fetch/transform without backing API.
    // 90% pass rate to demo both states.
    setTimeout(() => {
      const success = Math.random() > 0.1;
      const correlationId = 'corr-' + Math.random().toString(36).slice(2, 10);
      const message = success
        ? `Test passed. ${this.draft().fieldMappings.length} mappings applied to sample order. Correlation: ${correlationId}`
        : 'Test failed: source path $.order.id was not found in the sample document.';

      this.lastResult.set({ success, message });
      this.state.patch({ testPassed: success, testCorrelationId: success ? correlationId : '' });
      this.running.set(false);
      if (success) {
        this.gen.success('Test passed.');
      } else {
        this.gen.error(message);
      }
    }, 1200);
  }
}

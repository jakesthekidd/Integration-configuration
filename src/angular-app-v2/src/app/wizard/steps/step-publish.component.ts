import { Component, inject, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';

import { GeneralService } from '../../services/general.service';
import { WizardStateService } from '../wizard-state.service';

/**
 * Wizard step 9 — Publish the CustomerTemplate as a new Deployment in `Tested`
 * (or `Draft` if the test never ran). Publishing snapshots the mappings; it
 * does NOT make the deployment live — that's step 10 (Activate).
 *
 * Mock action — real wire-up will POST to the deployments API when backend lands.
 */
@Component({
  selector: 'app-step-publish',
  imports: [ButtonModule, MessageModule],
  template: `
    <div class="hero">
      <h3>Publish this customer's mapping</h3>
      <p class="muted">
        Publishing creates a new Deployment snapshot for
        <strong>{{ state.draft().customerId || 'this customer' }}</strong>. It is NOT yet live;
        the snapshot enters the {{ state.draft().testPassed ? 'Tested' : 'Draft' }} state and
        is ready to activate in the next step.
      </p>
      @if (!state.draft().testPassed) {
        <p-message
          severity="warn"
          text="No successful test run on file. You can still publish, but consider running the test first."
        />
      }
    </div>

    <div class="actions">
      <p-button
        label="Publish snapshot"
        icon="pi pi-bookmark"
        severity="primary"
        size="small"
        [loading]="publishing()"
        [disabled]="publishing() || published()"
        (onClick)="publish()"
      />
      @if (published()) {
        <p-message severity="success" text="Snapshot published. Proceed to Activate." />
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        max-width: 640px;
      }
      .hero {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-3);
        margin-bottom: var(--tf-space-5);
      }
      .hero h3 {
        margin: 0;
        font-size: var(--tf-text-heading);
        font-weight: 700;
      }
      .muted {
        margin: 0;
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
      }
      .actions {
        display: flex;
        align-items: center;
        gap: var(--tf-space-3);
        flex-wrap: wrap;
      }
    `,
  ],
})
export class StepPublishComponent {
  protected state = inject(WizardStateService);
  private gen = inject(GeneralService);

  publishing = signal<boolean>(false);
  published = signal<boolean>(false);

  publish() {
    if (this.publishing() || this.published()) return;
    this.publishing.set(true);
    setTimeout(() => {
      this.publishing.set(false);
      this.published.set(true);
      this.gen.success('Snapshot published successfully.');
    }, 800);
  }
}

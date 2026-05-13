import { Component, EventEmitter, Output, inject, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';

import { GeneralService } from '../../services/general.service';
import { WizardStateService } from '../wizard-state.service';

/**
 * Wizard step 10 — Activate the published Deployment.
 *
 * Activating makes this Deployment LIVE for the (Customer, Application, Capability)
 * tuple and DRAINS any prior Active deployment to `Retired` (the one-Active
 * invariant from PRODUCT-GUIDING-PRINCIPLES.md §4). Surfaces a confirmation
 * dialog before doing it.
 *
 * Emits `(finished)` when the user is done so the shell can return them to the
 * Customers list with a success toast.
 */
@Component({
  selector: 'app-step-activate',
  imports: [ButtonModule, MessageModule],
  template: `
    <div class="hero">
      <h3>Activate this deployment</h3>
      <p class="muted">
        Activating makes this mapping live for
        <strong>{{ state.draft().customerId || 'this customer' }}</strong>'s
        {{ state.draft().applicationId || 'integration' }}. Any prior Active deployment
        for the same Customer / Application / Capability will be retired automatically.
      </p>
      <p-message
        severity="info"
        text="This is reversible — you can rollback to the prior Active from the Customer detail screen."
      />
    </div>

    <div class="actions">
      @if (!activated()) {
        <p-button
          label="Activate now"
          icon="pi pi-check"
          severity="success"
          size="small"
          [loading]="activating()"
          [disabled]="activating()"
          (onClick)="activate()"
        />
      } @else {
        <p-message severity="success" text="Deployment is live. Returning to Customers…" />
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
      }
    `,
  ],
})
export class StepActivateComponent {
  protected state = inject(WizardStateService);
  private gen = inject(GeneralService);

  @Output() finished = new EventEmitter<void>();

  activating = signal<boolean>(false);
  activated = signal<boolean>(false);

  activate() {
    if (this.activating() || this.activated()) return;
    this.gen
      .confirm({
        title: 'Activate deployment?',
        text:
          'Any prior Active deployment for the same Customer / Application / Capability ' +
          'will be retired immediately. This affects live traffic.',
        confirmText: 'Yes, activate',
        cancelText: 'Cancel',
        confirmColor: '#28a745',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.activating.set(true);
        setTimeout(() => {
          this.activating.set(false);
          this.activated.set(true);
          this.gen.success('Deployment activated.');
          // Hand control back to the shell after a short delay so user reads the success message.
          setTimeout(() => this.finished.emit(), 1200);
        }, 800);
      });
  }
}

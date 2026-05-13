import { Component, EventEmitter, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { WizardStateService } from '../wizard-state.service';

@Component({
  selector: 'app-step-customer-basics',
  imports: [FormsModule],
  template: `
    <div class="step">
      <p class="muted">Identify the customer being onboarded. You can edit these later from the Customers list.</p>

      <form #f="ngForm" class="grid">
        <label class="field">
          <span>Customer ID <em>*</em></span>
          <input
            type="text"
            name="customerId"
            [ngModel]="model.customerId"
            (ngModelChange)="patch({ customerId: $event })"
            required
            pattern="[a-zA-Z0-9_-]+"
            maxlength="50"
            placeholder="acme-prod"
          />
          <small class="hint">
            Used as the stable identifier. Letters, numbers, dash and underscore. Cannot be changed later.
          </small>
        </label>

        <label class="field">
          <span>Customer name <em>*</em></span>
          <input
            type="text"
            name="customerName"
            [ngModel]="model.customerName"
            (ngModelChange)="patch({ customerName: $event })"
            required
            maxlength="100"
            placeholder="Acme Logistics"
          />
        </label>

        <label class="field">
          <span>Primary contact</span>
          <input
            type="text"
            name="contactName"
            [ngModel]="model.contactName"
            (ngModelChange)="patch({ contactName: $event })"
            maxlength="100"
            placeholder="Jane Doe"
          />
        </label>

        <label class="field">
          <span>Contact email</span>
          <input
            type="email"
            name="contactEmail"
            [ngModel]="model.contactEmail"
            (ngModelChange)="patch({ contactEmail: $event })"
            maxlength="200"
            placeholder="jane@acme.com"
          />
        </label>

        <label class="field full">
          <span>Notes</span>
          <textarea
            name="notes"
            [ngModel]="model.notes"
            (ngModelChange)="patch({ notes: $event })"
            rows="3"
            maxlength="500"
            placeholder="Anything the support team should know about this customer..."
          ></textarea>
        </label>
      </form>
    </div>
  `,
  styles: [
    `
      .step {
        max-width: 640px;
        margin: 0 auto;
      }
      .muted {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin-bottom: var(--tf-space-5);
      }
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
        gap: var(--tf-space-4);
      }
      .field {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-1);
        font-size: var(--tf-text-body);
        color: var(--tf-text-strong);
      }
      .field.full {
        grid-column: 1 / -1;
      }
      .field span {
        font-weight: 600;
      }
      .field em {
        color: var(--tf-required);
        font-style: normal;
        margin-left: 2px;
      }
      input,
      textarea {
        font: inherit;
        font-size: var(--tf-text-body);
        padding: 8px var(--tf-space-3);
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-sm);
        outline: none;
        color: var(--tf-text-strong);
        background: white;
      }
      input::placeholder,
      textarea::placeholder {
        color: var(--tf-text-soft);
      }
      input:focus,
      textarea:focus {
        border-color: var(--tf-blue-500);
        box-shadow: 0 0 0 2px rgba(36, 116, 187, 0.2);
      }
      .hint {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-meta);
        font-weight: 400;
        margin-top: 2px;
      }
    `,
  ],
})
export class StepCustomerBasicsComponent {
  model = this.state.draft();

  constructor(private state: WizardStateService) {}

  patch(update: Partial<typeof this.model>) {
    this.state.patch(update);
    this.model = this.state.draft();
  }
}

import { Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { MessageModule } from 'primeng/message';

import { WizardStateService } from '../wizard-state.service';
import {
  CONNECTION_CREDENTIAL_SCHEMAS,
  CredentialField,
} from '../../constants/connection-credentials.constants';

/**
 * Wizard step 5 — Capture this customer's credentials for the picked Connection.
 *
 * The form is generated from `CONNECTION_CREDENTIAL_SCHEMAS[connectionId]`.
 * Values are persisted to `state.draft().credentials` on every keystroke;
 * the shell's canAdvance() reads `credentialsAreValid()` to gate Next.
 */
@Component({
  selector: 'app-step-add-credentials',
  imports: [FormsModule, InputTextModule, PasswordModule, MessageModule],
  template: `
    @if (!schema().length) {
      <p-message severity="warn" text="Pick a connection in the previous step first." />
    } @else {
      <p class="intro">
        Enter the credentials this customer uses to authenticate against the selected connection.
        Values are stored encrypted and never shown again.
      </p>
      <form class="grid" autocomplete="off">
        @for (field of schema(); track field.key) {
          <label class="field" [class.field--full]="field.type === 'url'">
            <span class="label">
              {{ field.label }}
              @if (field.required) {
                <em>*</em>
              }
            </span>

            @switch (field.type) {
              @case ('password') {
                <p-password
                  [ngModel]="state.draft().credentials[field.key] ?? ''"
                  (ngModelChange)="onChange(field.key, $event)"
                  [name]="field.key"
                  [feedback]="false"
                  [toggleMask]="true"
                  [inputStyle]="{ width: '100%' }"
                  [style]="{ width: '100%' }"
                />
              }
              @case ('number') {
                <input
                  pInputText
                  type="number"
                  [ngModel]="state.draft().credentials[field.key] ?? ''"
                  (ngModelChange)="onChange(field.key, $event)"
                  [name]="field.key"
                  [placeholder]="field.placeholder ?? ''"
                />
              }
              @default {
                <input
                  pInputText
                  [type]="field.type === 'url' ? 'url' : 'text'"
                  [ngModel]="state.draft().credentials[field.key] ?? ''"
                  (ngModelChange)="onChange(field.key, $event)"
                  [name]="field.key"
                  [placeholder]="field.placeholder ?? ''"
                />
              }
            }

            @if (field.hint) {
              <small class="hint">{{ field.hint }}</small>
            }
          </label>
        }
      </form>
    }
  `,
  styles: [
    `
      :host {
        display: block;
        max-width: 720px;
      }
      .intro {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin: 0 0 var(--tf-space-4) 0;
      }
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
        gap: var(--tf-space-4);
      }
      .field {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-1);
        font-size: var(--tf-text-body);
      }
      .field--full {
        grid-column: 1 / -1;
      }
      .label {
        font-weight: 600;
        color: var(--tf-text-strong);
      }
      .label em {
        color: var(--tf-required);
        font-style: normal;
        margin-left: 2px;
      }
      .hint {
        font-size: var(--tf-text-meta);
        color: var(--tf-text-muted);
        margin-top: 2px;
      }
      :host ::ng-deep .p-password,
      :host ::ng-deep .p-password input {
        width: 100%;
      }
    `,
  ],
})
export class StepAddCredentialsComponent {
  protected state = inject(WizardStateService);

  schema = computed<CredentialField[]>(() => {
    const cid = this.state.draft().connectionId;
    return CONNECTION_CREDENTIAL_SCHEMAS[cid] ?? [];
  });

  onChange(key: string, value: string) {
    const current = this.state.draft().credentials;
    this.state.patch({ credentials: { ...current, [key]: value ?? '' } });
  }
}

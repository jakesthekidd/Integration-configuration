import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';

import { ApiService } from '../services/api.service';
import { GeneralService } from '../services/general.service';
import { Deployment } from '../models/deployment.model';
import { TmsSystem } from '../models/tms-system.model';
import {
  CONNECTION_CREDENTIAL_SCHEMAS,
  CredentialField,
  credentialsAreValid,
} from '../constants/connection-credentials.constants';

/**
 * Connection tab — which Connection this deployment uses + the customer's credentials.
 *
 * Per PRODUCT-GUIDING-PRINCIPLES.md §6 this tab is independently savable;
 * partial credentials are allowed (the Test & Publish tab gates Activate
 * on completeness, not Save).
 *
 * Today the mock doesn't persist credentials anywhere visible, so we keep
 * them in component state and emit `(saved)` to signal the parent.
 */
@Component({
  selector: 'app-connection-tab',
  imports: [
    CommonModule,
    FormsModule,
    SelectModule,
    InputTextModule,
    PasswordModule,
    ButtonModule,
    MessageModule,
  ],
  template: `
    <section class="block">
      <header class="block__head">
        <div>
          <h4>Connection</h4>
          <p class="muted">Which system does this customer connect through for this capability?</p>
        </div>
      </header>

      <label class="field field--narrow">
        <span class="field__label">Connection</span>
        <p-select
          [options]="connectionOptions()"
          [ngModel]="connectionId()"
          (ngModelChange)="onConnectionChange($event)"
          placeholder="Pick a connection"
          appendTo="body"
        />
        <small class="hint" *ngIf="connectionId()">
          {{ connectionDescription() }}
        </small>
      </label>

    </section>

    <section class="block">
      <header class="block__head">
        <div>
          <h4>Credentials</h4>
          <p class="muted">
            Per-customer auth for the selected connection. Stored encrypted; values are masked
            after save.
          </p>
        </div>
      </header>

      @if (schema().length === 0) {
        <p-message
          severity="info"
          text="Pick a connection above to reveal the credential form."
        />
      } @else {
        <form class="grid" autocomplete="off">
          @for (field of schema(); track field.key) {
            <label class="field" [class.field--full]="field.type === 'url'">
              <span class="field__label">
                {{ field.label }}
                @if (field.required) {
                  <em>*</em>
                }
              </span>

              @switch (field.type) {
                @case ('password') {
                  <p-password
                    [ngModel]="credentials()[field.key] ?? ''"
                    (ngModelChange)="onCredChange(field.key, $event)"
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
                    [ngModel]="credentials()[field.key] ?? ''"
                    (ngModelChange)="onCredChange(field.key, $event)"
                    [name]="field.key"
                    [placeholder]="field.placeholder ?? ''"
                  />
                }
                @default {
                  <input
                    pInputText
                    [type]="field.type === 'url' ? 'url' : 'text'"
                    [ngModel]="credentials()[field.key] ?? ''"
                    (ngModelChange)="onCredChange(field.key, $event)"
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
    </section>

    <footer class="actions">
      @if (!isComplete()) {
        <span class="status-pill status-pill--draft">
          <i class="pi pi-exclamation-circle"></i>
          Incomplete — missing required fields
        </span>
      } @else {
        <span class="status-pill status-pill--ok">
          <i class="pi pi-check-circle"></i>
          All required fields filled
        </span>
      }
      <span class="grow"></span>
      <p-button
        label="Save changes"
        icon="pi pi-save"
        severity="primary"
        size="small"
        [disabled]="!dirty()"
        [loading]="saving()"
        (onClick)="save()"
      />
    </footer>
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-5);
      }

      .block {
        background: white;
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-md);
        padding: var(--tf-space-4) var(--tf-space-5);
      }
      .block__head {
        margin-bottom: var(--tf-space-3);
      }
      .block__head h4 {
        margin: 0;
        font-size: var(--tf-text-heading);
      }
      .muted {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin: 4px 0 0 0;
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
      .field--narrow {
        max-width: 420px;
      }
      .field--full {
        grid-column: 1 / -1;
      }
      .field__label {
        font-weight: 600;
        color: var(--tf-text-strong);
      }
      .field__label em {
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
      :host ::ng-deep .p-password input,
      :host ::ng-deep .p-select {
        width: 100%;
      }

      .actions {
        display: flex;
        align-items: center;
        gap: var(--tf-space-3);
        padding: var(--tf-space-3) var(--tf-space-4);
        background: var(--tf-slate-100);
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-md);
      }
      .grow {
        flex: 1 1 auto;
      }
      .status-pill {
        display: inline-flex;
        align-items: center;
        gap: var(--tf-space-1);
        font-size: var(--tf-text-meta);
        font-weight: 600;
        padding: 4px 10px;
        border-radius: var(--tf-radius-pill);
      }
      .status-pill--ok {
        background: #e5f9ea;
        color: #1b6b3a;
      }
      .status-pill--draft {
        background: #fff6e5;
        color: #92510a;
      }
    `,
  ],
})
export class ConnectionTabComponent implements OnChanges {
  @Input({ required: true }) deployment!: Deployment;
  @Output() saved = new EventEmitter<void>();

  private api = inject(ApiService);
  private gen = inject(GeneralService);

  connections = signal<TmsSystem[]>([]);
  connectionId = signal<string>('');
  credentials = signal<Record<string, string>>({});

  /** Original snapshot to compare for dirty state. */
  private snapshot = signal<{ connectionId: string; credentials: Record<string, string> }>({
    connectionId: '',
    credentials: {},
  });

  saving = signal<boolean>(false);

  schema = computed<CredentialField[]>(() => {
    const cid = this.connectionId();
    return CONNECTION_CREDENTIAL_SCHEMAS[cid] ?? [];
  });

  connectionOptions = computed(() =>
    this.connections()
      .filter((c) => c.isActive)
      .map((c) => ({ label: `${c.displayName} (v${c.version})`, value: c.id })),
  );

connectionDescription = computed(() => {
    const c = this.connections().find((x) => x.id === this.connectionId());
    return c?.description ?? '';
  });

  isComplete = computed(() => credentialsAreValid(this.connectionId(), this.credentials()));

  dirty = computed(() => {
    const snap = this.snapshot();
    if (snap.connectionId !== this.connectionId()) return true;
    const a = snap.credentials;
    const b = this.credentials();
    const keys = new Set([...Object.keys(a), ...Object.keys(b)]);
    for (const k of keys) {
      if ((a[k] ?? '') !== (b[k] ?? '')) return true;
    }
    return false;
  });

  ngOnChanges(changes: SimpleChanges) {
    if (changes['deployment']) {
      this.connectionId.set(this.deployment.connectionId);
      // Credentials aren't on the Deployment object yet; would come from a CustomerConnection.
      // For now reset to empty and let the user fill them.
      this.credentials.set({});
      this.snapshot.set({ connectionId: this.deployment.connectionId, credentials: {} });
      this.loadConnections();
    }
  }

  private loadConnections() {
    this.api.getTmsSystems(true).subscribe((res) => {
      if (res.success && res.data) this.connections.set(res.data.systems);
    });
  }

onConnectionChange(id: string) {
    this.connectionId.set(id);
    // Switching connections wipes the credentials since the schema changes.
    this.credentials.set({});
  }

  onCredChange(key: string, value: string) {
    this.credentials.update((c) => ({ ...c, [key]: value ?? '' }));
  }

  save() {
    if (!this.dirty() || this.saving()) return;
    this.saving.set(true);
    // Mock: pretend we POSTed; in reality this would persist CustomerConnection.
    setTimeout(() => {
      this.snapshot.set({
        connectionId: this.connectionId(),
        credentials: { ...this.credentials() },
      });
      this.saving.set(false);
      this.gen.success('Connection saved.');
      this.saved.emit();
    }, 400);
  }
}

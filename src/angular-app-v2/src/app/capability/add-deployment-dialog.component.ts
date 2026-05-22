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
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { MessageModule } from 'primeng/message';

import { ApiService } from '../services/api.service';
import { GeneralService } from '../services/general.service';
import { Application } from '../models/application.model';
import { Capability } from '../models/capability.model';
import { Deployment } from '../models/deployment.model';

/**
 * Add Deployment dialog — used for both:
 *   - `+ Add application` (rail root): two-step flow, pick app → pick capability
 *   - `+ Add capability` (under an existing app): one-step, pick capability
 *
 * Either way, the output is one new `Draft` Deployment with empty Connection /
 * Template fields — the user fills those in via the four tabs.
 *
 * Filters apps/capabilities to those NOT yet deployed for this customer (each
 * (customer, app, capability) tuple can only have ONE non-Retired Deployment).
 */
@Component({
  selector: 'app-add-deployment-dialog',
  imports: [CommonModule, DialogModule, ButtonModule, TagModule, MessageModule],
  template: `
    <p-dialog
      [(visible)]="visibleProxy"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [closable]="true"
      [draggable]="false"
      [resizable]="false"
      [style]="{ width: '560px' }"
      [header]="header()"
    >
      <p class="muted" *ngIf="step() === 'app'">
        Which application do you want to set up for this customer?
      </p>
      <p class="muted" *ngIf="step() === 'capability' && pickedApp(); let app">
        Which capability of <strong>{{ app.displayName }}</strong> are you setting up?
      </p>

      <!-- ── App picker ────────────────────────────────────────── -->
      @if (step() === 'app') {
        @if (availableApps().length === 0) {
          <p-message
            severity="info"
            text="Every application is already deployed for this customer. Add a new capability under an existing application instead."
          />
        } @else {
          <ul class="picker">
            @for (app of availableApps(); track app.id) {
              <li>
                <button
                  type="button"
                  class="picker-row"
                  [class.picker-row--selected]="selectedAppId() === app.id"
                  (click)="selectedAppId.set(app.id)"
                >
                  <span class="picker-row__title">{{ app.displayName }}</span>
                  <span class="picker-row__desc">{{ app.description ?? '' }}</span>
                </button>
              </li>
            }
          </ul>
        }
      }

      <!-- ── Capability picker ─────────────────────────────────── -->
      @if (step() === 'capability') {
        @if (availableCapabilities().length === 0) {
          <p-message
            severity="info"
            text="Every capability of this application is already deployed for this customer."
          />
        } @else {
          <ul class="picker">
            @for (cap of availableCapabilities(); track cap.id) {
              <li>
                <button
                  type="button"
                  class="picker-row"
                  [class.picker-row--selected]="selectedCapabilityId() === cap.id"
                  (click)="selectedCapabilityId.set(cap.id)"
                >
                  <span class="picker-row__head">
                    <span class="picker-row__title">{{ cap.displayName }}</span>
                    <p-tag
                      [value]="cap.direction"
                      [severity]="dirSeverity(cap.direction)"
                      [rounded]="true"
                    />
                  </span>
                  <span class="picker-row__desc">{{ cap.description ?? '' }}</span>
                </button>
              </li>
            }
          </ul>
        }
      }

      <ng-template pTemplate="footer">
        <div class="footer">
          <p-button
            label="Cancel"
            severity="secondary"
            [text]="true"
            size="small"
            (onClick)="close()"
          />
          <span class="grow"></span>

          @if (step() === 'app') {
            <p-button
              label="Continue"
              icon="pi pi-arrow-right"
              iconPos="right"
              severity="primary"
              size="small"
              [disabled]="!selectedAppId()"
              (onClick)="step.set('capability')"
            />
          } @else {
            @if (!preSelectedAppId) {
              <p-button
                label="Back"
                icon="pi pi-arrow-left"
                severity="secondary"
                [outlined]="true"
                size="small"
                (onClick)="step.set('app')"
              />
            }
            <p-button
              label="Create"
              icon="pi pi-check"
              severity="primary"
              size="small"
              [disabled]="!canCreate()"
              [loading]="creating()"
              (onClick)="create()"
            />
          }
        </div>
      </ng-template>
    </p-dialog>
  `,
  styles: [
    `
      .muted {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin: 0 0 var(--tf-space-3) 0;
      }
      .picker {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-2);
      }
      .picker-row {
        width: 100%;
        background: white;
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-md);
        padding: var(--tf-space-3);
        cursor: pointer;
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-1);
        font-family: inherit;
        text-align: left;
      }
      .picker-row:hover {
        border-color: var(--tf-blue-400);
        background: var(--tf-blue-50);
      }
      .picker-row--selected,
      .picker-row--selected:hover {
        border-color: var(--tf-blue-500);
        background: var(--tf-blue-100);
      }
      .picker-row__head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--tf-space-3);
      }
      .picker-row__title {
        font-weight: 700;
        font-size: var(--tf-text-body);
        color: var(--tf-text-strong);
      }
      .picker-row__desc {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-meta);
      }
      .footer {
        display: flex;
        align-items: center;
        gap: var(--tf-space-2);
        width: 100%;
      }
      .grow {
        flex: 1 1 auto;
      }
    `,
  ],
})
export class AddDeploymentDialogComponent implements OnChanges {
  @Input() visible = false;
  @Input({ required: true }) customerId = '';
  @Input({ required: true }) applications: Application[] = [];
  @Input({ required: true }) capabilities: Capability[] = [];
  @Input({ required: true }) existingDeployments: Deployment[] = [];
  /** When set, the dialog skips straight to the capability picker for this app. */
  @Input() preSelectedAppId: string | null = null;

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() created = new EventEmitter<Deployment>();

  private api = inject(ApiService);
  private gen = inject(GeneralService);

  /** Local two-way mirror for [(visible)] on the underlying p-dialog. */
  visibleProxy = false;

  step = signal<'app' | 'capability'>('app');
  selectedAppId = signal<string>('');
  selectedCapabilityId = signal<string>('');
  creating = signal<boolean>(false);

  // Signal mirrors of the @Inputs so the computeds below stay reactive.
  // `computed()` only tracks signal reads — plain @Input properties don't
  // trigger re-runs, which is what bit us before (cached empty filter result).
  private applicationsSig = signal<Application[]>([]);
  private capabilitiesSig = signal<Capability[]>([]);
  private existingDeploymentsSig = signal<Deployment[]>([]);

  pickedApp = computed<Application | null>(() => {
    const id = this.selectedAppId();
    return this.applicationsSig().find((a) => a.id === id) ?? null;
  });

  header = computed(() => (this.step() === 'app' ? 'Add application' : 'Add capability'));

  /** Apps the customer doesn't yet have ANY non-Retired deployment for. */
  availableApps = computed<Application[]>(() => {
    const liveAppIds = new Set(
      this.existingDeploymentsSig().filter((d) => d.status !== 'Retired').map((d) => d.applicationId),
    );
    return this.applicationsSig().filter((a) => a.isActive && !liveAppIds.has(a.id));
  });

  /** Capabilities of `selectedAppId` not yet deployed for this customer. */
  availableCapabilities = computed<Capability[]>(() => {
    const appId = this.selectedAppId();
    if (!appId) return [];
    const liveCapIds = new Set(
      this.existingDeploymentsSig()
        .filter((d) => d.applicationId === appId && d.status !== 'Retired')
        .map((d) => d.capabilityId),
    );
    return this.capabilitiesSig().filter(
      (c) => c.applicationId === appId && c.isActive && !liveCapIds.has(c.id),
    );
  });

  canCreate = computed<boolean>(() => !!this.selectedAppId() && !!this.selectedCapabilityId());

  ngOnChanges(changes: SimpleChanges) {
    if (changes['applications']) this.applicationsSig.set(this.applications);
    if (changes['capabilities']) this.capabilitiesSig.set(this.capabilities);
    if (changes['existingDeployments']) this.existingDeploymentsSig.set(this.existingDeployments);
    if (changes['visible']) {
      this.visibleProxy = this.visible;
      if (this.visible) this.reset();
    }
  }

  private reset() {
    this.selectedAppId.set(this.preSelectedAppId ?? '');
    this.selectedCapabilityId.set('');
    this.step.set(this.preSelectedAppId ? 'capability' : 'app');
    this.creating.set(false);
  }

  onVisibleChange(v: boolean) {
    this.visibleProxy = v;
    this.visibleChange.emit(v);
  }

  close() {
    this.visibleProxy = false;
    this.visibleChange.emit(false);
  }

  create() {
    if (!this.canCreate() || this.creating()) return;
    this.creating.set(true);
    this.api
      .createDeployment({
        customerId: this.customerId,
        applicationId: this.selectedAppId(),
        capabilityId: this.selectedCapabilityId(),
        connectionId: '',
        forkedFromTemplateId: '',
        forkedFromTemplateVersion: null,
        status: 'Draft',
        snapshotVersion: 0,
      })
      .subscribe({
        next: (res) => {
          this.creating.set(false);
          if (res.success && res.data) {
            this.gen.success('Deployment created. Fill in the Connection tab to continue.');
            this.created.emit(res.data);
            this.visibleProxy = false;
            this.visibleChange.emit(false);
          } else {
            this.gen.error(res.message ?? 'Failed to create deployment.');
          }
        },
        error: (err) => {
          this.creating.set(false);
          this.gen.error('Failed to create deployment: ' + (err.message ?? err));
        },
      });
  }

  dirSeverity(dir: string): 'info' | 'warn' | 'contrast' {
    if (dir === 'Inbound') return 'info';
    if (dir === 'Outbound') return 'warn';
    return 'contrast';
  }
}

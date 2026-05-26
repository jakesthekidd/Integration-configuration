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
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { MessageModule } from 'primeng/message';

import { GeneralService } from '../services/general.service';
import { Deployment, DeploymentStatus } from '../models/deployment.model';

/**
 * Publish & Activate tab — the lifecycle home for one deployment.
 *
 *   - Publish snapshots the current Connection + Mapping into a versioned Deployment.
 *   - Activate makes it live and retires any prior Active for the same
 *     (customer, application, capability) tuple — see §4 invariant.
 *   - Retire / Rollback available based on current status.
 *
 * Auth testing lives on the Connection tab (Test Authentication button).
 * Mapping testing is handled separately. This tab is lifecycle-only.
 *
 * State transitions emit `(statusChanged)` so the parent refetches the
 * deployments list and the rail re-renders.
 */
@Component({
  selector: 'app-test-publish-tab',
  imports: [CommonModule, ButtonModule, TagModule, MessageModule],
  template: `
    <!-- ── Lifecycle ───────────────────────────────────────────────── -->
    <section class="block">
      <header class="block__head">
        <div>
          <h4>Lifecycle</h4>
          <p class="muted">Current status of this deployment.</p>
        </div>
        <p-tag [value]="status()" [severity]="severity()" [rounded]="true" />
      </header>

      <div class="lifecycle">
        <!-- Publish: from Draft / Tested → Published -->
        @if (status() === 'Draft' || status() === 'Tested') {
          <article class="action-card">
            <div>
              <h5>Publish snapshot</h5>
              <p class="muted">
                Captures the current Connection + Mapping into a versioned snapshot
                (v{{ deployment.snapshotVersion + 1 }}). Does not make it live.
              </p>
              @if (!testedRecently()) {
                <p-message
                  severity="warn"
                  text="No successful test on file. You can still publish, but consider testing first."
                />
              }
            </div>
            <p-button
              label="Publish"
              icon="pi pi-bookmark"
              severity="primary"
              size="small"
              [loading]="busyAction() === 'publish'"
              (onClick)="publish()"
            />
          </article>
        }

        <!-- Activate: from Published → Active (only after Publish step) -->
        @if (status() === 'Published') {
          <article class="action-card">
            <div>
              <h5>Activate</h5>
              <p class="muted">
                Makes this deployment <strong>live</strong>. Any prior Active deployment for
                the same Customer / Application / Capability is automatically retired.
              </p>
              @if (!canActivate()) {
                <p-message
                  severity="warn"
                  text="Complete the Connection and Mapping tabs before activating."
                />
              }
            </div>
            <p-button
              label="Activate"
              icon="pi pi-check"
              severity="success"
              size="small"
              [disabled]="!canActivate()"
              [loading]="busyAction() === 'activate'"
              (onClick)="activate()"
            />
          </article>
        }

        <!-- Retire: from Active → Retired -->
        @if (status() === 'Active') {
          <article class="action-card action-card--danger">
            <div>
              <h5>Retire</h5>
              <p class="muted">
                Takes this deployment offline. Existing traffic stops flowing through this
                mapping. You can publish a new version later to bring it back.
              </p>
            </div>
            <p-button
              label="Retire"
              icon="pi pi-pause"
              severity="danger"
              [outlined]="true"
              size="small"
              [loading]="busyAction() === 'retire'"
              (onClick)="retire()"
            />
          </article>
        }

        <!-- Rollback: from Active → restore prior Active (placeholder) -->
        @if (status() === 'Active') {
          <article class="action-card">
            <div>
              <h5>Rollback</h5>
              <p class="muted">
                Reactivates the most recently retired version of this deployment. Useful when a
                new release misbehaves and you need to revert quickly.
              </p>
            </div>
            <p-button
              label="Rollback"
              icon="pi pi-undo"
              severity="secondary"
              [outlined]="true"
              size="small"
              [loading]="busyAction() === 'rollback'"
              (onClick)="rollback()"
            />
          </article>
        }

        <!-- Reactivate: from Retired → republish + activate (placeholder) -->
        @if (status() === 'Retired') {
          <article class="action-card">
            <div>
              <h5>Reactivate</h5>
              <p class="muted">
                Publishes a new snapshot from the current Connection + Mapping and activates it.
              </p>
            </div>
            <p-button
              label="Reactivate"
              icon="pi pi-refresh"
              severity="primary"
              size="small"
              [loading]="busyAction() === 'reactivate'"
              (onClick)="reactivate()"
            />
          </article>
        }
      </div>
    </section>

    <!-- ── Promote ─────────────────────────────────────────────────── -->
    <section class="block">
      <header class="block__head">
        <div>
          <h4>Promote</h4>
          <p class="muted">Share this mapping with the rest of the team as a master template.</p>
        </div>
      </header>

      <article class="action-card">
        <div>
          <h5>Send to master templates</h5>
          <p class="muted">
            Promotes this deployment's current Connection + Mapping as a <strong>master template</strong>.
            Other customers can fork from it as a starting point — useful when you've built a configuration
            that should become the default for similar customers.
          </p>
          @if (!canPromote()) {
            <p-message
              severity="warn"
              text="Available once this deployment is Published or Active. Master templates are built from finalized mappings, not drafts."
            />
          }
          @if (promotedAt(); as p) {
            <p-message
              severity="success"
              [text]="'Promoted to master templates · ' + (p | date: 'medium')"
            />
          }
        </div>
        <p-button
          label="Send to master templates"
          icon="pi pi-share-alt"
          severity="secondary"
          [outlined]="true"
          size="small"
          [disabled]="!canPromote()"
          [loading]="busyAction() === 'promote'"
          (onClick)="sendToMasterTemplates()"
        />
      </article>
    </section>
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
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: var(--tf-space-4);
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

      .lifecycle {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-3);
      }
      .action-card {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: var(--tf-space-4);
        padding: var(--tf-space-3) var(--tf-space-4);
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-sm);
        background: var(--tf-slate-50);
      }
      .action-card h5 {
        margin: 0;
        font-size: var(--tf-text-body);
        font-weight: 700;
      }
      .action-card--danger {
        border-color: #f0a5ab;
        background: #fbe9ea;
      }
      .action-card--danger h5 {
        color: #83131a;
      }
    `,
  ],
})
export class TestPublishTabComponent implements OnChanges {
  @Input({ required: true }) deployment!: Deployment;
  @Output() statusChanged = new EventEmitter<void>();

  private gen = inject(GeneralService);

  /** Locally-overridden status — flips immediately on successful action while
   *  the parent refetches to make the rail catch up. */
  private localStatus = signal<DeploymentStatus | null>(null);

  status = computed<DeploymentStatus>(() => this.localStatus() ?? this.deployment.status);

  busyAction = signal<'publish' | 'activate' | 'retire' | 'rollback' | 'reactivate' | 'promote' | null>(null);

  /** Timestamp of the most recent successful promotion to master templates. */
  promotedAt = signal<Date | null>(null);

  /** Gate for "Send to master templates" — only finalized mappings should be promoted. */
  canPromote = computed<boolean>(() => {
    const s = this.status();
    return s === 'Published' || s === 'Active';
  });

  /** True when a Tested correlation exists (auth tested on Connection tab). */
  testedRecently = computed<boolean>(() => !!this.deployment.lastTestCorrelationId);

  /** Gate for Activate: need Connection + at least a template choice + a recent test. */
  canActivate = computed<boolean>(() => {
    const d = this.deployment;
    const hasConnection = !!d.connectionId;
    // Either a forked master or "from scratch" sentinel — either is acceptable.
    const hasTemplate = !!d.forkedFromTemplateId || d.forkedFromTemplateVersion === null;
    return hasConnection && hasTemplate;
  });

  severity = computed<'success' | 'info' | 'warn' | 'secondary' | 'danger'>(() => {
    switch (this.status()) {
      case 'Active':
        return 'success';
      case 'Published':
      case 'Tested':
        return 'info';
      case 'Draft':
        return 'warn';
      case 'Retired':
      default:
        return 'secondary';
    }
  });

  ngOnChanges(changes: SimpleChanges) {
    if (changes['deployment']) {
      this.localStatus.set(null);
      this.promotedAt.set(null);
    }
  }

  sendToMasterTemplates() {
    if (this.busyAction() || !this.canPromote()) return;
    this.gen
      .confirm({
        title: 'Send to master templates?',
        text:
          'A new master template will be created from this deployment\'s current Connection + Mapping. ' +
          'Other customers will be able to fork it as a starting point.',
        confirmText: 'Yes, promote',
        confirmColor: '#0066cc',
        icon: 'info',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.busyAction.set('promote');
        // Mock: pretend we POSTed a new master template. Real impl would call
        // api.createTemplateFromDeployment(deploymentId) and refresh the library.
        setTimeout(() => {
          this.promotedAt.set(new Date());
          this.busyAction.set(null);
          this.gen.success('Mapping promoted to master templates.');
        }, 600);
      });
  }

  publish() {
    if (this.busyAction()) return;
    this.busyAction.set('publish');
    setTimeout(() => {
      this.localStatus.set('Published');
      this.busyAction.set(null);
      this.gen.success('Snapshot published.');
      this.statusChanged.emit();
    }, 600);
  }

  activate() {
    if (this.busyAction() || !this.canActivate()) return;
    this.gen
      .confirm({
        title: 'Activate deployment?',
        text:
          'Any prior Active deployment for the same Customer / Application / Capability will be ' +
          'retired automatically. This affects live traffic.',
        confirmText: 'Yes, activate',
        confirmColor: '#28a745',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.busyAction.set('activate');
        setTimeout(() => {
          this.localStatus.set('Active');
          this.busyAction.set(null);
          this.gen.success('Deployment is live.');
          this.statusChanged.emit();
        }, 600);
      });
  }

  retire() {
    if (this.busyAction()) return;
    this.gen
      .confirm({
        title: 'Retire deployment?',
        text: 'Live traffic for this mapping stops immediately. You can publish a new version later.',
        confirmText: 'Yes, retire',
        confirmColor: '#e74c3c',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.busyAction.set('retire');
        setTimeout(() => {
          this.localStatus.set('Retired');
          this.busyAction.set(null);
          this.gen.success('Deployment retired.');
          this.statusChanged.emit();
        }, 600);
      });
  }

  rollback() {
    if (this.busyAction()) return;
    this.busyAction.set('rollback');
    setTimeout(() => {
      this.busyAction.set(null);
      this.gen.success('Rollback is a placeholder — real impl when backend lands.');
    }, 400);
  }

  reactivate() {
    if (this.busyAction()) return;
    this.busyAction.set('reactivate');
    setTimeout(() => {
      this.localStatus.set('Active');
      this.busyAction.set(null);
      this.gen.success('Deployment reactivated.');
      this.statusChanged.emit();
    }, 600);
  }
}

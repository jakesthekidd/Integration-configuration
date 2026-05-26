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
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';

import { GeneralService } from '../services/general.service';
import { Deployment, DeploymentStatus } from '../models/deployment.model';

type StepKey = 'draft' | 'published' | 'active';
type StepState = 'done' | 'current' | 'pending';

interface NextAction {
  label: string;
  icon: string;
  busyKey: 'publish' | 'activate' | 'reactivate';
  disabled: boolean;
  handler: () => void;
}

/**
 * Status tab — the lifecycle home for one deployment.
 *
 * Replaces the prior stacked "Lifecycle" + "Promote" cards with a single
 * status banner (state + version + stepper + one primary CTA) and a
 * Manage overflow menu for the secondary actions (Retire / Rollback /
 * Send to master templates). One forward step is always the obvious
 * thing to click — destructive / niche actions live behind ⋯.
 *
 *   Draft / Tested → Published → Active     (Retire → Retired → Reactivate)
 *
 * State transitions emit `(statusChanged)` so the parent refetches the
 * deployments list and the rail re-renders.
 */
@Component({
  selector: 'app-test-publish-tab',
  imports: [CommonModule, ButtonModule, TagModule, MessageModule, MenuModule],
  template: `
    <!-- ── Status banner ───────────────────────────────────────────── -->
    <section class="banner" [attr.data-status]="status()">
      <div class="banner__top">
        <div class="banner__title">
          <span class="status-dot" [attr.data-status]="status()" aria-hidden="true"></span>
          <strong>{{ statusLabel() }}</strong>
          @if (deployment.snapshotVersion > 0) {
            <span class="banner__meta">· v{{ deployment.snapshotVersion }}</span>
          }
          @if (publishedRelative(); as p) {
            <span class="banner__meta">· {{ p }}</span>
          }
        </div>

        <div class="banner__actions">
          @if (canPromote()) {
            <p-button
              label="Send to master templates"
              icon="pi pi-share-alt"
              severity="secondary"
              [outlined]="true"
              size="small"
              [loading]="busyAction() === 'promote'"
              (onClick)="sendToMasterTemplates()"
            />
          }
          @if (nextAction(); as next) {
            <p-button
              [label]="next.label"
              [icon]="next.icon"
              iconPos="right"
              severity="primary"
              size="small"
              [disabled]="next.disabled"
              [loading]="busyAction() === next.busyKey"
              (onClick)="next.handler()"
            />
          }
          @if (hasManageActions()) {
            <p-button
              label="Manage"
              icon="pi pi-chevron-down"
              iconPos="right"
              severity="secondary"
              [outlined]="true"
              size="small"
              (onClick)="manageMenu.toggle($event)"
            />
            <p-menu #manageMenu [model]="manageMenuItems()" [popup]="true" appendTo="body" />
          }
        </div>
      </div>

      <!-- Stepper -->
      <ol class="stepper" [class.stepper--retired]="status() === 'Retired'">
        <li class="step" [attr.data-state]="stepState('draft')">
          <span class="step__bullet">
            @if (stepState('draft') === 'done') {
              <i class="pi pi-check"></i>
            }
          </span>
          <span class="step__label">Draft</span>
        </li>
        <li class="step__connector" [attr.data-state]="stepState('published')"></li>
        <li class="step" [attr.data-state]="stepState('published')">
          <span class="step__bullet">
            @if (stepState('published') === 'done') {
              <i class="pi pi-check"></i>
            }
          </span>
          <span class="step__label">Published</span>
        </li>
        <li class="step__connector" [attr.data-state]="stepState('active')"></li>
        <li class="step" [attr.data-state]="stepState('active')">
          <span class="step__bullet">
            @if (stepState('active') === 'done' && status() !== 'Retired') {
              <i class="pi pi-check"></i>
            }
          </span>
          <span class="step__label">{{ status() === 'Retired' ? 'Retired' : 'Active' }}</span>
        </li>
      </ol>

      <p class="banner__desc">{{ statusDescription() }}</p>

      <!-- Inline callouts -->
      @if (status() === 'Published' && !canActivate()) {
        <p-message
          severity="warn"
          text="Complete the Connection and Mapping tabs before activating."
        />
      }
      @if (
        (status() === 'Draft' || status() === 'Tested') &&
        !testedRecently()
      ) {
        <p-message
          severity="info"
          text="No successful test on file yet. You can still publish, but consider running a test first."
        />
      }
      @if (promotedAt(); as p) {
        <p-message
          severity="success"
          [text]="'Promoted to master templates · ' + (p | date: 'medium')"
        />
      }
    </section>
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-5);
      }

      /* ── Banner ─────────────────────────────────────────────────── */
      .banner {
        background: white;
        border: 1px solid var(--tf-slate-300);
        border-radius: var(--tf-radius-md);
        padding: var(--tf-space-5);
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-4);
      }
      .banner[data-status='Active'] {
        border-color: #a3d9b1;
        background: linear-gradient(135deg, #f0fdf4 0%, white 70%);
      }
      .banner[data-status='Published'] {
        border-color: #9ec5ec;
        background: linear-gradient(135deg, #f0f7ff 0%, white 70%);
      }
      .banner[data-status='Draft'],
      .banner[data-status='Tested'] {
        border-color: #f5cf94;
        background: linear-gradient(135deg, #fff8eb 0%, white 70%);
      }
      .banner[data-status='Retired'] {
        border-color: var(--tf-slate-400);
        background: var(--tf-slate-50);
      }

      .banner__top {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--tf-space-3);
      }
      .banner__title {
        display: flex;
        align-items: baseline;
        gap: 8px;
        font-size: var(--tf-text-heading);
        line-height: 1.2;
      }
      .banner__title strong {
        font-weight: 700;
        color: var(--tf-text-strong);
      }
      .banner__meta {
        font-weight: 400;
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
      }
      .banner__actions {
        display: flex;
        align-items: center;
        gap: var(--tf-space-2);
        flex-shrink: 0;
      }
      .banner__desc {
        margin: 0;
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        max-width: 70ch;
      }

      .status-dot {
        width: 10px;
        height: 10px;
        border-radius: 50%;
        background: var(--tf-slate-500);
        flex-shrink: 0;
        align-self: center;
      }
      .status-dot[data-status='Active'] {
        background: #1b6b3a;
      }
      .status-dot[data-status='Published'],
      .status-dot[data-status='Tested'] {
        background: #1d6fc0;
      }
      .status-dot[data-status='Draft'] {
        background: #d97706;
      }
      .status-dot[data-status='Retired'] {
        background: var(--tf-slate-500);
      }

      /* ── Stepper ────────────────────────────────────────────────── */
      .stepper {
        display: flex;
        align-items: center;
        gap: 0;
        padding: 0;
        margin: 0;
        list-style: none;
      }
      .step {
        display: flex;
        align-items: center;
        gap: 8px;
        font-size: var(--tf-text-meta);
        font-weight: 600;
        color: var(--tf-text-muted);
      }
      .step__bullet {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 22px;
        height: 22px;
        border-radius: 50%;
        background: var(--tf-slate-200);
        color: white;
        font-size: 10px;
        flex-shrink: 0;
      }
      .step[data-state='done'] .step__bullet {
        background: #1d6fc0;
      }
      .step[data-state='current'] .step__bullet {
        background: white;
        border: 2px solid #1d6fc0;
        box-shadow: 0 0 0 4px rgba(29, 111, 192, 0.18);
      }
      .step[data-state='done'] .step__label,
      .step[data-state='current'] .step__label {
        color: var(--tf-text-strong);
      }
      .step__connector {
        flex: 0 0 40px;
        height: 2px;
        margin: 0 8px;
        background: var(--tf-slate-200);
      }
      .step__connector[data-state='done'] {
        background: #1d6fc0;
      }

      .stepper--retired .step__bullet,
      .stepper--retired .step__connector {
        background: var(--tf-slate-400);
        border-color: var(--tf-slate-400);
        box-shadow: none;
      }
      .stepper--retired .step__label {
        color: var(--tf-text-muted);
      }
      .stepper--retired .step:last-child .step__label {
        color: #83131a;
      }

      /* PrimeNG menu danger item styling */
      :host ::ng-deep .p-menu .menu-item--danger .p-menuitem-link {
        color: #83131a;
      }
      :host ::ng-deep .p-menu .menu-item--danger .p-menuitem-icon {
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

  busyAction = signal<
    'publish' | 'activate' | 'retire' | 'rollback' | 'reactivate' | 'promote' | null
  >(null);

  /** Timestamp of the most recent successful promotion to master templates. */
  promotedAt = signal<Date | null>(null);

  /** Gate for "Send to master templates" — only finalized mappings should be promoted. */
  canPromote = computed<boolean>(() => {
    const s = this.status();
    return s === 'Published' || s === 'Active';
  });

  /** True when a Tested correlation exists (auth tested on Connection tab). */
  testedRecently = computed<boolean>(() => !!this.deployment.lastTestCorrelationId);

  /** Gate for Activate: need Connection + at least a template choice. */
  canActivate = computed<boolean>(() => {
    const d = this.deployment;
    const hasConnection = !!d.connectionId;
    const hasTemplate = !!d.forkedFromTemplateId || d.forkedFromTemplateVersion === null;
    return hasConnection && hasTemplate;
  });

  statusLabel = computed<string>(() => {
    switch (this.status()) {
      case 'Tested':
        return 'Draft · tested';
      default:
        return this.status();
    }
  });

  statusDescription = computed<string>(() => {
    switch (this.status()) {
      case 'Draft':
        return `No snapshot yet. Publish to capture the current Connection + Mapping as v${this.deployment.snapshotVersion + 1}.`;
      case 'Tested':
        return `Tested but not yet published. Publish to capture as v${this.deployment.snapshotVersion + 1}.`;
      case 'Published':
        return 'Snapshot is ready. Activate to make it live for this customer.';
      case 'Active':
        return 'This deployment is live and processing customer traffic.';
      case 'Retired':
        return 'This deployment is offline. Reactivate to publish a new snapshot and bring it back.';
    }
  });

  /** Relative "Published 3 days ago" — driven by deployment.updatedAt for the mock. */
  publishedRelative = computed<string | null>(() => {
    if (this.deployment.snapshotVersion === 0) return null;
    const updated = new Date(this.deployment.updatedAt);
    if (Number.isNaN(updated.getTime())) return null;
    const days = Math.floor((Date.now() - updated.getTime()) / 86400000);
    if (days <= 0) return 'Published today';
    if (days === 1) return 'Published yesterday';
    if (days < 30) return `Published ${days} days ago`;
    const months = Math.floor(days / 30);
    if (months === 1) return 'Published 1 month ago';
    return `Published ${months} months ago`;
  });

  /** Returns the single forward step the user can take from the current status. */
  nextAction = computed<NextAction | null>(() => {
    const s = this.status();
    if (s === 'Draft' || s === 'Tested') {
      return {
        label: 'Publish snapshot',
        icon: 'pi pi-bookmark',
        busyKey: 'publish',
        disabled: false,
        handler: () => this.publish(),
      };
    }
    if (s === 'Published') {
      return {
        label: 'Activate',
        icon: 'pi pi-arrow-right',
        busyKey: 'activate',
        disabled: !this.canActivate(),
        handler: () => this.activate(),
      };
    }
    if (s === 'Retired') {
      return {
        label: 'Reactivate',
        icon: 'pi pi-refresh',
        busyKey: 'reactivate',
        disabled: false,
        handler: () => this.reactivate(),
      };
    }
    // Active is a terminal "happy" state — no forward action, manage via menu.
    return null;
  });

  /** Status-dependent Manage menu. Destructive actions sit below a separator. */
  manageMenuItems = computed<MenuItem[]>(() => {
    const s = this.status();
    const items: MenuItem[] = [];

    if (s === 'Active') {
      items.push({
        label: 'Publish new version',
        icon: 'pi pi-pencil',
        command: () => this.publishNewVersion(),
      });
      items.push({
        label: 'Roll back to previous version',
        icon: 'pi pi-undo',
        command: () => this.rollback(),
      });
      items.push({ separator: true });
      items.push({
        label: 'Retire deployment',
        icon: 'pi pi-pause',
        styleClass: 'menu-item--danger',
        command: () => this.retire(),
      });
    }

    return items;
  });

  /** Whether to render the Manage button at all (skip it on states with no actions). */
  hasManageActions = computed<boolean>(() => this.manageMenuItems().length > 0);

  /** Stepper progression. Retired is rendered with a special class on the <ol>. */
  stepState(step: StepKey): StepState {
    const s = this.status();
    if (s === 'Retired') {
      // All three steps show as completed but visually muted via stepper--retired.
      return 'done';
    }
    if (step === 'draft') {
      if (s === 'Draft' || s === 'Tested') return 'current';
      return 'done';
    }
    if (step === 'published') {
      if (s === 'Draft' || s === 'Tested') return 'pending';
      if (s === 'Published') return 'current';
      return 'done';
    }
    // active
    if (s === 'Active') return 'current';
    return 'pending';
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['deployment']) {
      this.localStatus.set(null);
      this.promotedAt.set(null);
    }
  }

  // ── Actions ───────────────────────────────────────────────────────
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

  /** From Active → Draft. Lets users demo the full Draft→Published→Active
   *  cycle on a single deployment without first retiring it. In a real product
   *  this is what happens implicitly when you edit the Connection or Mapping
   *  tabs of a live deployment. */
  publishNewVersion() {
    if (this.busyAction()) return;
    this.gen
      .confirm({
        title: 'Publish a new version?',
        text:
          'This drops the deployment back to Draft so you can capture an updated snapshot. ' +
          'Live traffic keeps flowing on the current snapshot until you re-Activate.',
        confirmText: 'Yes, start new version',
        confirmColor: '#0066cc',
        icon: 'info',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.busyAction.set('publish');
        setTimeout(() => {
          this.localStatus.set('Draft');
          this.busyAction.set(null);
          this.gen.success('New version started — Publish when ready.');
          this.statusChanged.emit();
        }, 400);
      });
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

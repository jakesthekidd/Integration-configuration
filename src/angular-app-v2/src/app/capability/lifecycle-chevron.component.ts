import { Component, EventEmitter, Output, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';

import { VersionState } from '../models/version.model';

type Stage = 'Draft' | 'Published' | 'Activated';

/**
 * Lifecycle chevron — the primary lifecycle control for one version.
 *
 * Three arrow-shaped segments (Draft / Publish→Published / Activate→Activated)
 * styled per Figma component 116. Click is always forward; the chevron emits
 * an `advance` event with the target stage and the parent runs the confirm
 * + commit. When the version is Activated or Archived the chevron is inert.
 *
 * Archived versions render a separate pink "Archived" pill instead of the
 * three-stage chevron — see the archived branch in the template below.
 *
 * Signal inputs so the computeds stay reactive across @Input changes.
 *
 * See DESIGN-STATUS-VERSIONING.md §3 for the full spec.
 */
@Component({
  selector: 'app-lifecycle-chevron',
  imports: [CommonModule],
  template: `
    @if (current() === 'Archived') {
      <div class="archived-bar" aria-label="Archived">
        <i class="pi pi-inbox" aria-hidden="true"></i>
        <span class="archived-bar__label">Archived</span>
      </div>
    } @else {
      <div class="chev" [class.chev--inert]="inert()">
        <button
          type="button"
          class="chev__stage chev__stage--first"
          [attr.data-stage]="'Draft'"
          [attr.data-state]="stageState('Draft')"
          [disabled]="!isClickable('Draft')"
          (click)="onClick('Draft')"
        >
          <span class="chev__bullet">
            @if (showCheck('Draft')) {
              <i class="pi pi-check" aria-hidden="true"></i>
            }
          </span>
          <span class="chev__label">Draft</span>
        </button>

        <button
          type="button"
          class="chev__stage"
          [attr.data-stage]="'Published'"
          [attr.data-state]="stageState('Published')"
          [disabled]="!isClickable('Published')"
          (click)="onClick('Published')"
        >
          <span class="chev__bullet">
            @if (showCheck('Published')) {
              <i class="pi pi-check" aria-hidden="true"></i>
            }
          </span>
          <span class="chev__label">{{ labelFor('Published') }}</span>
        </button>

        <button
          type="button"
          class="chev__stage chev__stage--last"
          [attr.data-stage]="'Activated'"
          [attr.data-state]="stageState('Activated')"
          [disabled]="!isClickable('Activated')"
          (click)="onClick('Activated')"
        >
          <span class="chev__bullet">
            @if (showCheck('Activated')) {
              <i class="pi pi-check" aria-hidden="true"></i>
            }
          </span>
          <span class="chev__label">{{ labelFor('Activated') }}</span>
        </button>
      </div>
    }
  `,
  styles: [
    `
      :host {
        display: block;
      }

      /* ── Three-stage chevron ────────────────────────────────────── */
      .chev {
        display: flex;
        align-items: stretch;
        width: 100%;
        font-size: var(--tf-text-body);
        font-weight: 600;
      }

      .chev__stage {
        flex: 1 1 0;
        min-width: 0;
        display: inline-flex;
        align-items: center;
        gap: 10px;
        padding: 12px 28px 12px 36px;
        background: #f3f4f6;
        color: #9ca3af;
        border: 0;
        font-family: inherit;
        font-size: inherit;
        font-weight: inherit;
        cursor: pointer;
        transition: filter 0.12s ease;
        /* Arrow shape: notch on left, point on right */
        clip-path: polygon(
          0 0,
          calc(100% - 14px) 0,
          100% 50%,
          calc(100% - 14px) 100%,
          0 100%,
          14px 50%
        );
        margin-left: -14px;
      }
      .chev__stage--first {
        padding-left: 22px;
        margin-left: 0;
        /* First stage: no left notch */
        clip-path: polygon(
          0 0,
          calc(100% - 14px) 0,
          100% 50%,
          calc(100% - 14px) 100%,
          0 100%
        );
        border-top-left-radius: 6px;
        border-bottom-left-radius: 6px;
      }

      .chev__stage:disabled {
        cursor: not-allowed;
      }
      .chev__stage:not(:disabled):hover {
        filter: brightness(0.96);
      }
      .chev__stage:not(:disabled):focus-visible {
        outline: 2px solid #1d6fc0;
        outline-offset: -4px;
      }

      /* Bullet */
      .chev__bullet {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 22px;
        height: 22px;
        border-radius: 50%;
        background: transparent;
        border: 2px solid currentColor;
        font-size: 10px;
        flex-shrink: 0;
      }

      /* ── Per-stage color palette (per Figma component 116) ─────── */
      /* Draft — cream/amber */
      .chev__stage[data-stage='Draft'][data-state='current'],
      .chev__stage[data-stage='Draft'][data-state='done'] {
        background: #fef3d7;
        color: #8b5d00;
      }
      .chev__stage[data-stage='Draft'][data-state='current'] .chev__bullet,
      .chev__stage[data-stage='Draft'][data-state='done'] .chev__bullet {
        background: #8b5d00;
        border-color: #8b5d00;
        color: #fef3d7;
      }

      /* Published — light green */
      .chev__stage[data-stage='Published'][data-state='current'],
      .chev__stage[data-stage='Published'][data-state='done'] {
        background: #c5e6ce;
        color: #1b6b3a;
      }
      .chev__stage[data-stage='Published'][data-state='current'] .chev__bullet,
      .chev__stage[data-stage='Published'][data-state='done'] .chev__bullet {
        background: #1b6b3a;
        border-color: #1b6b3a;
        color: #c5e6ce;
      }

      /* Activated — deep green */
      .chev__stage[data-stage='Activated'][data-state='current'] {
        background: #1b6b3a;
        color: #ffffff;
      }
      .chev__stage[data-stage='Activated'][data-state='current'] .chev__bullet {
        background: #ffffff;
        border-color: #ffffff;
        color: #1b6b3a;
      }

      /* Pending / next-up — neutral gray */
      .chev__stage[data-state='pending'],
      .chev__stage[data-state='next'] {
        background: #f3f4f6;
        color: #9ca3af;
      }
      .chev__stage:not(:disabled)[data-state='next']:hover {
        background: #e5e7eb;
      }

      /* Inert mode — Activated terminal state. Same colors, no hover. */
      .chev--inert .chev__stage {
        cursor: default;
      }
      .chev--inert .chev__stage:hover {
        filter: none;
      }

      /* ── Archived bar ───────────────────────────────────────────── */
      .archived-bar {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 14px 24px;
        background: #fbe9ea;
        color: #83131a;
        border-radius: 6px;
        font-size: var(--tf-text-body);
        font-weight: 700;
      }
      .archived-bar i {
        font-size: 16px;
      }
      .archived-bar__label {
        letter-spacing: 0.2px;
      }
    `,
  ],
})
export class LifecycleChevronComponent {
  /** Current state of the version this chevron belongs to. Signal input
   *  so derived computeds stay reactive across @Input changes. */
  current = input.required<VersionState>();
  /** When true, the chevron is purely informational — no clicks. */
  inert = input<boolean>(false);

  @Output() advance = new EventEmitter<Stage>();

  /** Translates the version's state into where the chevron should sit.
   *  Archived is handled separately in the template. */
  private currentStage = computed<Stage>(() => {
    const c = this.current();
    if (c === 'Activated') return 'Activated';
    if (c === 'Published') return 'Published';
    return 'Draft';
  });

  stageState(stage: Stage): 'done' | 'current' | 'next' | 'pending' {
    const cur = this.currentStage();
    const order: Stage[] = ['Draft', 'Published', 'Activated'];
    const curIdx = order.indexOf(cur);
    const stageIdx = order.indexOf(stage);
    if (stageIdx < curIdx) return 'done';
    if (stageIdx === curIdx) return 'current';
    if (!this.inert() && stageIdx === curIdx + 1) return 'next';
    return 'pending';
  }

  /** Show the checkmark icon when the stage is done or current. */
  showCheck(stage: Stage): boolean {
    const s = this.stageState(stage);
    return s === 'done' || s === 'current';
  }

  /** Per-Figma: stage label morphs to past-tense when CURRENT.
   *  Draft is always "Draft" (state == action). */
  labelFor(stage: Stage): string {
    if (stage === 'Draft') return 'Draft';
    const cur = this.currentStage();
    if (stage === 'Published') return cur === 'Published' ? 'Published' : 'Publish';
    return cur === 'Activated' ? 'Activated' : 'Activate';
  }

  isClickable(stage: Stage): boolean {
    if (this.inert()) return false;
    return this.stageState(stage) === 'next';
  }

  onClick(stage: Stage) {
    if (!this.isClickable(stage)) return;
    this.advance.emit(stage);
  }
}

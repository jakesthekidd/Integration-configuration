import { Component, EventEmitter, Output, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';

import { VersionState } from '../models/version.model';

type Stage = 'Draft' | 'Published' | 'Activated';

/**
 * Lifecycle chevron — the primary lifecycle control for one version.
 *
 * Visually shows three forward stages: Draft → Published → Activated.
 * Click is always forward. The chevron emits an `advance` event with the
 * target stage; the parent runs the confirmation dialog and commits the
 * transition. When the version is Activated (or Archived), the chevron
 * is rendered inert — no stage is clickable.
 *
 * Uses Angular signal inputs so the computeds stay reactive when the
 * parent re-binds `current` or `inert`.
 *
 * See DESIGN-STATUS-VERSIONING.md §3 for the full spec.
 */
@Component({
  selector: 'app-lifecycle-chevron',
  imports: [CommonModule],
  template: `
    <div class="chevron" [class.chevron--inert]="inert()">
      <button
        type="button"
        class="chevron__stage"
        [attr.data-state]="stageState('Draft')"
        [disabled]="!isClickable('Draft')"
        (click)="onClick('Draft')"
      >
        <span class="stage__bullet">
          @if (stageState('Draft') === 'done') {
            <i class="pi pi-check" aria-hidden="true"></i>
          }
        </span>
        <span class="stage__label">Draft</span>
      </button>

      <span class="chevron__sep" [attr.data-state]="sepState('Published')"></span>

      <button
        type="button"
        class="chevron__stage"
        [attr.data-state]="stageState('Published')"
        [disabled]="!isClickable('Published')"
        (click)="onClick('Published')"
      >
        <span class="stage__bullet">
          @if (stageState('Published') === 'done') {
            <i class="pi pi-check" aria-hidden="true"></i>
          }
        </span>
        <span class="stage__label">Published</span>
      </button>

      <span class="chevron__sep" [attr.data-state]="sepState('Activated')"></span>

      <button
        type="button"
        class="chevron__stage"
        [attr.data-state]="stageState('Activated')"
        [disabled]="!isClickable('Activated')"
        (click)="onClick('Activated')"
      >
        <span class="stage__bullet">
          @if (stageState('Activated') === 'done') {
            <i class="pi pi-check" aria-hidden="true"></i>
          }
        </span>
        <span class="stage__label">Activated</span>
      </button>
    </div>
  `,
  styles: [
    `
      .chevron {
        display: inline-flex;
        align-items: center;
        gap: 0;
      }

      .chevron__stage {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        background: transparent;
        border: 0;
        padding: 6px 10px;
        margin: -6px -2px;
        border-radius: var(--tf-radius-sm);
        cursor: pointer;
        font-family: inherit;
        font-size: var(--tf-text-meta);
        font-weight: 600;
        color: var(--tf-text-muted);
        transition: background 0.12s ease;
      }
      .chevron__stage:disabled {
        cursor: not-allowed;
      }
      .chevron__stage:not(:disabled):hover {
        background: var(--tf-blue-50, #f0f7ff);
      }
      .chevron__stage:not(:disabled):focus-visible {
        outline: 2px solid #1d6fc0;
        outline-offset: 2px;
      }

      .stage__bullet {
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
      .chevron__stage[data-state='done'] .stage__bullet {
        background: #1d6fc0;
      }
      .chevron__stage[data-state='current'] .stage__bullet {
        background: white;
        border: 2px solid #1d6fc0;
        box-shadow: 0 0 0 4px rgba(29, 111, 192, 0.18);
      }
      .chevron__stage[data-state='next'] .stage__bullet {
        background: white;
        border: 2px dashed #94a3b8;
      }
      .chevron__stage:not(:disabled)[data-state='next']:hover .stage__bullet {
        border-color: #1d6fc0;
        border-style: solid;
      }

      .chevron__stage[data-state='done'] .stage__label,
      .chevron__stage[data-state='current'] .stage__label {
        color: var(--tf-text-strong);
      }
      .chevron__stage[data-state='next'] .stage__label {
        color: var(--tf-text-muted);
      }
      .chevron__stage:not(:disabled)[data-state='next']:hover .stage__label {
        color: #1d6fc0;
      }

      .chevron__sep {
        display: inline-block;
        flex: 0 0 36px;
        height: 2px;
        margin: 0 6px;
        background: var(--tf-slate-200);
      }
      .chevron__sep[data-state='done'] {
        background: #1d6fc0;
      }

      /* Inert mode — all stages locked, no hover, dimmed. Used for
         Activated (terminal) and on Archived containers' history view. */
      .chevron--inert .chevron__stage {
        cursor: default;
      }
      .chevron--inert .chevron__stage:hover {
        background: transparent;
      }
      .chevron--inert .chevron__stage[data-state='done'] .stage__bullet,
      .chevron--inert .chevron__sep[data-state='done'] {
        background: #6b8aa8;
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

  /** Translates the version's state into where the chevron should sit. */
  private currentStage = computed<Stage>(() => {
    const c = this.current();
    if (c === 'Activated') return 'Activated';
    if (c === 'Published') return 'Published';
    // Archived renders the chevron full + inert (treated like Activated for stages).
    if (c === 'Archived') return 'Activated';
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

  sepState(rightOf: Stage): 'done' | 'pending' {
    return this.stageState(rightOf) === 'done' ? 'done' : 'pending';
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

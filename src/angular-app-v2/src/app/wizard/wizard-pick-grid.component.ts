import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TagModule } from 'primeng/tag';

export interface PickItem {
  id: string;
  label: string;
  description?: string;
  /** PrimeIcons class, e.g. `pi pi-book` */
  icon?: string;
  /** Short meta line below description (e.g. "McLeod v23 · 12 templates") */
  meta?: string;
  /** Optional status tag rendered top-right of the card */
  tag?: { value: string; severity: 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' };
  /** When true, the card is non-selectable and dimmed */
  disabled?: boolean;
}

/**
 * Shared selection grid used by wizard pick-style steps:
 * Select Customer (step 1), Pick Application (2), Pick Capability (3),
 * Pick Connection (4), Pick Template (6), Pick API Client (8).
 *
 * Single-select. Emits the picked id; parent persists to WizardStateService.
 */
@Component({
  selector: 'wizard-pick-grid',
  standalone: true,
  imports: [TagModule],
  template: `
    @if (items.length === 0) {
      <p class="empty">No options available.</p>
    } @else {
      <div class="grid">
        @for (item of items; track item.id) {
          <button
            type="button"
            class="card"
            [class.card--selected]="item.id === selectedId"
            [class.card--disabled]="item.disabled"
            [disabled]="item.disabled"
            (click)="pick.emit(item.id)"
          >
            @if (item.tag) {
              <p-tag class="card__tag" [value]="item.tag.value" [severity]="item.tag.severity" [rounded]="true" />
            }
            @if (item.icon) {
              <span class="card__icon"><i [class]="item.icon" aria-hidden="true"></i></span>
            }
            <span class="card__label">{{ item.label }}</span>
            @if (item.description) {
              <span class="card__desc">{{ item.description }}</span>
            }
            @if (item.meta) {
              <span class="card__meta">{{ item.meta }}</span>
            }
          </button>
        }
      </div>
    }
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .empty {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
      }
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: var(--tf-space-3);
      }
      .card {
        position: relative;
        text-align: left;
        background: white;
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-md);
        padding: var(--tf-space-4);
        cursor: pointer;
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-1);
        transition: border-color 0.15s, box-shadow 0.15s, background 0.15s;
        font-family: inherit;
      }
      .card:hover:not(.card--disabled):not(.card--selected) {
        border-color: var(--tf-blue-400);
        background: var(--tf-blue-50);
      }
      .card--selected {
        border-color: var(--tf-blue-500);
        background: var(--tf-blue-50);
        box-shadow: 0 0 0 1px var(--tf-blue-500) inset;
      }
      .card--disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
      .card__tag {
        position: absolute;
        top: var(--tf-space-3);
        right: var(--tf-space-3);
      }
      .card__icon {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 36px;
        height: 36px;
        border-radius: var(--tf-radius-sm);
        background: var(--tf-blue-100);
        color: var(--tf-blue-700);
        font-size: 16px;
        margin-bottom: var(--tf-space-2);
      }
      .card--selected .card__icon {
        background: var(--tf-blue-500);
        color: white;
      }
      .card__label {
        font-size: var(--tf-text-heading);
        font-weight: 700;
        color: var(--tf-text-strong);
      }
      .card__desc {
        font-size: var(--tf-text-body);
        color: var(--tf-text-muted);
        line-height: 1.4;
      }
      .card__meta {
        margin-top: var(--tf-space-1);
        font-size: var(--tf-text-meta);
        color: var(--tf-text-muted);
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.4px;
      }
    `,
  ],
})
export class WizardPickGridComponent {
  @Input() items: PickItem[] = [];
  @Input() selectedId = '';
  @Output() pick = new EventEmitter<string>();
}

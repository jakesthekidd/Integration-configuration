import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface FilterChipOption<T = string> {
  /** Display label (e.g. "All", "Enabled"). */
  label: string;
  /** The value emitted when this chip is selected. */
  value: T;
  /** Optional count badge rendered to the right of the label. */
  count?: number;
}

/**
 * Standard pill-shaped filter chip group.
 *
 *   <app-filter-chips
 *     [options]="[
 *       { label: 'All', value: 'all', count: 25 },
 *       { label: 'Enabled', value: 'enabled', count: 10 },
 *       { label: 'Disabled', value: 'disabled', count: 15 }
 *     ]"
 *     [value]="statusFilter()"
 *     (valueChange)="setFilter($event)"
 *   />
 *
 * Selected chip → navy. Hover on selected chip → slightly darker navy (the
 * pre-extraction implementation had a hover bug where the selected chip's
 * background lightened on hover, making the white text unreadable).
 */
@Component({
  selector: 'app-filter-chips',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="chips" role="group" [attr.aria-label]="ariaLabel">
      @if (label) {
        <span class="chips__label">{{ label }}</span>
      }
      @for (opt of options; track opt.value) {
        <button
          type="button"
          class="chips__chip"
          [class.chips__chip--active]="opt.value === value"
          [attr.aria-pressed]="opt.value === value"
          (click)="onPick(opt.value)"
        >
          {{ opt.label }}
          @if (opt.count !== undefined) {
            <span class="chips__count">{{ opt.count }}</span>
          }
        </button>
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .chips {
        display: inline-flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 0.5rem;
      }
      .chips__label {
        font-size: 0.75rem;
        font-weight: 600;
        color: #64748b;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        margin-right: 0.25rem;
      }
      .chips__chip {
        border: 1px solid #e2e8f0;
        background: #ffffff;
        color: #334155;
        padding: 0.4rem 0.85rem;
        border-radius: 999px;
        font-size: 0.8125rem;
        font-weight: 500;
        cursor: pointer;
        display: inline-flex;
        align-items: center;
        gap: 0.5rem;
        transition: background-color 0.15s ease, border-color 0.15s ease, color 0.15s ease;
      }
      /* Hover applies only to non-active chips — active chip stays navy. */
      .chips__chip:not(.chips__chip--active):hover {
        background: #f8fafc;
        border-color: #cbd5e1;
      }
      .chips__chip--active {
        background: #1e3a8a;
        border-color: #1e3a8a;
        color: #ffffff;
      }
      /* Slightly darker navy on hover when active — keeps white text legible. */
      .chips__chip--active:hover {
        background: #1e40af;
        border-color: #1e40af;
      }
      .chips__chip:focus-visible {
        outline: 2px solid #3b82f6;
        outline-offset: 2px;
      }
      .chips__count {
        background: rgba(255, 255, 255, 0.18);
        color: inherit;
        font-size: 0.6875rem;
        font-weight: 600;
        padding: 0.05rem 0.4rem;
        border-radius: 999px;
        min-width: 1.5rem;
        text-align: center;
      }
      .chips__chip:not(.chips__chip--active) .chips__count {
        background: #f1f5f9;
        color: #475569;
      }
    `,
  ],
})
export class FilterChipsComponent<T = string> {
  @Input({ required: true }) options: FilterChipOption<T>[] = [];

  /** Currently-selected value. */
  @Input() value!: T;

  /** Optional inline label rendered before the first chip (e.g. "STATUS:"). */
  @Input() label?: string;

  /** Accessibility label for the chip group. */
  @Input() ariaLabel = 'Filter';

  @Output() valueChange = new EventEmitter<T>();

  onPick(v: T) {
    if (v === this.value) return;
    this.valueChange.emit(v);
  }
}

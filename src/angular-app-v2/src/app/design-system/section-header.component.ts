import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Standard section-header used above every data table.
 *
 * Replaces both the legacy navy gradient banner ("Templates" / "Lookup
 * Tables Management") and the plain h2 patterns ("Connections"). Caller
 * projects the right-side action button(s) via the default content slot.
 *
 *   <app-section-header title="Connections" subtitle="...">
 *     <button pButton label="Create Connection" />
 *   </app-section-header>
 */
@Component({
  selector: 'app-section-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <header class="sh">
      <div class="sh__text">
        <h2 class="sh__title">{{ title }}</h2>
        @if (subtitle) {
          <p class="sh__subtitle">{{ subtitle }}</p>
        }
      </div>
      <div class="sh__actions">
        <ng-content></ng-content>
      </div>
    </header>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .sh {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 2rem;
        margin-bottom: 1rem;
      }
      .sh__title {
        font-size: 1.5rem;
        font-weight: 600;
        color: #0f172a;
        margin: 0 0 0.25rem;
      }
      .sh__subtitle {
        color: #64748b;
        font-size: 0.875rem;
        margin: 0;
        max-width: 64ch;
        line-height: 1.4;
      }
      .sh__actions {
        flex-shrink: 0;
        display: flex;
        gap: 0.5rem;
        align-items: center;
      }
    `,
  ],
})
export class SectionHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() subtitle?: string;
}

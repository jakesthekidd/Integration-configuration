import { Component, Input, computed, signal } from '@angular/core';
import { TagModule } from 'primeng/tag';

/**
 * Canonical status pill used across every table in the app.
 *
 * Maps a small fixed vocabulary of statuses to PrimeNG severities so the same
 * word always renders the same color. Anything outside the vocabulary falls
 * back to a neutral "secondary" tag — that's intentional, so a typo doesn't
 * crash; you just see a gray pill and notice.
 */
type Severity = 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast';

const STATUS_TO_SEVERITY: Record<string, Severity> = {
  // Healthy
  active: 'success',
  enabled: 'success',
  published: 'info',
  // In-progress
  draft: 'warn',
  pending: 'warn',
  // Inert
  archived: 'secondary',
  disabled: 'secondary',
  inactive: 'secondary',
  // Bad
  error: 'danger',
  failed: 'danger',
};

@Component({
  selector: 'app-status-tag',
  standalone: true,
  imports: [TagModule],
  template: `
    <p-tag [severity]="severity()" [value]="label()" [rounded]="true" />
  `,
  styles: [
    `
      :host ::ng-deep .p-tag {
        font-weight: 600;
        font-size: 0.6875rem;
        letter-spacing: 0.025em;
        text-transform: uppercase;
        padding: 0.2rem 0.55rem;
      }
    `,
  ],
})
export class StatusTagComponent {
  private statusSig = signal('');

  /**
   * Status string, case-insensitive. Examples: "Active", "Draft", "Published",
   * "Archived", "Enabled", "Disabled", "Error".
   */
  @Input({ required: true })
  set status(value: string) {
    this.statusSig.set(value ?? '');
  }

  severity = computed<Severity>(
    () => STATUS_TO_SEVERITY[this.statusSig().trim().toLowerCase()] ?? 'secondary',
  );

  label = computed(() => {
    const v = this.statusSig().trim();
    if (!v) return '—';
    // Title-case ("Active") so it reads cleanly even if the caller passes "active".
    return v.charAt(0).toUpperCase() + v.slice(1).toLowerCase();
  });
}

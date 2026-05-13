import { Injectable, inject } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';

/**
 * GeneralService — thin facade over PrimeNG `ConfirmationService` + `MessageService`.
 *
 * Keeps the previous SweetAlert2 surface so existing call sites (`generalService.confirm(...)`,
 * `.success(...)`, `.error(...)`) don't need to change. `confirm()` returns a Promise that
 * resolves to `{ isConfirmed: boolean }`, matching the historical Swal contract.
 */

export interface ConfirmResult {
  isConfirmed: boolean;
  isDismissed: boolean;
}

export type ConfirmIcon = 'question' | 'warning' | 'info' | 'success' | 'error';

export interface ConfirmOptions {
  title: string;
  text?: string;
  confirmText?: string;
  cancelText?: string;
  icon?: ConfirmIcon;
  /**
   * Historical: a hex color for the confirm button. We map this to a PrimeNG severity
   * so the dialog stays themed. Accepts loose strings — non-matching values fall back to "primary".
   */
  confirmColor?: string;
}

const ICON_TO_PI: Record<ConfirmIcon, string> = {
  question: 'pi pi-question-circle',
  warning: 'pi pi-exclamation-triangle',
  info: 'pi pi-info-circle',
  success: 'pi pi-check-circle',
  error: 'pi pi-times-circle',
};

/** Map a historical hex (or omitted) to a PrimeNG severity for the confirm button. */
function severityFromColor(color: string | undefined): 'primary' | 'danger' | 'success' | 'warn' | 'info' {
  if (!color) return 'primary';
  const c = color.toLowerCase().replace('#', '');
  // common Swal palette mappings used in the codebase
  if (c.startsWith('e74') || c.startsWith('d33') || c === 'e74c3c') return 'danger';
  if (c.startsWith('28a') || c.startsWith('2ec') || c === '28a745') return 'success';
  if (c.startsWith('ff') || c.startsWith('f0a') || c === 'f0ad4e') return 'warn';
  return 'primary';
}

@Injectable({ providedIn: 'root' })
export class GeneralService {
  private confirmService = inject(ConfirmationService);
  private messageService = inject(MessageService);

  /**
   * Show a confirmation dialog. Returns a Promise that resolves to a SweetAlert-shaped result
   * so existing `result.isConfirmed` checks keep working.
   */
  confirm(options: ConfirmOptions): Promise<ConfirmResult> {
    return new Promise<ConfirmResult>((resolve) => {
      this.confirmService.confirm({
        header: options.title,
        message: options.text ?? '',
        icon: ICON_TO_PI[options.icon ?? 'question'],
        acceptLabel: options.confirmText ?? 'Yes',
        rejectLabel: options.cancelText ?? 'Cancel',
        acceptButtonProps: { severity: severityFromColor(options.confirmColor) },
        rejectButtonProps: { severity: 'secondary', outlined: true },
        accept: () => resolve({ isConfirmed: true, isDismissed: false }),
        reject: () => resolve({ isConfirmed: false, isDismissed: true }),
      });
    });
  }

  simpleConfirm(message: string): Promise<ConfirmResult> {
    return this.confirm({ title: message, icon: 'question' });
  }

  /** Show a transient success toast. Returns a resolved Promise so existing `await`/`.then(...)` calls still work. */
  success(message: string): Promise<void> {
    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: message,
      life: 2500,
    });
    return Promise.resolve();
  }

  /** Show an error toast. Sticky-ish — 5s — so users actually read it. */
  error(message: string): Promise<void> {
    this.messageService.add({
      severity: 'error',
      summary: 'Error',
      detail: message,
      life: 5000,
    });
    return Promise.resolve();
  }
}

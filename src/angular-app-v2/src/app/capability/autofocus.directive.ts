import { AfterViewInit, Directive, ElementRef } from '@angular/core';

/**
 * Focuses the host element on view init. Standalone utility for inline
 * edit affordances (e.g. textarea that appears after click-to-edit).
 */
@Directive({
  selector: '[appAutofocus]',
  standalone: true,
})
export class AutofocusDirective implements AfterViewInit {
  constructor(private el: ElementRef<HTMLElement>) {}

  ngAfterViewInit() {
    // Microtask so the focus happens after Angular finishes the current
    // change-detection pass — otherwise focus may be stolen by the click
    // that just triggered the structural change.
    queueMicrotask(() => {
      const el = this.el.nativeElement;
      if (typeof (el as HTMLInputElement | HTMLTextAreaElement).select === 'function') {
        el.focus();
        // For text fields, place the caret at the end of the content.
        const inputLike = el as HTMLInputElement | HTMLTextAreaElement;
        const len = inputLike.value?.length ?? 0;
        try {
          inputLike.setSelectionRange(len, len);
        } catch {
          /* ignored — not all inputs support setSelectionRange */
        }
      } else {
        el.focus();
      }
    });
  }
}

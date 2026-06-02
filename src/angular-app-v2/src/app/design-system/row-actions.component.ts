import { Component, Input, ViewChild } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { MenuModule, Menu } from 'primeng/menu';
import { MenuItem } from 'primeng/api';

/**
 * Standard kebab-menu row action. Use this in every data table's "Actions"
 * column instead of stamping inline View/Edit/Delete buttons.
 *
 * Caller passes a `MenuItem[]` (PrimeNG menu model). The component renders a
 * single `⋯` icon button; clicking it pops a shared `p-menu` with the items.
 */
@Component({
  selector: 'app-row-actions',
  standalone: true,
  imports: [ButtonModule, MenuModule],
  template: `
    <button
      pButton
      type="button"
      icon="pi pi-ellipsis-h"
      [text]="true"
      [rounded]="true"
      severity="secondary"
      size="small"
      [attr.aria-label]="ariaLabel"
      (click)="menu.toggle($event)"
    ></button>
    <p-menu #menu [model]="items" [popup]="true" appendTo="body" />
  `,
  styles: [
    `
      :host {
        display: inline-block;
      }
      :host ::ng-deep .p-button.p-button-icon-only {
        width: 2rem;
        height: 2rem;
      }
    `,
  ],
})
export class RowActionsComponent {
  @Input({ required: true }) items: MenuItem[] = [];
  @Input() ariaLabel = 'Row actions';

  @ViewChild('menu') menu!: Menu;
}

import { Component, OnDestroy, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { TooltipModule } from 'primeng/tooltip';

import { CustomerAccessService } from '../../services/customer-access.service';
import { GeneralService } from '../../services/general.service';
import { Customer } from '../../models/customer.model';
import { FilterChipsComponent, FilterChipOption } from '../../design-system/filter-chips.component';

type StatusFilter = 'all' | 'enabled' | 'disabled';

/**
 * Integration Library → "Customers" tab.
 *
 * Developer-curated allowlist of which customers appear in the Customer Setup app.
 * New customers default to Disabled (opt-in). Enable is instant + inline undo banner;
 * Disable goes through a confirmation dialog. Bulk select supports both.
 *
 * All mutations flow through CustomerAccessService.setIntegrationEnabled — the single
 * seam a backend dev will swap for a Supabase write later.
 */
@Component({
  selector: 'app-customers-access',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    TagModule,
    InputTextModule,
    CheckboxModule,
    TooltipModule,
    FilterChipsComponent,
  ],
  template: `
    <section class="ca">
      <!-- Header / controls row -->
      <header class="ca__header">
        <div class="ca__intro">
          <h2 class="ca__title">Customers</h2>
          <p class="ca__subtitle">
            Control which customers are visible in the Customer Setup app.
            Disabled customers stay configured but are hidden from the customer list.
          </p>
        </div>

        <div class="ca__controls">
          <span class="p-input-icon-left ca__search">
            <i class="pi pi-search"></i>
            <input
              pInputText
              type="text"
              placeholder="Search by customer name or code"
              [(ngModel)]="searchTermModel"
              (ngModelChange)="onSearchChange($event)"
            />
          </span>
        </div>
      </header>

      <!-- Filter chips -->
      <app-filter-chips
        class="ca__filter"
        ariaLabel="Status filter"
        [options]="filterOptions()"
        [value]="statusFilter()"
        (valueChange)="setFilter($event)"
      />


      <!-- Bulk action bar — appears on selection -->
      @if (selected().length > 0) {
        <div class="ca__bulk" role="region" aria-label="Bulk actions">
          <span class="ca__bulk-count">
            <strong>{{ selected().length }}</strong> selected
          </span>
          <div class="ca__bulk-actions">
            <button
              pButton
              type="button"
              label="Enable"
              icon="pi pi-check"
              severity="primary"
              size="small"
              (click)="bulkEnable()"
            ></button>
            <button
              pButton
              type="button"
              label="Disable"
              icon="pi pi-ban"
              severity="secondary"
              [outlined]="true"
              size="small"
              (click)="bulkDisable()"
            ></button>
            <button
              pButton
              type="button"
              label="Clear"
              icon="pi pi-times"
              [text]="true"
              size="small"
              (click)="clearSelection()"
            ></button>
          </div>
        </div>
      }

      <!-- Inline Undo banner — appears for 5s after any Enable -->
      @if (undoVisible()) {
        <div class="ca__undo" role="status">
          <span class="ca__undo-icon"><i class="pi pi-check-circle"></i></span>
          <span class="ca__undo-text">
            Enabled access for
            <strong>{{ pendingUndo()!.ids.length }}</strong>
            customer{{ pendingUndo()!.ids.length === 1 ? '' : 's' }}.
          </span>
          <button
            pButton
            type="button"
            label="Undo"
            icon="pi pi-undo"
            [text]="true"
            size="small"
            (click)="undo()"
          ></button>
          <span class="ca__undo-timer">{{ undoSecondsLeft() }}s</span>
        </div>
      }

      <!-- Table -->
      <p-table
        [value]="filteredRows()"
        [(selection)]="selectedRowsModel"
        (selectionChange)="onSelectionChange($event)"
        dataKey="customerId"
        [sortField]="'customerName'"
        [sortOrder]="1"
        [paginator]="true"
        [rows]="25"
        [rowsPerPageOptions]="[10, 25, 50]"
        styleClass="p-datatable-sm ca__table"
        [globalFilterFields]="['customerName', 'expressCustomerCode']"
      >
        <ng-template pTemplate="header">
          <tr>
            <th style="width: 3rem">
              <p-tableHeaderCheckbox></p-tableHeaderCheckbox>
            </th>
            <th pSortableColumn="customerName">
              Customer name <p-sortIcon field="customerName"></p-sortIcon>
            </th>
            <th pSortableColumn="expressCustomerCode" style="width: 11rem">
              Express Code <p-sortIcon field="expressCustomerCode"></p-sortIcon>
            </th>
            <th style="width: 14rem">Applications</th>
            <th pSortableColumn="integrationEnabled" style="width: 9rem">
              Status <p-sortIcon field="integrationEnabled"></p-sortIcon>
            </th>
            <th pSortableColumn="integrationStatusChangedAt" style="width: 10rem">
              Last changed <p-sortIcon field="integrationStatusChangedAt"></p-sortIcon>
            </th>
            <th style="width: 7rem; text-align: center">Access</th>
          </tr>
        </ng-template>

        <ng-template pTemplate="body" let-c>
          <tr>
            <td>
              <p-tableCheckbox [value]="c"></p-tableCheckbox>
            </td>
            <td class="ca__cell-name">{{ c.customerName }}</td>
            <td><code class="ca__code">{{ c.expressCustomerCode || '—' }}</code></td>
            <td>
              @if (c.applications?.length) {
                <span class="ca__pills">
                  @for (app of c.applications; track app) {
                    <span class="ca__pill">{{ app }}</span>
                  }
                </span>
              } @else {
                <span class="ca__muted">—</span>
              }
            </td>
            <td>
              <p-tag
                [severity]="c.integrationEnabled ? 'success' : 'secondary'"
                [value]="c.integrationEnabled ? 'Enabled' : 'Disabled'"
              ></p-tag>
            </td>
            <td>
              <span
                [pTooltip]="absoluteDate(c.integrationStatusChangedAt)"
                tooltipPosition="top"
                class="ca__muted-time"
              >
                {{ relativeDate(c.integrationStatusChangedAt) }}
              </span>
            </td>
            <td style="text-align: center">
              <button
                pButton
                type="button"
                [icon]="c.integrationEnabled ? 'pi pi-check' : 'pi pi-ban'"
                [label]="c.integrationEnabled ? 'On' : 'Off'"
                size="small"
                [severity]="c.integrationEnabled ? 'success' : 'secondary'"
                [outlined]="!c.integrationEnabled"
                (click)="toggleOne(c)"
              ></button>
            </td>
          </tr>
        </ng-template>

        <ng-template pTemplate="emptystate">
          <tr>
            <td colspan="7">
              <div class="ca__empty">
                <i class="pi pi-search ca__empty-icon"></i>
                <h3>No customers match your filters</h3>
                <p>Try clearing your search or switching to a different status chip.</p>
                <button
                  pButton
                  type="button"
                  label="Clear filters"
                  icon="pi pi-filter-slash"
                  [text]="true"
                  size="small"
                  (click)="clearFilters()"
                ></button>
              </div>
            </td>
          </tr>
        </ng-template>
      </p-table>
    </section>
  `,
  styles: [
    `
      :host {
        display: block;
        height: 100%;
        overflow: auto;
      }
      .ca {
        padding: 1.5rem 2rem 3rem;
        max-width: 1400px;
        margin: 0 auto;
      }
      .ca__header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 2rem;
        margin-bottom: 1rem;
      }
      .ca__title {
        font-size: 1.5rem;
        font-weight: 600;
        color: #0f172a;
        margin: 0 0 0.25rem;
      }
      .ca__subtitle {
        color: #64748b;
        font-size: 0.875rem;
        margin: 0;
        max-width: 56ch;
      }
      .ca__controls {
        flex-shrink: 0;
      }
      .ca__search {
        position: relative;
        display: inline-block;
      }
      .ca__search i {
        position: absolute;
        left: 0.75rem;
        top: 50%;
        transform: translateY(-50%);
        color: #94a3b8;
        z-index: 1;
      }
      .ca__search input {
        padding-left: 2.25rem;
        width: 320px;
      }
      .ca__filter {
        display: block;
        margin: 0.75rem 0 1rem;
      }
      .ca__bulk {
        display: flex;
        align-items: center;
        justify-content: space-between;
        background: #eff6ff;
        border: 1px solid #bfdbfe;
        border-radius: 8px;
        padding: 0.65rem 1rem;
        margin-bottom: 0.75rem;
        position: sticky;
        top: 0;
        z-index: 5;
      }
      .ca__bulk-count {
        font-size: 0.875rem;
        color: #1e3a8a;
      }
      .ca__bulk-actions {
        display: flex;
        gap: 0.5rem;
      }
      .ca__undo {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        background: #ecfdf5;
        border: 1px solid #a7f3d0;
        color: #065f46;
        padding: 0.6rem 1rem;
        border-radius: 8px;
        margin-bottom: 0.75rem;
        font-size: 0.875rem;
      }
      .ca__undo-icon i {
        font-size: 1rem;
      }
      .ca__undo-text {
        flex: 1;
      }
      .ca__undo-timer {
        font-variant-numeric: tabular-nums;
        color: #047857;
        font-weight: 600;
        font-size: 0.75rem;
      }
      .ca__table {
        background: #ffffff;
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        overflow: hidden;
      }
      .ca__cell-name {
        font-weight: 500;
        color: #0f172a;
      }
      .ca__code {
        background: #f1f5f9;
        color: #334155;
        padding: 0.1rem 0.45rem;
        border-radius: 4px;
        font-size: 0.75rem;
        font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, monospace;
      }
      .ca__pills {
        display: inline-flex;
        gap: 0.25rem;
        flex-wrap: wrap;
      }
      .ca__pill {
        background: #f1f5f9;
        color: #334155;
        font-size: 0.6875rem;
        font-weight: 500;
        padding: 0.125rem 0.5rem;
        border-radius: 999px;
      }
      .ca__muted {
        color: #94a3b8;
      }
      .ca__muted-time {
        color: #64748b;
        font-size: 0.8125rem;
        cursor: default;
      }
      .ca__empty {
        text-align: center;
        padding: 3rem 1rem;
        color: #64748b;
      }
      .ca__empty-icon {
        font-size: 2rem;
        color: #cbd5e1;
        margin-bottom: 0.75rem;
        display: block;
      }
      .ca__empty h3 {
        font-size: 1rem;
        font-weight: 600;
        color: #334155;
        margin: 0 0 0.25rem;
      }
      .ca__empty p {
        margin: 0 0 1rem;
        font-size: 0.875rem;
      }
    `,
  ],
})
export class CustomersAccessComponent implements OnDestroy {
  private access = inject(CustomerAccessService);
  private general = inject(GeneralService);

  // ---- Filter / search state ----
  searchTerm = signal('');
  searchTermModel = ''; // ngModel mirror
  statusFilter = signal<StatusFilter>('all');

  // ---- Selection state (p-table binds via ngModel-shaped two-way) ----
  selected = signal<Customer[]>([]);
  selectedRowsModel: Customer[] = [];

  // ---- Reactive view bits from the service ----
  counts = this.access.counts;

  /** Filter chip options, decorated with live counts. */
  filterOptions = computed<FilterChipOption<StatusFilter>[]>(() => {
    const c = this.counts();
    return [
      { label: 'All', value: 'all', count: c.all },
      { label: 'Enabled', value: 'enabled', count: c.enabled },
      { label: 'Disabled', value: 'disabled', count: c.disabled },
    ];
  });
  pendingUndo = this.access.pendingUndo;
  undoSecondsLeft = signal(0);
  private undoTimer: number | null = null;

  /** Sort customers A→Z, then apply the search + status chip filters. */
  filteredRows = computed<Customer[]>(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const status = this.statusFilter();
    return this.access.decoratedCustomers().filter((c) => {
      if (status === 'enabled' && !c.integrationEnabled) return false;
      if (status === 'disabled' && c.integrationEnabled) return false;
      if (term) {
        const inName = c.customerName.toLowerCase().includes(term);
        const inCode = (c.expressCustomerCode ?? '').toLowerCase().includes(term);
        if (!inName && !inCode) return false;
      }
      return true;
    });
  });

  undoVisible = computed(() => !!this.pendingUndo() && this.undoSecondsLeft() > 0);

  constructor() {
    // Arm a 5-second countdown whenever a fresh undo snapshot lands.
    effect(() => {
      const snap = this.pendingUndo();
      if (snap) {
        this.armUndoTimer();
      }
    });
  }

  ngOnDestroy(): void {
    if (this.undoTimer != null) clearInterval(this.undoTimer);
  }

  // ---- Filter handlers ----
  onSearchChange(value: string) {
    this.searchTerm.set(value);
  }

  setFilter(f: StatusFilter) {
    this.statusFilter.set(f);
  }

  clearFilters() {
    this.searchTerm.set('');
    this.searchTermModel = '';
    this.statusFilter.set('all');
  }

  // ---- Selection ----
  onSelectionChange(rows: Customer[]) {
    this.selected.set(rows ?? []);
  }

  clearSelection() {
    this.selectedRowsModel = [];
    this.selected.set([]);
  }

  // ---- Single-row toggle ----
  async toggleOne(c: Customer) {
    if (c.integrationEnabled) {
      const confirmed = await this.confirmDisable(1, c.customerName);
      if (!confirmed) return;
      this.access.setIntegrationEnabled([c.customerId], false);
    } else {
      this.access.setIntegrationEnabled([c.customerId], true);
    }
  }

  // ---- Bulk actions ----
  bulkEnable() {
    const ids = this.selected().map((c) => c.customerId);
    if (!ids.length) return;
    this.access.setIntegrationEnabled(ids, true);
    this.clearSelection();
  }

  async bulkDisable() {
    const rows = this.selected();
    if (!rows.length) return;
    const confirmed = await this.confirmDisable(rows.length);
    if (!confirmed) return;
    this.access.setIntegrationEnabled(
      rows.map((c) => c.customerId),
      false,
    );
    this.clearSelection();
  }

  private async confirmDisable(count: number, name?: string): Promise<boolean> {
    const subject = count === 1 ? `"${name}"` : `${count} customers`;
    const result = await this.general.confirm({
      title: `Disable access for ${subject}?`,
      text: `Disabled customers are hidden from the Customer Setup app. Existing configuration is preserved — you can re-enable anytime.`,
      icon: 'warning',
      confirmText: count === 1 ? 'Disable' : `Disable ${count}`,
      cancelText: 'Cancel',
      confirmColor: '#e74c3c',
    });
    return result.isConfirmed;
  }

  // ---- Undo ----
  undo() {
    this.access.undo();
    this.undoSecondsLeft.set(0);
    if (this.undoTimer != null) {
      clearInterval(this.undoTimer);
      this.undoTimer = null;
    }
  }

  private armUndoTimer() {
    if (this.undoTimer != null) clearInterval(this.undoTimer);
    this.undoSecondsLeft.set(5);
    this.undoTimer = window.setInterval(() => {
      const next = this.undoSecondsLeft() - 1;
      this.undoSecondsLeft.set(next);
      if (next <= 0) {
        if (this.undoTimer != null) clearInterval(this.undoTimer);
        this.undoTimer = null;
        this.access.clearUndo();
      }
    }, 1000);
  }

  // ---- Date helpers ----
  relativeDate(iso?: string): string {
    if (!iso) return '—';
    const then = new Date(iso).getTime();
    const now = Date.now();
    const sec = Math.floor((now - then) / 1000);
    if (sec < 60) return 'just now';
    const min = Math.floor(sec / 60);
    if (min < 60) return `${min}m ago`;
    const hr = Math.floor(min / 60);
    if (hr < 24) return `${hr}h ago`;
    const day = Math.floor(hr / 24);
    if (day < 30) return `${day}d ago`;
    const mo = Math.floor(day / 30);
    if (mo < 12) return `${mo}mo ago`;
    return `${Math.floor(mo / 12)}y ago`;
  }

  absoluteDate(iso?: string): string {
    if (!iso) return '';
    return new Date(iso).toLocaleString();
  }
}

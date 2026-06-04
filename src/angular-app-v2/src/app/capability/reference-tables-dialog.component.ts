import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';

import { ApiService } from '../services/api.service';
import { GeneralService } from '../services/general.service';
import { LookupTable } from '../models/lookup-table.model';

interface XrefRow {
  source: string;
  target: string;
}

/**
 * Reference Tables dialog — read-only "cheat sheet" of cross-reference
 * (lookup) tables relevant to the current deployment.
 *
 * Resolution: a table is considered relevant when its `tmsSystemId`
 * matches EITHER the deployment's `connectionId` (legacy seed data
 * uses connection ids) OR the customer's `tmsName` (newer seeds key
 * by TMS). This double-lookup keeps existing data working while
 * allowing new tables to be keyed cleanly by TMS.
 *
 * Editing happens elsewhere — clicking "Edit in Library" routes to
 * the existing lookup-tables editor under /library/lookup-tables.
 */
@Component({
  selector: 'app-reference-tables-dialog',
  imports: [
    CommonModule,
    FormsModule,
    DialogModule,
    TableModule,
    InputTextModule,
    ButtonModule,
    TagModule,
    TooltipModule,
  ],
  template: `
    <p-dialog
      [(visible)]="visible"
      (visibleChange)="visibleChange.emit($event)"
      [modal]="true"
      [closable]="true"
      [draggable]="false"
      [resizable]="false"
      [style]="{ width: '900px', maxWidth: '95vw' }"
      [contentStyle]="{ padding: '0' }"
      header="Reference tables (read-only)"
    >
      <div class="xref-banner">
        <i class="pi pi-info-circle"></i>
        <span>
          These are the cross-reference tables your mapping can call. They translate source-system
          codes (e.g. <code>AVL</code>) into platform values (e.g. <code>Available</code>).
          This view is read-only — open the Integration Library to edit.
        </span>
        <a class="xref-banner__link" [href]="libraryUrl" target="_blank" rel="noopener">
          Edit in Library
          <i class="pi pi-external-link"></i>
        </a>
      </div>

      @if (loading()) {
        <div class="xref-loading">
          <i class="pi pi-spin pi-spinner"></i>
          Loading reference tables…
        </div>
      } @else if (tables().length === 0) {
        <div class="xref-empty">
          <i class="pi pi-table"></i>
          <h4>No reference tables yet</h4>
          <p>
            No cross-reference tables are configured for this connection.
            Add one in the Integration Library to make it available here.
          </p>
        </div>
      } @else {
        <div class="xref-layout">
          <!-- Left rail: list of tables -->
          <aside class="xref-rail">
            <header class="xref-rail__head">
              <span class="xref-rail__count">
                {{ tables().length }} {{ tables().length === 1 ? 'table' : 'tables' }}
              </span>
            </header>
            <ul class="xref-list">
              @for (t of tables(); track t.id) {
                <li>
                  <button
                    type="button"
                    class="xref-list__item"
                    [class.xref-list__item--active]="selectedId() === t.id"
                    (click)="selectedId.set(t.id)"
                  >
                    <span class="xref-list__name">{{ t.name }}</span>
                    <span class="xref-list__field">
                      <i class="pi pi-bookmark"></i>
                      {{ t.fieldName }}
                    </span>
                    <span class="xref-list__count">
                      {{ entryCount(t) }} {{ entryCount(t) === 1 ? 'entry' : 'entries' }}
                    </span>
                  </button>
                </li>
              }
            </ul>
          </aside>

          <!-- Right pane: selected table detail -->
          <section class="xref-detail">
            @if (selected(); as t) {
              <header class="xref-detail__head">
                <div>
                  <h3>{{ t.name }}</h3>
                  @if (t.description) {
                    <p class="muted">{{ t.description }}</p>
                  }
                </div>
                <div class="xref-detail__meta">
                  <p-tag
                    severity="info"
                    [value]="'Field: ' + t.fieldName"
                    icon="pi pi-bookmark"
                  />
                  @if (t.defaultValue) {
                    <p-tag
                      severity="secondary"
                      [value]="'Default: ' + t.defaultValue"
                    />
                  }
                  <p-tag
                    [severity]="t.isCaseSensitive ? 'warn' : 'success'"
                    [value]="t.isCaseSensitive ? 'Case-sensitive' : 'Case-insensitive'"
                    [icon]="t.isCaseSensitive ? 'pi pi-exclamation-triangle' : 'pi pi-check'"
                  />
                </div>
              </header>

              <div class="xref-detail__search">
                <span class="p-input-icon-left">
                  <i class="pi pi-search"></i>
                  <input
                    pInputText
                    type="text"
                    [ngModel]="filterText()"
                    (ngModelChange)="filterText.set($event)"
                    placeholder="Search by source code or platform value…"
                  />
                </span>
                <span class="xref-detail__hint">
                  Showing {{ filteredRows().length }} of {{ entryCount(t) }}
                </span>
              </div>

              <p-table
                [value]="filteredRows()"
                [rowHover]="true"
                [scrollable]="true"
                scrollHeight="320px"
                styleClass="p-datatable-sm p-datatable-striped xref-table"
              >
                <ng-template pTemplate="header">
                  <tr>
                    <th style="width: 45%;">Source code</th>
                    <th style="width: 10%; text-align: center;"></th>
                    <th>Platform value</th>
                    <th style="width: 80px;"></th>
                  </tr>
                </ng-template>
                <ng-template pTemplate="body" let-row>
                  <tr>
                    <td>
                      <code class="xref-code">{{ row.source }}</code>
                    </td>
                    <td class="xref-arrow">
                      <i class="pi pi-arrow-right"></i>
                    </td>
                    <td>
                      <span class="xref-value">{{ row.target }}</span>
                    </td>
                    <td class="xref-copy">
                      <p-button
                        icon="pi pi-copy"
                        size="small"
                        [text]="true"
                        [rounded]="true"
                        severity="secondary"
                        pTooltip="Copy platform value"
                        tooltipPosition="left"
                        (onClick)="copy(row.target)"
                      />
                    </td>
                  </tr>
                </ng-template>
                <ng-template pTemplate="emptymessage">
                  <tr>
                    <td colspan="4" class="empty">
                      No entries match “{{ filterText() }}”.
                    </td>
                  </tr>
                </ng-template>
              </p-table>
            } @else {
              <div class="xref-empty xref-empty--small">
                Select a table from the list to view its entries.
              </div>
            }
          </section>
        </div>
      }
    </p-dialog>
  `,
  styles: [
    `
      .xref-banner {
        display: flex;
        align-items: center;
        gap: 10px;
        background: #eef2ff;
        border-bottom: 1px solid #c7d2fe;
        color: #3730a3;
        padding: 10px 18px;
        font-size: var(--tf-text-body);
      }
      .xref-banner code {
        background: rgba(67, 56, 202, 0.12);
        padding: 1px 6px;
        border-radius: 4px;
        font-size: 0.9em;
      }
      .xref-banner__link {
        margin-left: auto;
        display: inline-flex;
        align-items: center;
        gap: 4px;
        color: #3730a3;
        text-decoration: none;
        font-weight: 600;
        font-size: var(--tf-text-meta);
        white-space: nowrap;
      }
      .xref-banner__link:hover {
        text-decoration: underline;
      }

      .xref-loading,
      .xref-empty {
        padding: 48px 24px;
        text-align: center;
        color: var(--tf-text-muted);
      }
      .xref-empty .pi {
        font-size: 32px;
        margin-bottom: 8px;
        display: block;
      }
      .xref-empty h4 {
        margin: 4px 0 6px 0;
        color: var(--tf-text-strong);
      }
      .xref-empty--small {
        padding: 80px 24px;
        font-style: italic;
      }

      .xref-layout {
        display: grid;
        grid-template-columns: 280px 1fr;
        min-height: 460px;
      }

      .xref-rail {
        border-right: 1px solid var(--tf-slate-300);
        background: var(--tf-slate-50);
        display: flex;
        flex-direction: column;
      }
      .xref-rail__head {
        padding: 10px 14px;
        border-bottom: 1px solid var(--tf-slate-300);
        font-size: var(--tf-text-meta);
        color: var(--tf-text-muted);
        text-transform: uppercase;
        font-weight: 700;
        letter-spacing: 0.4px;
      }
      .xref-list {
        list-style: none;
        margin: 0;
        padding: 6px;
        overflow-y: auto;
      }
      .xref-list__item {
        width: 100%;
        display: flex;
        flex-direction: column;
        gap: 4px;
        background: transparent;
        border: 1px solid transparent;
        border-radius: var(--tf-radius-sm);
        padding: 8px 10px;
        cursor: pointer;
        text-align: left;
        font-family: inherit;
        color: var(--tf-text-strong);
      }
      .xref-list__item:hover {
        background: white;
        border-color: var(--tf-slate-300);
      }
      .xref-list__item--active {
        background: white;
        border-color: var(--tf-blue-400);
        box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.12);
      }
      .xref-list__name {
        font-weight: 600;
        font-size: var(--tf-text-body);
      }
      .xref-list__field,
      .xref-list__count {
        font-size: var(--tf-text-meta);
        color: var(--tf-text-muted);
      }
      .xref-list__field .pi {
        font-size: 10px;
        margin-right: 4px;
      }

      .xref-detail {
        padding: 16px 20px 20px 20px;
        display: flex;
        flex-direction: column;
        gap: 12px;
        min-width: 0;
      }
      .xref-detail__head {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 16px;
      }
      .xref-detail__head h3 {
        margin: 0 0 4px 0;
        font-size: var(--tf-text-heading);
      }
      .xref-detail__head .muted {
        margin: 0;
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
      }
      .xref-detail__meta {
        display: flex;
        flex-direction: column;
        gap: 6px;
        align-items: flex-end;
      }

      .xref-detail__search {
        display: flex;
        align-items: center;
        gap: 12px;
      }
      .xref-detail__search .p-input-icon-left {
        position: relative;
        flex: 1;
      }
      .xref-detail__search .p-input-icon-left .pi {
        position: absolute;
        left: 10px;
        top: 50%;
        transform: translateY(-50%);
        color: var(--tf-text-muted);
        z-index: 1;
      }
      .xref-detail__search input {
        width: 100%;
        padding-left: 32px;
      }
      .xref-detail__hint {
        font-size: var(--tf-text-meta);
        color: var(--tf-text-muted);
        white-space: nowrap;
      }

      .xref-code {
        font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
        background: var(--tf-slate-100);
        padding: 2px 8px;
        border-radius: 4px;
        font-size: 0.9em;
        color: var(--tf-text-strong);
      }
      .xref-arrow {
        text-align: center;
        color: var(--tf-text-muted);
      }
      .xref-value {
        font-weight: 500;
      }
      .xref-copy {
        text-align: right;
      }
      .empty {
        text-align: center;
        color: var(--tf-text-muted);
        font-style: italic;
        padding: var(--tf-space-4);
      }
    `,
  ],
})
export class ReferenceTablesDialogComponent implements OnChanges {
  /** Two-way binding for dialog visibility. */
  @Input() visible = false;
  @Output() visibleChange = new EventEmitter<boolean>();

  /** Drives which lookup tables match this deployment. */
  @Input() connectionId: string | null = null;
  @Input() tmsName: string | null = null;

  private api = inject(ApiService);
  private gen = inject(GeneralService);

  /** Deep-link for "Edit in Library" — opens the lookup-tables editor. */
  readonly libraryUrl = '/library/lookup-tables';

  loading = signal<boolean>(false);
  tables = signal<LookupTable[]>([]);
  selectedId = signal<string | null>(null);
  filterText = signal<string>('');

  selected = computed<LookupTable | null>(() => {
    const id = this.selectedId();
    if (!id) return null;
    return this.tables().find((t) => t.id === id) ?? null;
  });

  /** Decoded rows of the selected table's JSON `mappings` blob. */
  rows = computed<XrefRow[]>(() => {
    const t = this.selected();
    if (!t?.mappings) return [];
    try {
      const obj = JSON.parse(t.mappings) as Record<string, string>;
      return Object.entries(obj)
        .map(([source, target]) => ({ source, target: String(target) }))
        .sort((a, b) => a.source.localeCompare(b.source));
    } catch {
      return [];
    }
  });

  filteredRows = computed<XrefRow[]>(() => {
    const q = this.filterText().trim().toLowerCase();
    const all = this.rows();
    if (!q) return all;
    return all.filter(
      (r) => r.source.toLowerCase().includes(q) || r.target.toLowerCase().includes(q),
    );
  });

  entryCount(t: LookupTable): number {
    if (!t?.mappings) return 0;
    try {
      return Object.keys(JSON.parse(t.mappings) as Record<string, string>).length;
    } catch {
      return 0;
    }
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['visible'] && this.visible) {
      this.load();
    }
  }

  private load() {
    this.loading.set(true);
    this.filterText.set('');
    this.api.getLookupTables().subscribe({
      next: (res) => {
        const all = res.success && res.data?.lookupTables ? res.data.lookupTables : [];
        const keys = new Set<string>();
        if (this.connectionId) keys.add(this.connectionId);
        if (this.tmsName) keys.add(this.tmsName);
        const matched = keys.size
          ? all.filter((t) => keys.has(t.tmsSystemId))
          : all;
        this.tables.set(matched);
        // Auto-select the first table so the user sees content immediately.
        this.selectedId.set(matched[0]?.id ?? null);
        this.loading.set(false);
      },
      error: () => {
        this.tables.set([]);
        this.selectedId.set(null);
        this.loading.set(false);
      },
    });
  }

  async copy(value: string) {
    try {
      await navigator.clipboard.writeText(value);
      this.gen.success(`Copied “${value}” to clipboard.`);
    } catch {
      this.gen.error('Could not copy to clipboard.');
    }
  }
}

import {
  Component,
  ContentChild,
  EventEmitter,
  Input,
  Output,
  TemplateRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';

/**
 * Column metadata for `<app-data-table>`.
 *
 * Drives sortable headers + width hints. Cells themselves are rendered by the
 * `#row` template the caller projects — this metadata is only for the header
 * strip and (optionally) the sort field key.
 */
export interface DataTableColumn {
  /** Object key used for sorting. Pass an empty string for non-data columns (actions, selection). */
  field: string;
  /** Header label shown in the `<th>`. */
  header: string;
  /** CSS width hint (e.g. "11rem", "120px"). Optional. */
  width?: string;
  /** Default true — set false to disable sorting on this column. */
  sortable?: boolean;
  /** Text alignment for the header cell. */
  align?: 'left' | 'center' | 'right';
}

/**
 * Unified data table — every list-of-things view in the app should use this.
 *
 * Wraps `p-table` with our standard styling, density, paginator, loading
 * skeleton, and empty state. Callers project a `#row` template for the body.
 *
 *   <app-data-table [rows]="systems" [columns]="cols" [loading]="loading">
 *     <ng-template #row let-r>
 *       <tr>
 *         <td>{{ r.name }}</td>
 *         …
 *         <td><app-row-actions [items]="menuFor(r)" /></td>
 *       </tr>
 *     </ng-template>
 *   </app-data-table>
 */
@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, SkeletonModule],
  template: `
    <div class="dt">
      <p-table
        [value]="rows ?? []"
        [dataKey]="dataKey || undefined"
        [loading]="false"
        [paginator]="showPaginator()"
        [rows]="pageSize"
        [rowsPerPageOptions]="rowsPerPageOptions"
        [showCurrentPageReport]="true"
        currentPageReportTemplate="{first}–{last} of {totalRecords}"
        [globalFilterFields]="globalFilterFields"
        [sortField]="defaultSortField"
        [sortOrder]="defaultSortOrder"
        styleClass="p-datatable-sm dt__table"
      >
        <ng-template pTemplate="header">
          <tr>
            @for (col of columns; track col.field || col.header) {
              @if (col.sortable === false || !col.field) {
                <th [style.width]="col.width" [style.text-align]="col.align ?? 'left'">
                  {{ col.header }}
                </th>
              } @else {
                <!--
                  PrimeNG 21's pSortableColumn renders its own sort indicator,
                  so we deliberately omit <p-sortIcon>; using both stacks two
                  arrows side-by-side.
                -->
                <th
                  [pSortableColumn]="col.field"
                  [style.width]="col.width"
                  [style.text-align]="col.align ?? 'left'"
                >
                  {{ col.header }}
                </th>
              }
            }
          </tr>
        </ng-template>

        <ng-template pTemplate="body" let-r let-i="rowIndex">
          @if (loading) {
            <!-- consumed by loading branch below; no row output here -->
          } @else {
            <ng-container
              *ngTemplateOutlet="rowTpl; context: { $implicit: r, index: i }"
            ></ng-container>
          }
        </ng-template>

        <ng-template pTemplate="emptymessage">
          @if (loading) {
            <!-- skeleton handled outside the empty branch -->
            <tr>
              <td [attr.colspan]="columns.length">
                <div class="dt__skeleton">
                  @for (s of skeletonRows; track $index) {
                    <p-skeleton width="100%" height="2rem"></p-skeleton>
                  }
                </div>
              </td>
            </tr>
          } @else {
            <tr>
              <td [attr.colspan]="columns.length">
                <div class="dt__empty">
                  <i class="pi {{ emptyIcon }} dt__empty-icon"></i>
                  <h3>{{ emptyHeading }}</h3>
                  @if (emptyMessage) {
                    <p>{{ emptyMessage }}</p>
                  }
                  @if (showClearFilters) {
                    <button
                      pButton
                      type="button"
                      label="Clear filters"
                      icon="pi pi-filter-slash"
                      [text]="true"
                      size="small"
                      (click)="clearFilters.emit()"
                    ></button>
                  }
                </div>
              </td>
            </tr>
          }
        </ng-template>
      </p-table>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .dt {
        background: #ffffff;
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        overflow: hidden;
      }
      /* Soft-gray header, consistent with Customers tab. */
      :host ::ng-deep .dt__table .p-datatable-thead > tr > th {
        background: #f8fafc;
        color: #475569;
        font-weight: 600;
        font-size: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        border-bottom: 1px solid #e2e8f0;
        padding: 0.65rem 0.85rem;
      }
      :host ::ng-deep .dt__table .p-datatable-tbody > tr > td {
        padding: 0.7rem 0.85rem;
        border-bottom: 1px solid #f1f5f9;
        font-size: 0.875rem;
        color: #0f172a;
        vertical-align: middle;
      }
      :host ::ng-deep .dt__table .p-datatable-tbody > tr:last-child > td {
        border-bottom: none;
      }
      :host ::ng-deep .dt__table .p-datatable-tbody > tr:hover {
        background: #f8fafc;
      }
      :host ::ng-deep .dt__table .p-sortable-column:hover {
        background: #f1f5f9;
      }
      :host ::ng-deep .dt__table .p-datatable-thead > tr > th.p-sortable-column-active {
        color: #1e3a8a;
      }
      :host ::ng-deep .dt__table .p-paginator {
        background: #fafafa;
        border-top: 1px solid #e2e8f0;
        padding: 0.5rem 0.85rem;
      }
      .dt__skeleton {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        padding: 0.5rem 0;
      }
      .dt__empty {
        text-align: center;
        padding: 3rem 1rem;
        color: #64748b;
      }
      .dt__empty-icon {
        font-size: 2rem;
        color: #cbd5e1;
        margin-bottom: 0.75rem;
        display: block;
      }
      .dt__empty h3 {
        font-size: 1rem;
        font-weight: 600;
        color: #334155;
        margin: 0 0 0.25rem;
      }
      .dt__empty p {
        margin: 0 0 1rem;
        font-size: 0.875rem;
      }
    `,
  ],
})
export class DataTableComponent<T = unknown> {
  /** The rows. */
  @Input() rows: T[] = [];

  /** Column metadata; drives sortable headers. */
  @Input({ required: true }) columns: DataTableColumn[] = [];

  /** Show a loading skeleton instead of rows. */
  @Input() loading = false;

  /** Per-row identifier for selection + tracking. Optional. */
  @Input() dataKey = '';

  /** Default page size. */
  @Input() pageSize = 25;

  /** Per-page selector. */
  @Input() rowsPerPageOptions: number[] = [10, 25, 50];

  /** Initial sort. */
  @Input() defaultSortField = '';
  @Input() defaultSortOrder: 1 | -1 = 1;

  /** Fields searched by p-table's globalFilter (when caller wires it). */
  @Input() globalFilterFields: string[] = [];

  // Empty state customization
  @Input() emptyIcon = 'pi-inbox';
  @Input() emptyHeading = 'No results';
  @Input() emptyMessage = '';
  @Input() showClearFilters = false;

  /** Emitted when the empty-state "Clear filters" button is clicked. */
  @Output() clearFilters = new EventEmitter<void>();

  /** Body row template projected by the caller. */
  @ContentChild('row', { read: TemplateRef, static: false })
  rowTpl!: TemplateRef<unknown>;

  /** Auto-hide the paginator when total fits one page. */
  showPaginator(): boolean {
    return (this.rows?.length ?? 0) > this.pageSize;
  }

  /** Loading skeleton placeholder rows. */
  skeletonRows = Array.from({ length: 5 });
}

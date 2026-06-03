import { Component, OnInit, Pipe, PipeTransform } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { ApiService } from '../../services/api.service';
import { TransformationLogSummary, TransformationLogDetail } from '../../models/transformation-log.model';
import { TransformationStatus, StatusBadgeClass } from '../../constants/transformation-status.constants';
import { DataTableComponent, DataTableColumn } from '../../design-system/data-table.component';
import { StatusTagComponent } from '../../design-system/status-tag.component';
import { SectionHeaderComponent } from '../../design-system/section-header.component';

@Pipe({ name: 'prettyJson', standalone: true })
export class PrettyJsonPipe implements PipeTransform {
  transform(value: string | undefined): string {
    if (!value) return '';
    try {
      return JSON.stringify(JSON.parse(value), null, 2);
    } catch {
      return value;
    }
  }
}

@Component({
    selector: 'app-transformation-logs',
    imports: [
      CommonModule,
      FormsModule,
      ButtonModule,
      PrettyJsonPipe,
      DataTableComponent,
      StatusTagComponent,
      SectionHeaderComponent,
    ],
    templateUrl: './transformation-logs.component.html',
    styleUrl: './transformation-logs.component.scss'
})
export class TransformationLogsComponent implements OnInit {
  /** Column metadata for the unified data table. */
  logColumns: DataTableColumn[] = [
    { field: '', header: '', sortable: false, width: '2.5rem' },
    { field: 'status', header: 'Status', width: '9rem' },
    { field: 'templateName', header: 'Template' },
    { field: 'messageSummary', header: 'Message' },
    { field: 'correlationId', header: 'Correlation ID', width: '11rem' },
    { field: 'timestamp', header: 'Timestamp', width: '12rem' },
    { field: 'durationMs', header: 'Duration', width: '7rem', align: 'right' },
    { field: 'source', header: 'Source', width: '7rem' },
    { field: 'expiresAt', header: 'Expires', width: '8rem' },
  ];

  logs: TransformationLogSummary[] = [];
  detail: TransformationLogDetail | null = null;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  parsedErrors: any[] = [];
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  parsedWarnings: any[] = [];

  filterStatus = '';
  filterTemplateId = '';
  filterLimit = 100;
  filterFrom = '';
  filterTo = '';

  loading = false;
  detailLoading = false;
  error = '';
  detailError = '';
  selectedId: string | null = null;

  get total() {
    return this.logs.length;
  }
  get avgMs(): string {
    if (!this.logs.length) return '—';
    const avg = this.logs.reduce((s, l) => s + (l.durationMs ?? 0), 0) / this.logs.length;
    return avg.toFixed(0);
  }
  get counts(): Record<string, number> {
    const map: Record<string, number> = {};
    for (const l of this.logs) map[l.status] = (map[l.status] ?? 0) + 1;
    return map;
  }

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading = true;
    this.error = '';
    // Parse as local midnight / end-of-day so the filter aligns with the local timestamps shown in the UI,
    // then convert to UTC ISO strings for the backend (which stores timestamps as UTC).
    const fromParam = this.filterFrom ? new Date(`${this.filterFrom}T00:00:00`).toISOString() : undefined;
    const toParam = this.filterTo ? new Date(`${this.filterTo}T23:59:59`).toISOString() : undefined;
    this.api
      .getTransformationLogs(
        this.filterTemplateId || undefined,
        this.filterStatus || undefined,
        this.filterLimit,
        fromParam,
        toParam,
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.logs = res.data.logs;
          }
          this.loading = false;
        },
        error: (err) => {
          this.error = 'Failed to load transformation logs';
          console.error(err);
          this.loading = false;
        },
      });
  }

  toggleDetail(log: TransformationLogSummary) {
    if (this.selectedId === log.id) {
      this.selectedId = null;
      this.detail = null;
      this.parsedErrors = [];
      this.parsedWarnings = [];
      return;
    }
    this.selectedId = log.id;
    this.detail = null;
    this.parsedErrors = [];
    this.parsedWarnings = [];
    this.detailError = '';
    this.detailLoading = true;

    this.api.getTransformationLogById(log.id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.detail = res.data;
          this.parsedErrors = this.parseJson(res.data.errors);
          this.parsedWarnings = this.parseJson(res.data.warnings);
        }
        this.detailLoading = false;
      },
      error: (err) => {
        this.detailError = 'Failed to load log detail';
        console.error(err);
        this.detailLoading = false;
      },
    });
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private parseJson(json: string | undefined): any[] {
    if (!json) return [];
    try {
      return JSON.parse(json);
    } catch {
      return [];
    }
  }

  statusClass(status: string): Record<string, boolean> {
    return Object.fromEntries(Object.entries(StatusBadgeClass).map(([s, cls]) => [cls, status === s]));
  }

  statusLabel(status: string): string {
    return status === TransformationStatus.PartialSuccess ? 'Partial' : status;
  }
}

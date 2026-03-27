import { Component, OnInit, Pipe, PipeTransform } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../services/api.service';
import { TransformationLogSummary, TransformationLogDetail } from '../models/transformation-log.model';

@Pipe({ name: 'prettyJson', standalone: true })
export class PrettyJsonPipe implements PipeTransform {
  transform(value: string | undefined): string {
    if (!value) return '';
    try { return JSON.stringify(JSON.parse(value), null, 2); }
    catch { return value; }
  }
}

@Component({
  selector: 'app-transformation-logs',
  standalone: true,
  imports: [CommonModule, FormsModule, PrettyJsonPipe],
  template: `
    <div class="container">
      <div class="page-header">
        <h2>Transformation History</h2>
        <button class="btn-refresh" (click)="load()" [class.spinning]="loading">
          &#8635; Refresh
        </button>
      </div>

      <!-- Filters -->
      <div class="filter-bar">
        <div class="filter-group">
          <label>Status</label>
          <select [(ngModel)]="filterStatus" (change)="load()">
            <option value="">All</option>
            <option value="Success">Success</option>
            <option value="Warning">Warning</option>
            <option value="PartialSuccess">Partial Success</option>
            <option value="Error">Error</option>
          </select>
        </div>
        <div class="filter-group">
          <label>Template</label>
          <input type="text" [(ngModel)]="filterTemplateId" placeholder="Filter by template ID…"
                 (keyup.enter)="load()" />
        </div>
        <div class="filter-group">
          <label>Limit</label>
          <select [(ngModel)]="filterLimit" (change)="load()">
            <option [ngValue]="25">25</option>
            <option [ngValue]="50">50</option>
            <option [ngValue]="100">100</option>
            <option [ngValue]="250">250</option>
          </select>
        </div>
      </div>

      <!-- Summary cards -->
      <div class="stat-cards">
        <div class="stat-card">
          <span class="stat-num">{{ total }}</span>
          <span class="stat-label">Total</span>
        </div>
        <div class="stat-card success">
          <span class="stat-num">{{ counts['Success'] || 0 }}</span>
          <span class="stat-label">Success</span>
        </div>
        <div class="stat-card warning">
          <span class="stat-num">{{ counts['Warning'] || 0 }}</span>
          <span class="stat-label">Warning</span>
        </div>
        <div class="stat-card partial">
          <span class="stat-num">{{ counts['PartialSuccess'] || 0 }}</span>
          <span class="stat-label">Partial</span>
        </div>
        <div class="stat-card error">
          <span class="stat-num">{{ counts['Error'] || 0 }}</span>
          <span class="stat-label">Error</span>
        </div>
        <div class="stat-card neutral">
          <span class="stat-num">{{ avgMs }}</span>
          <span class="stat-label">Avg ms</span>
        </div>
      </div>

      <div *ngIf="error" class="alert alert-error">{{ error }}</div>

      <!-- Grid -->
      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th></th>
              <th>Status</th>
              <th>Template</th>
              <th>Message</th>
              <th>Correlation ID</th>
              <th>Timestamp</th>
              <th>Duration</th>
              <th>Source</th>
              <th>Expires</th>
            </tr>
          </thead>
          <tbody>
            <ng-container *ngFor="let log of logs">
              <tr class="summary-row" (click)="toggleDetail(log)" [class.expanded]="selectedId === log.id">
                <td class="expand-cell">
                  <span class="expand-icon">{{ selectedId === log.id ? '▼' : '▶' }}</span>
                </td>
                <td>
                  <span class="badge" [ngClass]="statusClass(log.status)">{{ statusLabel(log.status) }}</span>
                </td>
                <td class="template-id-cell">
                  {{ log.templateName || log.templateId }}
                </td>
                <td class="message-cell">{{ log.messageSummary || '—' }}</td>
                <td class="correlation-cell"><code>{{ log.correlationId || '—' }}</code></td>
                <td>{{ log.timestamp | date:'MMM d, y HH:mm:ss' }}</td>
                <td class="ms-cell">{{ log.durationMs }} ms</td>
                <td>{{ log.source || '—' }}</td>
                <td class="muted">{{ log.expiresAt | date:'mediumDate' }}</td>
              </tr>

              <!-- Expanded detail row -->
              <tr *ngIf="selectedId === log.id" class="detail-row">
                <td colspan="9">
                  <div class="detail-panel" *ngIf="!detailLoading">
                    <div *ngIf="detailError" class="alert alert-error">{{ detailError }}</div>

                    <div class="detail-grid" *ngIf="detail">
                      <!-- Errors panel -->
                      <div *ngIf="parsedErrors.length > 0" class="detail-section error-section">
                        <div class="section-title error-title">Errors ({{ parsedErrors.length }})</div>
                        <div class="error-list">
                          <div *ngFor="let e of parsedErrors" class="error-item">
                            <strong>{{ e.errorCode }}</strong>
                            <span *ngIf="e.sourcePath" class="path-chip">{{ e.sourcePath }} → {{ e.fieldPath }}</span>
                            <span class="error-msg">{{ e.message }}</span>
                          </div>
                        </div>
                      </div>

                      <!-- Input / Output JSON -->
                      <div class="detail-section">
                        <div class="section-title">Input JSON</div>
                        <pre class="json-block">{{ detail.inputData | prettyJson }}</pre>
                      </div>
                      <div class="detail-section">
                        <div class="section-title">Output JSON</div>
                        <pre class="json-block" *ngIf="detail.outputData">{{ detail.outputData | prettyJson }}</pre>
                        <span *ngIf="!detail.outputData" class="muted">No output produced</span>
                      </div>
                    </div>
                  </div>
                  <div *ngIf="detailLoading" class="loading-detail">Loading…</div>
                </td>
              </tr>
            </ng-container>

            <tr *ngIf="logs.length === 0 && !loading">
              <td colspan="9" class="no-data">
                No transformation logs found. Run a transformation on the "Test Transform" tab to generate logs.
              </td>
            </tr>
            <tr *ngIf="loading">
              <td colspan="9" class="no-data">Loading…</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  `,
  styles: [`
    .container { max-width: 1400px; margin: 0 auto; padding: 20px; }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    h2 { color: #2c3e50; margin: 0; }

    .btn-refresh {
      background: #3498db;
      color: white;
      border: none;
      padding: 8px 18px;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
    }
    .btn-refresh:hover { background: #2980b9; }
    .btn-refresh.spinning { opacity: 0.7; pointer-events: none; }

    /* Filters */
    .filter-bar {
      display: flex;
      gap: 20px;
      align-items: flex-end;
      margin-bottom: 20px;
      flex-wrap: wrap;
    }
    .filter-group {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .filter-group label { font-size: 12px; font-weight: 600; color: #777; text-transform: uppercase; }
    .filter-group select,
    .filter-group input {
      padding: 7px 10px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
      min-width: 140px;
    }

    /* Stats */
    .stat-cards {
      display: flex;
      gap: 12px;
      margin-bottom: 24px;
      flex-wrap: wrap;
    }
    .stat-card {
      background: white;
      border: 1px solid #ddd;
      border-radius: 8px;
      padding: 14px 22px;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 4px;
      min-width: 90px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.06);
    }
    .stat-num  { font-size: 28px; font-weight: 700; color: #2c3e50; }
    .stat-label { font-size: 11px; font-weight: 600; text-transform: uppercase; color: #999; }
    .stat-card.success .stat-num { color: #27ae60; }
    .stat-card.warning .stat-num { color: #e67e22; }
    .stat-card.partial .stat-num { color: #8e44ad; }
    .stat-card.error   .stat-num { color: #e74c3c; }
    .stat-card.neutral .stat-num { color: #3498db; }

    /* Alert */
    .alert { padding: 12px 16px; border-radius: 4px; margin-bottom: 16px; font-size: 14px; }
    .alert-error { background: #fee; color: #c33; border-left: 4px solid #e74c3c; }

    /* Table */
    .table-container {
      background: white;
      border-radius: 6px;
      border: 1px solid #ddd;
      overflow: auto;
      box-shadow: 0 2px 4px rgba(0,0,0,0.06);
    }
    table { width: 100%; border-collapse: collapse; font-size: 14px; }
    thead tr { background: #f8f9fa; }
    th, td { padding: 11px 14px; text-align: left; border-bottom: 1px solid #eee; }
    th { font-weight: 600; color: #555; font-size: 12px; text-transform: uppercase; letter-spacing: 0.4px; }

    .summary-row { cursor: pointer; transition: background 0.1s; }
    .summary-row:hover td { background: #f0f7ff; }
    .summary-row.expanded td { background: #e8f4fd; border-bottom: none; }

    .expand-cell { width: 32px; text-align: center; }
    .expand-icon { color: #3498db; font-size: 10px; }

    .template-id-cell {
      font-weight: 500;
      color: #2c3e50;
    }
    .message-cell {
      max-width: 260px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      color: #444;
    }
    .correlation-cell code {
      font-size: 11px;
      background: #f4f4f4;
      padding: 2px 6px;
      border-radius: 3px;
      color: #666;
    }
    .ms-cell { font-variant-numeric: tabular-nums; }
    .muted { color: #aaa; }
    .no-data { text-align: center; color: #999; font-style: italic; padding: 50px; }

    /* Status badges */
    .badge {
      display: inline-block;
      padding: 3px 10px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.3px;
    }
    .badge-success  { background: #d4edda; color: #155724; }
    .badge-warning  { background: #fff3cd; color: #856404; }
    .badge-partial  { background: #e9d5f5; color: #6a1a8a; }
    .badge-error    { background: #f8d7da; color: #721c24; }

    /* Detail panel */
    .detail-row td { padding: 0; border-bottom: 2px solid #3498db; }
    .detail-panel {
      padding: 20px 24px;
      background: #f8fbff;
    }
    .loading-detail {
      padding: 20px 24px;
      color: #999;
      font-style: italic;
    }
    .detail-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 20px;
    }
    @media (max-width: 1100px) {
      .detail-grid { grid-template-columns: 1fr; }
    }

    .detail-section {}
    .section-title {
      font-size: 12px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.4px;
      color: #555;
      margin-bottom: 8px;
    }
    .error-title { color: #c0392b; }

    .json-block {
      background: #1e1e1e;
      color: #d4d4d4;
      font-size: 12px;
      padding: 14px;
      border-radius: 5px;
      overflow: auto;
      max-height: 480px;
      margin: 0;
      white-space: pre;
      font-family: 'Consolas', 'Menlo', monospace;
      word-break: break-all;
    }

    /* Error list */
    .error-section {
      background: #fff5f5;
      border: 1px solid #f5c6cb;
      border-radius: 6px;
      padding: 14px;
    }
    .error-list { display: flex; flex-direction: column; gap: 8px; }
    .error-item {
      display: flex;
      align-items: baseline;
      gap: 10px;
      font-size: 13px;
      flex-wrap: wrap;
    }
    .error-item strong { color: #c0392b; font-size: 12px; white-space: nowrap; }
    .error-msg { color: #555; }
    .path-chip {
      background: #f8d7da;
      color: #721c24;
      font-size: 11px;
      padding: 1px 7px;
      border-radius: 10px;
      font-family: monospace;
      white-space: nowrap;
    }
  `]
})
export class TransformationLogsComponent implements OnInit {
  logs: TransformationLogSummary[] = [];
  detail: TransformationLogDetail | null = null;
  parsedErrors: any[] = [];

  filterStatus = '';
  filterTemplateId = '';
  filterLimit = 100;

  loading = false;
  detailLoading = false;
  error = '';
  detailError = '';
  selectedId: string | null = null;

  get total() { return this.logs.length; }
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

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.error = '';
    this.api.getTransformationLogs(
      this.filterTemplateId || undefined,
      this.filterStatus || undefined,
      this.filterLimit
    ).subscribe({
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
      }
    });
  }

  toggleDetail(log: TransformationLogSummary) {
    if (this.selectedId === log.id) {
      this.selectedId = null;
      this.detail = null;
      this.parsedErrors = [];
      return;
    }
    this.selectedId = log.id;
    this.detail = null;
    this.parsedErrors = [];
    this.detailError = '';
    this.detailLoading = true;

    this.api.getTransformationLogById(log.id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.detail = res.data;
          this.parsedErrors = this.parseErrors(res.data.errors);
        }
        this.detailLoading = false;
      },
      error: (err) => {
        this.detailError = 'Failed to load log detail';
        console.error(err);
        this.detailLoading = false;
      }
    });
  }

  private parseErrors(errorsJson: string | undefined): any[] {
    if (!errorsJson) return [];
    try { return JSON.parse(errorsJson); } catch { return []; }
  }

  statusClass(status: string): Record<string, boolean> {
    return {
      'badge-success': status === 'Success',
      'badge-warning': status === 'Warning',
      'badge-partial': status === 'PartialSuccess',
      'badge-error':   status === 'Error'
    };
  }

  statusLabel(status: string): string {
    return status === 'PartialSuccess' ? 'Partial' : status;
  }
}


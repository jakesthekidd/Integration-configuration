import { Component, Input, OnChanges, SimpleChanges, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';

import { ApiService } from '../services/api.service';
import { Deployment } from '../models/deployment.model';
import { TransformationLogSummary } from '../models/transformation-log.model';

/**
 * Activity tab — read-only stream of recent transformation logs for THIS deployment.
 *
 * Today the mock doesn't carry a `deploymentId` on logs, so we filter loosely by the
 * deployment's template id. When the backend is real, logs should reference deployment
 * directly.
 */
@Component({
  selector: 'app-activity-tab',
  imports: [CommonModule, TableModule, TagModule],
  template: `
    <div class="head">
      <h4>Recent runs</h4>
      <p class="muted">Last 50 transformations for this deployment.</p>
    </div>

    <p-table
      [value]="logs()"
      [loading]="loading()"
      [rowHover]="true"
      [paginator]="logs().length > 20"
      [rows]="20"
      styleClass="p-datatable-sm p-datatable-striped"
    >
      <ng-template pTemplate="header">
        <tr>
          <th>Time</th>
          <th>Status</th>
          <th>Records</th>
          <th>Duration</th>
          <th>Correlation</th>
          <th>Message</th>
        </tr>
      </ng-template>
      <ng-template pTemplate="body" let-row>
        <tr>
          <td>{{ row.timestamp | date: 'medium' }}</td>
          <td>
            <p-tag [value]="row.status" [severity]="severity(row.status)" [rounded]="true" />
          </td>
          <td>{{ row.recordCount ?? '—' }}</td>
          <td>{{ row.durationMs }} ms</td>
          <td class="mono">{{ row.correlationId ?? '—' }}</td>
          <td>{{ row.messageSummary ?? '' }}</td>
        </tr>
      </ng-template>
      <ng-template pTemplate="emptymessage">
        <tr>
          <td colspan="6" class="empty">No runs yet for this deployment.</td>
        </tr>
      </ng-template>
    </p-table>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .head {
        display: flex;
        flex-direction: column;
        gap: 4px;
        margin-bottom: var(--tf-space-3);
      }
      .head h4 {
        margin: 0;
        font-size: var(--tf-text-heading);
      }
      .muted {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin: 0;
      }
      .mono {
        font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
        font-size: var(--tf-text-meta);
        color: var(--tf-text-muted);
      }
      .empty {
        text-align: center;
        color: var(--tf-text-muted);
        font-style: italic;
        padding: var(--tf-space-6);
      }
    `,
  ],
})
export class ActivityTabComponent implements OnChanges {
  @Input({ required: true }) deployment!: Deployment;

  private api = inject(ApiService);
  logs = signal<TransformationLogSummary[]>([]);
  loading = signal<boolean>(false);

  ngOnChanges(changes: SimpleChanges) {
    if (changes['deployment']) this.load();
  }

  private load() {
    if (!this.deployment) return;
    this.loading.set(true);
    // Loose filter by template id since logs don't carry deploymentId in the mock.
    const tid = this.deployment.forkedFromTemplateId;
    this.api.getTransformationLogs(tid || undefined).subscribe((res) => {
      if (res.success && res.data) {
        this.logs.set(res.data.logs);
      } else {
        this.logs.set([]);
      }
      this.loading.set(false);
    });
  }

  severity(s: string): 'success' | 'info' | 'warn' | 'secondary' | 'danger' {
    if (s === 'Success') return 'success';
    if (s === 'Warning' || s === 'PartialSuccess') return 'warn';
    if (s === 'Error') return 'danger';
    return 'secondary';
  }
}

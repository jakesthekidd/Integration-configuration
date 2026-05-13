import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { MessageModule } from 'primeng/message';

import { ApiService } from '../../services/api.service';
import { TransformationTypes } from '../../models/field-mapping.model';
import { WizardStateService, FieldMappingDraft } from '../wizard-state.service';
import { TEMPLATE_FROM_SCRATCH } from './step-pick-template.component';

/**
 * Wizard step 7 — Customize the forked CustomerTemplate's field mappings.
 *
 * On first entry (or whenever the user picks a different master in step 6) the
 * master template's field mappings are forked into the wizard draft as
 * `fieldMappings`. Edits here are persisted per keystroke and never affect the
 * underlying master.
 *
 * This is intentionally a basic table editor for now — full schema-aware
 * mapping with sample-JSON test panel, drag-to-map source nodes, etc. comes
 * later (see PRODUCT-GUIDING-PRINCIPLES.md §10).
 */
@Component({
  selector: 'app-step-customize-mapping',
  imports: [
    FormsModule,
    TableModule,
    InputTextModule,
    SelectModule,
    CheckboxModule,
    ButtonModule,
    TagModule,
    TextareaModule,
    MessageModule,
  ],
  template: `
    <div class="header">
      <div class="header-text">
        <p class="intro">
          @if (state.draft().templateId === SCRATCH) {
            Build the field mappings for this customer's integration. Each row maps a
            source path to a target path with an optional transformation.
          } @else {
            Customize the mappings forked from the master template. Edits stay scoped
            to this customer and won't affect the master.
          }
        </p>
        <span class="count">
          {{ mappings().length }} {{ mappings().length === 1 ? 'mapping' : 'mappings' }}
        </span>
      </div>
      <p-button
        label="Add mapping"
        icon="pi pi-plus"
        size="small"
        severity="primary"
        (onClick)="addRow()"
      />
    </div>

    <p-table
      [value]="mappings()"
      [rowHover]="true"
      styleClass="p-datatable-sm p-datatable-striped mapping-table"
    >
      <ng-template pTemplate="header">
        <tr>
          <th style="width: 30%;">Source path</th>
          <th style="width: 30%;">Target path</th>
          <th style="width: 18%;">Transformation</th>
          <th style="width: 8%;">Required</th>
          <th style="width: 14%;">Default</th>
          <th style="width: 56px;"></th>
        </tr>
      </ng-template>
      <ng-template pTemplate="body" let-row let-i="rowIndex">
        <tr>
          <td>
            <input
              pInputText
              type="text"
              [ngModel]="row.sourcePath"
              (ngModelChange)="updateRow(i, { sourcePath: $event })"
              placeholder="$.order.id"
            />
          </td>
          <td>
            <input
              pInputText
              type="text"
              [ngModel]="row.targetPath"
              (ngModelChange)="updateRow(i, { targetPath: $event })"
              placeholder="order.id"
            />
          </td>
          <td>
            <p-select
              [options]="transformationOptions"
              [ngModel]="row.transformationType"
              (ngModelChange)="updateRow(i, { transformationType: $event })"
              appendTo="body"
            />
          </td>
          <td class="center">
            <p-checkbox
              [binary]="true"
              [ngModel]="row.isRequired"
              (ngModelChange)="updateRow(i, { isRequired: $event })"
            />
          </td>
          <td>
            <input
              pInputText
              type="text"
              [ngModel]="row.defaultValue"
              (ngModelChange)="updateRow(i, { defaultValue: $event })"
              placeholder="—"
            />
          </td>
          <td class="actions">
            <p-button
              icon="pi pi-trash"
              size="small"
              severity="danger"
              [text]="true"
              [rounded]="true"
              aria-label="Remove mapping"
              (onClick)="removeRow(i)"
            />
          </td>
        </tr>
      </ng-template>
      <ng-template pTemplate="emptymessage">
        <tr>
          <td colspan="6" class="empty">
            No mappings yet. Click <strong>+ Add mapping</strong> to start.
          </td>
        </tr>
      </ng-template>
    </p-table>

    <!-- ── Test panel ──────────────────────────────────────────────── -->
    <section class="test-panel" [class.test-panel--open]="testOpen()">
      <button type="button" class="test-panel__head" (click)="testOpen.set(!testOpen())">
        <i class="pi" [class.pi-chevron-right]="!testOpen()" [class.pi-chevron-down]="testOpen()" aria-hidden="true"></i>
        <span class="test-panel__title">Test with sample JSON</span>
        <span class="test-panel__hint">Paste a sample order and run the mapping locally.</span>
      </button>

      @if (testOpen()) {
        <div class="test-panel__body">
          <div class="test-panel__panes">
            <label class="pane">
              <span class="pane__label">Input</span>
              <textarea
                pTextarea
                rows="10"
                [ngModel]="sampleInput()"
                (ngModelChange)="sampleInput.set($event)"
                placeholder='{ "order": { "id": "A1001" } }'
                spellcheck="false"
              ></textarea>
            </label>
            <label class="pane">
              <span class="pane__label">Output</span>
              <textarea
                pTextarea
                rows="10"
                readonly
                [ngModel]="sampleOutput()"
                placeholder="Run the mapping to see output"
                spellcheck="false"
              ></textarea>
            </label>
          </div>

          <div class="test-panel__actions">
            <p-button
              label="Run mapping"
              icon="pi pi-play"
              size="small"
              severity="primary"
              [disabled]="!sampleInput().trim() || mappings().length === 0"
              (onClick)="runTest()"
            />
            <small class="muted">
              Local preview only. Real-data testing happens in step 8 with this customer's credentials.
            </small>
          </div>

          @if (parseError()) {
            <p-message severity="error" [text]="parseError()" />
          }
        </div>
      }
    </section>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .header {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: var(--tf-space-4);
        margin-bottom: var(--tf-space-4);
      }
      .header-text {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-1);
        flex: 1 1 auto;
      }
      .intro {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin: 0;
        max-width: 640px;
      }
      .count {
        font-size: var(--tf-text-meta);
        color: var(--tf-text-muted);
        font-weight: 600;
        letter-spacing: 0.4px;
        text-transform: uppercase;
      }
      .center {
        text-align: center;
      }
      .actions {
        text-align: right;
        padding-right: var(--tf-space-2) !important;
      }
      .empty {
        text-align: center;
        color: var(--tf-text-muted);
        font-style: italic;
        padding: var(--tf-space-6);
      }
      :host ::ng-deep .mapping-table input.p-inputtext,
      :host ::ng-deep .mapping-table .p-select {
        width: 100%;
      }

      /* ── Test panel ─────────────────────────────────────────── */
      .test-panel {
        margin-top: var(--tf-space-5);
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-md);
        background: white;
        overflow: hidden;
      }
      .test-panel__head {
        display: flex;
        align-items: center;
        gap: var(--tf-space-2);
        width: 100%;
        padding: var(--tf-space-3) var(--tf-space-4);
        background: var(--tf-slate-100);
        border: 0;
        cursor: pointer;
        font-family: inherit;
        text-align: left;
        color: var(--tf-text-strong);
      }
      .test-panel__head:hover {
        background: var(--tf-slate-200);
      }
      .test-panel__head .pi {
        font-size: 12px;
        color: var(--tf-text-muted);
      }
      .test-panel__title {
        font-weight: 700;
        font-size: var(--tf-text-body);
      }
      .test-panel__hint {
        margin-left: auto;
        font-size: var(--tf-text-meta);
        color: var(--tf-text-muted);
        font-weight: 500;
      }
      .test-panel__body {
        padding: var(--tf-space-4);
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-3);
      }
      .test-panel__panes {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: var(--tf-space-3);
      }
      .pane {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-1);
      }
      .pane__label {
        font-size: var(--tf-text-meta);
        font-weight: 700;
        color: var(--tf-text-muted);
        letter-spacing: 0.4px;
        text-transform: uppercase;
      }
      .pane textarea {
        font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
        font-size: var(--tf-text-meta);
        line-height: 1.5;
        width: 100%;
        resize: vertical;
      }
      .test-panel__actions {
        display: flex;
        align-items: center;
        gap: var(--tf-space-3);
      }
      .test-panel__actions .muted {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-meta);
      }
    `,
  ],
})
export class StepCustomizeMappingComponent implements OnInit {
  protected state = inject(WizardStateService);
  private api = inject(ApiService);

  protected readonly SCRATCH = TEMPLATE_FROM_SCRATCH;
  protected readonly transformationOptions = TransformationTypes.map((t) => ({ label: t, value: t }));

  mappings = computed<FieldMappingDraft[]>(() => this.state.draft().fieldMappings);

  // ─── Test panel state ─────────────────────────────────────────────
  testOpen = signal<boolean>(false);
  sampleInput = signal<string>('');
  sampleOutput = signal<string>('');
  parseError = signal<string>('');

  ngOnInit() {
    this.forkIfNeeded();
  }

  /**
   * Placeholder mapping runner. Validates that the input parses as JSON, then
   * shows a "wired-up-soon" stub in the output pane. Real in-browser evaluation
   * (Direct + Constant + Concat + DateFormat etc.) lands when the backend is real.
   */
  runTest() {
    this.parseError.set('');
    this.sampleOutput.set('');
    let parsed: unknown;
    try {
      parsed = JSON.parse(this.sampleInput());
    } catch (err) {
      this.parseError.set('Input is not valid JSON: ' + (err as Error).message);
      return;
    }
    const preview = {
      __status: 'Local preview unavailable',
      __note:
        'Mapping evaluation runs server-side. ' +
        'Wire this panel to /api/v1/templates/:id/preview when the backend lands.',
      __mappingCount: this.mappings().length,
      __inputParsed: parsed,
    };
    this.sampleOutput.set(JSON.stringify(preview, null, 2));
  }

  /**
   * Fork the master template's mappings into the draft if:
   *   - the user picked a real master template (not scratch)
   *   - AND we haven't already forked it (forkedFromTemplateId differs)
   * Scratch picks start with an empty mappings array.
   */
  private forkIfNeeded() {
    const d = this.state.draft();
    if (!d.templateId) return;
    if (d.forkedFromTemplateId === d.templateId) return;

    if (d.templateId === TEMPLATE_FROM_SCRATCH) {
      this.state.patch({ fieldMappings: [], forkedFromTemplateId: TEMPLATE_FROM_SCRATCH });
      return;
    }

    this.api.getFieldMappings(d.templateId).subscribe((res) => {
      if (!res.success || !res.data) return;
      const forked: FieldMappingDraft[] = res.data.mappings.map((m) => ({
        id: cryptoRandomId(),
        sourcePath: m.sourcePath,
        targetPath: m.targetPath,
        transformationType: m.transformationType,
        isRequired: m.isRequired,
        defaultValue: m.defaultValue ?? '',
      }));
      this.state.patch({ fieldMappings: forked, forkedFromTemplateId: d.templateId });
    });
  }

  addRow() {
    const next: FieldMappingDraft = {
      id: cryptoRandomId(),
      sourcePath: '',
      targetPath: '',
      transformationType: 'Direct',
      isRequired: false,
      defaultValue: '',
    };
    this.state.patch({ fieldMappings: [...this.mappings(), next] });
  }

  updateRow(index: number, change: Partial<FieldMappingDraft>) {
    const list = [...this.mappings()];
    list[index] = { ...list[index], ...change };
    this.state.patch({ fieldMappings: list });
  }

  removeRow(index: number) {
    const list = this.mappings().filter((_, i) => i !== index);
    this.state.patch({ fieldMappings: list });
  }
}

function cryptoRandomId(): string {
  return 'fm-' + Math.random().toString(36).slice(2, 10);
}

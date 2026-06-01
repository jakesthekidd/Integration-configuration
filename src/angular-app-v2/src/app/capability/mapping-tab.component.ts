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
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { MessageModule } from 'primeng/message';
import { DialogModule } from 'primeng/dialog';

import { Router, ActivatedRoute } from '@angular/router';
import { ApiService } from '../services/api.service';
import { GeneralService } from '../services/general.service';
import { DraftService } from '../services/draft.service';
import { Deployment } from '../models/deployment.model';
import { FieldMappingTemplate } from '../models/template.model';
import { TransformationTypes } from '../models/field-mapping.model';

interface MappingRow {
  id: string;
  sourcePath: string;
  targetPath: string;
  transformationType: string;
  isRequired: boolean;
  defaultValue: string;
}

const SCRATCH_ID = '__scratch__';

/**
 * Mapping tab — fork or pick a MasterTemplate, then customize the field mappings
 * for THIS deployment. Edits don't propagate back to the master.
 *
 * Inline template picker is a `<p-dialog>` filtered to the current
 * (Application, Capability, Connection) per PRODUCT-GUIDING-PRINCIPLES.md §6.
 *
 * JSON test panel is local-preview-only — a real evaluator lives server-side.
 */
@Component({
  selector: 'app-mapping-tab',
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    InputTextModule,
    SelectModule,
    CheckboxModule,
    ButtonModule,
    TagModule,
    TextareaModule,
    MessageModule,
    DialogModule,
  ],
  template: `
    <!-- ── Viewing-archived banner (read-only snapshot mode) ───────── -->
    @if (viewingVersion(); as view) {
      <div class="view-version-banner">
        <i class="pi pi-eye"></i>
        <span>
          Viewing field mappings for <strong>{{ view.label }}</strong> — read-only snapshot.
        </span>
        <button type="button" class="view-version-banner__btn" (click)="returnToCurrent()">
          Return to current
        </button>
      </div>
    }

    <!-- ── Template header ─────────────────────────────────────────── -->
    <section class="block">
      <header class="block__head">
        <div>
          <h4>Mapping template</h4>
          <p class="muted">
            @if (forkedFromId() === SCRATCH) {
              Built from scratch. No master template.
            } @else if (templateName()) {
              Forked from <strong>{{ templateName() }}</strong>
              <span *ngIf="forkedVersion(); let v">· v{{ v }}</span>
            } @else {
              No template yet. Fork one to start.
            }
          </p>
        </div>
        <p-button
          [label]="forkedFromId() ? 'Change template' : 'Fork a master'"
          icon="pi pi-file-import"
          severity="secondary"
          [outlined]="true"
          size="small"
          (onClick)="openPicker()"
        />
      </header>
    </section>

    <!-- ── Field mappings ───────────────────────────────────────────── -->
    <section class="block">
      <header class="block__head">
        <div>
          <h4>Field mappings</h4>
          <p class="muted">{{ mappings().length }} {{ mappings().length === 1 ? 'mapping' : 'mappings' }}</p>
        </div>
        <p-button
          label="Add mapping"
          icon="pi pi-plus"
          size="small"
          severity="primary"
          (onClick)="addRow()"
        />
      </header>

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
              No mappings yet. Fork a template above, or click <strong>+ Add mapping</strong>.
            </td>
          </tr>
        </ng-template>
      </p-table>
    </section>

    <!-- ── Test panel ───────────────────────────────────────────────── -->
    <section class="block test-panel" [class.test-panel--open]="testOpen()">
      <button type="button" class="test-panel__head" (click)="testOpen.set(!testOpen())">
        <i
          class="pi"
          [class.pi-chevron-right]="!testOpen()"
          [class.pi-chevron-down]="testOpen()"
          aria-hidden="true"
        ></i>
        <span class="test-panel__title">Test with sample JSON</span>
        <span class="test-panel__hint">Paste a sample order and preview the mapping.</span>
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
                placeholder="Run the mapping to see preview output"
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
              Local preview. Real-data testing uses live credentials in Test &amp; Publish.
            </small>
          </div>
          @if (parseError()) {
            <p-message severity="error" [text]="parseError()" />
          }
        </div>
      }
    </section>

    <!-- ── Footer: save bar ────────────────────────────────────────── -->
    <footer class="actions">
      <span class="status-pill" [class.status-pill--draft]="!dirty()" [class.status-pill--ok]="dirty()">
        @if (dirty()) {
          <i class="pi pi-pencil"></i>
          Unsaved changes
        } @else {
          <i class="pi pi-check"></i>
          No unsaved changes
        }
      </span>
      <span class="grow"></span>
      <p-button
        label="Save changes"
        icon="pi pi-save"
        severity="primary"
        size="small"
        [disabled]="!dirty()"
        [loading]="saving()"
        (onClick)="save()"
      />
    </footer>

    <!-- ── Template picker dialog ──────────────────────────────────── -->
    <p-dialog
      [(visible)]="pickerOpen"
      [modal]="true"
      [closable]="true"
      [draggable]="false"
      [resizable]="false"
      [style]="{ width: '640px' }"
      [header]="pickerHeader()"
    >
      <p class="muted" *ngIf="filteredTemplates().length === 0">
        No published master templates match this (Application × Capability × Connection) yet.
        You can still start from scratch.
      </p>
      <ul class="picker">
        @for (t of filteredTemplates(); track t.id) {
          <li>
            <button class="picker-row" type="button" (click)="forkTemplate(t)">
              <span class="picker-row__title">{{ t.name }}</span>
              <span class="picker-row__meta">v{{ t.version }} · {{ t.status }}</span>
              <span class="picker-row__desc">{{ t.description ?? '' }}</span>
            </button>
          </li>
        }
        <li>
          <button class="picker-row picker-row--scratch" type="button" (click)="forkScratch()">
            <span class="picker-row__title">— Start from scratch —</span>
            <span class="picker-row__desc">Build mappings without inheriting from a master.</span>
          </button>
        </li>
      </ul>
    </p-dialog>
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-5);
      }

      .view-version-banner {
        display: flex;
        align-items: center;
        gap: 8px;
        background: #eef2ff;
        border: 1px solid #c7d2fe;
        color: #3730a3;
        padding: 8px 14px;
        border-radius: var(--tf-radius-md);
        font-size: var(--tf-text-body);
      }
      .view-version-banner__btn {
        margin-left: auto;
        background: none;
        border: 1px solid #3730a3;
        color: #3730a3;
        font-weight: 600;
        padding: 4px 12px;
        border-radius: var(--tf-radius-pill);
        cursor: pointer;
        font-size: var(--tf-text-meta);
      }
      .view-version-banner__btn:hover {
        background: #3730a3;
        color: #fff;
      }

      .block {
        background: white;
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-md);
        padding: var(--tf-space-4) var(--tf-space-5);
      }
      .block__head {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: var(--tf-space-3);
        margin-bottom: var(--tf-space-3);
      }
      .block__head h4 {
        margin: 0;
        font-size: var(--tf-text-heading);
      }
      .muted {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin: 4px 0 0 0;
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

      /* Test panel */
      .test-panel {
        padding: 0;
      }
      .test-panel__head {
        display: flex;
        align-items: center;
        gap: var(--tf-space-2);
        width: 100%;
        padding: var(--tf-space-3) var(--tf-space-4);
        background: var(--tf-slate-100);
        border: 0;
        border-radius: var(--tf-radius-md) var(--tf-radius-md) 0 0;
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

      /* Footer bar */
      .actions {
        display: flex;
        align-items: center;
        gap: var(--tf-space-3);
        padding: var(--tf-space-3) var(--tf-space-4);
        background: var(--tf-slate-100);
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-md);
        text-align: initial !important;
      }
      .grow {
        flex: 1 1 auto;
      }
      .status-pill {
        display: inline-flex;
        align-items: center;
        gap: var(--tf-space-1);
        font-size: var(--tf-text-meta);
        font-weight: 600;
        padding: 4px 10px;
        border-radius: var(--tf-radius-pill);
      }
      .status-pill--ok {
        background: #fff6e5;
        color: #92510a;
      }
      .status-pill--draft {
        background: #e5f9ea;
        color: #1b6b3a;
      }

      /* Picker */
      .picker {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-2);
      }
      .picker-row {
        width: 100%;
        background: white;
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-md);
        padding: var(--tf-space-3);
        cursor: pointer;
        display: grid;
        grid-template-columns: 1fr auto;
        gap: var(--tf-space-1) var(--tf-space-3);
        font-family: inherit;
        text-align: left;
      }
      .picker-row:hover {
        border-color: var(--tf-blue-400);
        background: var(--tf-blue-50);
      }
      .picker-row__title {
        font-weight: 700;
        font-size: var(--tf-text-body);
        color: var(--tf-text-strong);
      }
      .picker-row__meta {
        font-size: var(--tf-text-meta);
        color: var(--tf-text-muted);
        font-weight: 600;
      }
      .picker-row__desc {
        grid-column: 1 / -1;
        color: var(--tf-text-muted);
        font-size: var(--tf-text-meta);
      }
      .picker-row--scratch {
        border-style: dashed;
      }
    `,
  ],
})
export class MappingTabComponent implements OnChanges {
  @Input({ required: true }) deployment!: Deployment;
  @Output() saved = new EventEmitter<void>();

  protected readonly SCRATCH = SCRATCH_ID;
  protected readonly transformationOptions = TransformationTypes.map((t) => ({ label: t, value: t }));

  private api = inject(ApiService);
  private gen = inject(GeneralService);
  private draftService = inject(DraftService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  /** When set, this tab is rendering a snapshot of an archived/published version. */
  viewingVersion = computed(() => this.draftService.viewVersion(this.deployment?.id ?? ''));

  returnToCurrent() {
    if (!this.deployment) return;
    this.draftService.setViewVersion(this.deployment.id, null);
  }

  mappings = signal<MappingRow[]>([]);
  forkedFromId = signal<string>('');
  forkedVersion = signal<number | null>(null);
  templateName = signal<string>('');

  private snapshot = signal<{
    forkedFromId: string;
    forkedVersion: number | null;
    mappings: MappingRow[];
  }>({ forkedFromId: '', forkedVersion: null, mappings: [] });

  saving = signal<boolean>(false);

  testOpen = signal<boolean>(false);
  sampleInput = signal<string>('');
  sampleOutput = signal<string>('');
  parseError = signal<string>('');

  pickerOpen = false;
  allTemplates = signal<FieldMappingTemplate[]>([]);

  /** Templates filtered to the deployment's (App, Capability, Connection).
   *  The mock template model doesn't carry these fields yet, so we show all
   *  Published templates as the pool. When backend lands this filter becomes real. */
  filteredTemplates = computed<FieldMappingTemplate[]>(() =>
    this.allTemplates().filter((t) => (t.latestVersionStatus ?? t.status) === 'Published'),
  );

  pickerHeader = computed(() => `Templates for this deployment`);

  dirty = computed<boolean>(() => {
    const s = this.snapshot();
    if (s.forkedFromId !== this.forkedFromId()) return true;
    if (s.forkedVersion !== this.forkedVersion()) return true;
    const a = s.mappings;
    const b = this.mappings();
    if (a.length !== b.length) return true;
    for (let i = 0; i < a.length; i++) {
      const x = a[i];
      const y = b[i];
      if (
        x.sourcePath !== y.sourcePath ||
        x.targetPath !== y.targetPath ||
        x.transformationType !== y.transformationType ||
        x.isRequired !== y.isRequired ||
        x.defaultValue !== y.defaultValue
      ) {
        return true;
      }
    }
    return false;
  });

  ngOnChanges(changes: SimpleChanges) {
    if (changes['deployment']) this.load();
  }

  private load() {
    if (!this.deployment) return;
    this.forkedFromId.set(this.deployment.forkedFromTemplateId || '');
    this.forkedVersion.set(this.deployment.forkedFromTemplateVersion);

    // Load all templates for the picker.
    this.api.getTemplates().subscribe((res) => {
      if (res.success && res.data) {
        this.allTemplates.set(res.data.templates);
        // Resolve display name of currently-forked master, if any.
        const master = res.data.templates.find((t) => t.id === this.forkedFromId());
        this.templateName.set(master?.name ?? '');
      }
    });

    // Load the field mappings (in mock these come keyed by template id).
    if (this.deployment.forkedFromTemplateId) {
      this.api.getFieldMappings(this.deployment.forkedFromTemplateId).subscribe((res) => {
        if (res.success && res.data) {
          const rows: MappingRow[] = res.data.mappings.map((m) => ({
            id: m.id,
            sourcePath: m.sourcePath,
            targetPath: m.targetPath,
            transformationType: m.transformationType,
            isRequired: m.isRequired,
            defaultValue: m.defaultValue ?? '',
          }));
          this.mappings.set(rows);
          this.snapshot.set({
            forkedFromId: this.forkedFromId(),
            forkedVersion: this.forkedVersion(),
            mappings: rows.map((r) => ({ ...r })),
          });
        }
      });
    } else {
      this.mappings.set([]);
      this.snapshot.set({
        forkedFromId: this.forkedFromId(),
        forkedVersion: this.forkedVersion(),
        mappings: [],
      });
    }
  }

  // ─── Picker ─────────────────────────────────────────────────────
  openPicker() {
    this.pickerOpen = true;
  }

  forkTemplate(t: FieldMappingTemplate) {
    this.pickerOpen = false;
    this.forkedFromId.set(t.id);
    this.forkedVersion.set(t.version);
    this.templateName.set(t.name);
    // Re-fetch the master's mappings to replace the table.
    this.api.getFieldMappings(t.id).subscribe((res) => {
      if (res.success && res.data) {
        this.mappings.set(
          res.data.mappings.map((m) => ({
            id: m.id,
            sourcePath: m.sourcePath,
            targetPath: m.targetPath,
            transformationType: m.transformationType,
            isRequired: m.isRequired,
            defaultValue: m.defaultValue ?? '',
          })),
        );
      }
    });
  }

  forkScratch() {
    this.pickerOpen = false;
    this.forkedFromId.set(SCRATCH_ID);
    this.forkedVersion.set(null);
    this.templateName.set('');
    this.mappings.set([]);
  }

  // ─── Mapping rows ───────────────────────────────────────────────
  addRow() {
    this.mappings.update((list) => [
      ...list,
      {
        id: 'fm-' + Math.random().toString(36).slice(2, 10),
        sourcePath: '',
        targetPath: '',
        transformationType: 'Direct',
        isRequired: false,
        defaultValue: '',
      },
    ]);
  }

  updateRow(index: number, change: Partial<MappingRow>) {
    this.mappings.update((list) => {
      const next = [...list];
      next[index] = { ...next[index], ...change };
      return next;
    });
  }

  removeRow(index: number) {
    this.mappings.update((list) => list.filter((_, i) => i !== index));
  }

  // ─── Test panel ─────────────────────────────────────────────────
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
    this.sampleOutput.set(
      JSON.stringify(
        {
          __status: 'Local preview unavailable',
          __note: 'Mapping evaluation runs server-side. Wire to /api/v1/templates/:id/preview later.',
          __mappingCount: this.mappings().length,
          __inputParsed: parsed,
        },
        null,
        2,
      ),
    );
  }

  // ─── Save ───────────────────────────────────────────────────────
  save() {
    if (!this.dirty() || this.saving()) return;
    this.saving.set(true);
    setTimeout(() => {
      this.snapshot.set({
        forkedFromId: this.forkedFromId(),
        forkedVersion: this.forkedVersion(),
        mappings: this.mappings().map((r) => ({ ...r })),
      });
      this.saving.set(false);
      this.gen.success('Mapping saved.');
      this.saved.emit();
    }, 400);
  }
}

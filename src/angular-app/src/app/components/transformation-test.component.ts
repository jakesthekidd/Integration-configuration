import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ApiService } from '../services/api.service';
import { FieldMappingTemplate } from '../models/template.model';
import { parseTree, printParseErrorCode, ParseError } from 'jsonc-parser';
import { MappingIssue, TransformRequest, TransformResult } from '../models/transformation-test.model';

@Component({
  selector: 'app-transformation-test',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="container">
      <h2>Test Transformation</h2>
      <p class="description">Upload or paste your source JSON to transform it using a field mapping template.</p>

      <div class="controls">
        <div class="template-selector">
          <label>
            Select Template:
            <select [(ngModel)]="selectedTemplateId" (change)="onTemplateChange()">
              <option value="">Choose a template...</option>
              <option *ngFor="let template of templates" [value]="template.id">
                {{ template.name }} (v{{ template.version }})
              </option>
            </select>
          </label>
        </div>

        <div class="file-upload">
          <label class="file-label">
            <input type="file" accept=".json" (change)="onFileSelected($event)" />
            Upload JSON File
          </label>
          <span *ngIf="fileName" class="file-name">{{ fileName }}</span>
        </div>

        <div class="sample-buttons">
          <button class="btn-secondary file-label" (click)="loadSampleInput()">Load Sample Input</button>
          <button class="btn-secondary file-label" (click)="clearAll()">Clear All</button>
        </div>
      </div>

      <div class="panels">
        <!-- Source JSON panel -->
        <div class="panel">
          <div class="panel-header">
            <h3>Source JSON (Input)</h3>
            <div class="panel-header-actions">
              <button class="btn-small" (click)="formatJson('source')" *ngIf="!showAnnotatedView">Format JSON</button>
              <button
                *ngIf="annotatedIssueCount > 0"
                class="btn-small btn-annotate"
                [class.active]="showAnnotatedView"
                (click)="toggleAnnotatedView()"
              >
                {{ showAnnotatedView ? 'Edit JSON' : 'Highlight Issues (' + annotatedIssueCount + ')' }}
              </button>
            </div>
          </div>
          <div
            class="source-body source-dropzone"
            [class.drag-over]="isDragOver"
            (dragover)="onDragOver($event)"
            (dragleave)="onDragLeave($event)"
            (drop)="onFileDrop($event)"
          >
            <div *ngIf="isParsing" class="spinner-overlay">
              <div class="spinner"></div>
              <div class="spinner-text">Parsing JSON...</div>
            </div>
            <textarea
              *ngIf="!showAnnotatedView"
              [(ngModel)]="sourceJson"
              class="json-editor"
              placeholder="Paste JSON or drag & drop a JSON file here..."
              spellcheck="false"
            ></textarea>

            <div *ngIf="isDragOver" class="drop-overlay">Drop JSON file here</div>
          </div>
          <div class="panel-footer">
            <span *ngIf="sourceJson">{{ getJsonSize(sourceJson) }}</span>
          </div>
        </div>

        <!-- Transformed JSON panel -->
        <div class="panel">
          <div class="panel-header">
            <h3>Transformed JSON (Output)</h3>
            <div class="panel-header-actions">
              <button class="btn-small" (click)="formatJson('target')" *ngIf="transformedJson">Format JSON</button>
              <button class="btn-small btn-success" (click)="copyToClipboard()" *ngIf="transformedJson">Copy</button>
            </div>
          </div>
          <textarea
            [(ngModel)]="transformedJson"
            class="json-editor"
            placeholder="Transformed JSON will appear here..."
            readonly
            spellcheck="false"
          ></textarea>
          <div class="panel-footer" *ngIf="transformResult">
            <span class="stat">Fields Mapped: {{ transformResult.fieldsMapped || 0 }}</span>
            <span class="stat">Fields Skipped: {{ transformResult.fieldsSkipped || 0 }}</span>
            <span class="stat">Execution Time: {{ transformResult.executionTimeMs || 0 }}ms</span>
          </div>
        </div>
      </div>

      <!-- Action bar -->
      <div class="action-bar">
        <button class="btn-primary btn-large" (click)="transform()" [disabled]="!canTransform()">Transform</button>
      </div>

      <!-- Status messages -->
      <div *ngIf="error" class="alert alert-error">
        <strong>Error:</strong> {{ error }}
        <pre *ngIf="errorDetails">{{ errorDetails }}</pre>
      </div>

      <div *ngIf="success" class="alert" [class.alert-success]="!hasErrors" [class.alert-partial]="hasErrors">
        <strong>{{ hasErrors ? 'Partial Success' : 'Success!' }}</strong>
        {{ success }}
      </div>

      <!-- Issues panel -->
      <div *ngIf="mappingIssues.length > 0" class="issues-panel">
        <div class="issues-panel-header">
          <h3 class="issues-title">Mapping Issues</h3>
          <div class="issues-summary">
            <span *ngIf="errorCount > 0" class="badge badge-error">
              {{ errorCount }} Error{{ errorCount !== 1 ? 's' : '' }}
            </span>
            <span *ngIf="warningCount > 0" class="badge badge-warning">
              {{ warningCount }} Warning{{ warningCount !== 1 ? 's' : '' }}
            </span>
          </div>
        </div>
        <div class="issues-table">
          <div class="issues-table-header">
            <span>Type</span>
            <span>Source Path</span>
            <span></span>
            <span>Target Path</span>
            <span>Message</span>
            <span></span>
          </div>
          <div
            *ngFor="let issue of mappingIssues"
            class="issue-row"
            [class.issue-error-row]="issue.type === 'error'"
            [class.issue-warning-row]="issue.type === 'warning'"
          >
            <span
              class="badge"
              [class.badge-error]="issue.type === 'error'"
              [class.badge-warning]="issue.type === 'warning'"
            >
              {{ issue.type === 'error' ? 'Error' : 'Warning' }}
            </span>
            <code class="path-code">{{ issue.sourcePath || '—' }}</code>
            <span class="arrow-sep">→</span>
            <code class="path-code">{{ issue.targetPath || '—' }}</code>
            <span class="issue-msg">{{ issue.message }}</span>
            <button
              *ngIf="issue.sourcePath"
              class="btn-small btn-locate"
              (click)="locateInSource(issue.sourcePath!)"
              title="Locate this field in source JSON"
            >
              Locate
            </button>
            <span *ngIf="!issue.sourcePath"></span>
          </div>
        </div>
      </div>

      <div class="info-section" *ngIf="!sourceJson && !transformedJson">
        <h3>How to Use:</h3>
        <ol>
          <li>Select a field mapping template from the dropdown</li>
          <li>Upload a JSON file or paste your JSON data in the Source JSON panel</li>
          <li>Click "Transform" to apply the field mappings</li>
          <li>Review the transformed output in the Transformed JSON panel</li>
          <li>If issues appear, click "Locate" next to any issue to jump to it in the source JSON</li>
        </ol>
        <p><strong>Tip:</strong> Click "Load Sample Input" to see an example transformation.</p>
      </div>
    </div>
  `,
  styles: [
    `
      .container {
        max-width: 1600px;
        margin: 0 auto;
        padding: 20px;
      }

      h2 {
        color: #2c3e50;
        margin-bottom: 10px;
      }

      .description {
        color: #666;
        margin-bottom: 20px;
      }

      .controls {
        display: flex;
        align-items: center;
        gap: 20px;
        margin-bottom: 20px;
        flex-wrap: nowrap;
      }
      .template-selector {
        flex: 1;
        min-width: 250px;
      }

      .template-selector label {
        display: flex;
        flex-direction: column;
        gap: 5px;
        font-weight: 500;
        color: #555;
      }

      .template-selector select {
        padding: 10px;
        border: 1px solid #ddd;
        border-radius: 4px;
        font-size: 14px;
      }

      .file-upload {
        display: flex;
        align-items: center;
        gap: 10px;
      }

      .file-label {
        display: inline-block;
        padding: 10px 20px;
        background: #3498db;
        color: white;
        border-radius: 4px;
        cursor: pointer;
        font-size: 14px;
        font-weight: 500;
        margin-top: 24px;
      }

      .file-label:hover {
        background: #2980b9;
      }

      .file-label input[type='file'] {
        display: none;
      }

      .file-name {
        color: #666;
        font-size: 14px;
      }

      .sample-buttons {
        display: flex;
        gap: 10px;
      }

      .panels {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 20px;
        margin-bottom: 20px;
      }

      @media (max-width: 1200px) {
        .panels {
          grid-template-columns: 1fr;
        }
      }

      .panel {
        display: flex;
        flex-direction: column;
        border: 1px solid #ddd;
        border-radius: 4px;
        background: white;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      }

      .panel-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 15px;
        background: #f8f9fa;
        border-bottom: 1px solid #ddd;
      }

      .panel-header h3 {
        margin: 0;
        color: #2c3e50;
        font-size: 16px;
      }

      .panel-header-actions {
        display: flex;
        gap: 8px;
        align-items: center;
      }

      .source-body {
        flex: 1;
        display: flex;
        flex-direction: column;
      }

      .json-editor {
        flex: 1;
        min-height: 400px;
        padding: 15px;
        border: none;
        font-family: 'Courier New', monospace;
        font-size: 13px;
        line-height: 1.5;
        resize: vertical;
        outline: none;
      }

      .json-editor:focus {
        background: #fafafa;
      }

      .json-annotated-container {
        flex: 1;
        overflow: auto;
      }

      :host ::ng-deep .json-annotated {
        min-height: 400px;
        margin: 0;
        padding: 15px;
        font-family: 'Courier New', monospace;
        font-size: 13px;
        line-height: 1.5;
        white-space: pre-wrap;
        word-break: break-all;
        background: #fafafa;
      }

      :host ::ng-deep mark.hl-error {
        background: #fdd;
        color: #c0392b;
        border-radius: 2px;
        padding: 0 2px;
        outline: 1px solid #e74c3c;
      }

      :host ::ng-deep mark.hl-warning {
        background: #fff3cd;
        color: #856404;
        border-radius: 2px;
        padding: 0 2px;
        outline: 1px solid #ffc107;
      }

      .panel-footer {
        padding: 10px 15px;
        background: #f8f9fa;
        border-top: 1px solid #ddd;
        font-size: 12px;
        color: #666;
        display: flex;
        gap: 15px;
      }

      .stat {
        display: flex;
        align-items: center;
        gap: 5px;
      }

      .action-bar {
        display: flex;
        justify-content: center;
        margin-bottom: 20px;
      }

      /* Buttons */
      .btn-primary,
      .btn-secondary,
      .btn-small,
      .btn-large,
      .btn-success {
        padding: 8px 16px;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 14px;
        font-weight: 500;
      }

      .btn-primary {
        background: #27ae60;
        color: white;
      }

      .btn-primary:hover:not(:disabled) {
        background: #229954;
      }

      .btn-primary:disabled {
        background: #95a5a6;
        cursor: not-allowed;
      }

      .btn-secondary {
        background: #95a5a6;
        color: white;
      }

      .btn-secondary:hover {
        background: #7f8c8d;
      }

      .btn-small {
        padding: 4px 12px;
        font-size: 12px;
        background: #3498db;
        color: white;
      }

      .btn-small:hover {
        background: #2980b9;
      }

      .btn-success {
        background: #27ae60;
        color: white;
      }

      .btn-success:hover {
        background: #229954;
      }

      .btn-large {
        padding: 12px 40px;
        font-size: 16px;
      }

      .btn-annotate {
        background: #6f42c1;
        color: white;
      }

      .btn-annotate:hover {
        background: #5a2d91;
      }

      .btn-annotate.active {
        background: #5a2d91;
      }

      /* Alerts */
      .alert {
        padding: 15px;
        border-radius: 4px;
        margin-bottom: 15px;
      }

      .alert-error {
        background: #fee;
        color: #c33;
        border-left: 4px solid #e74c3c;
      }

      .alert-error pre {
        margin-top: 10px;
        padding: 10px;
        background: #fff;
        border-radius: 4px;
        overflow-x: auto;
        font-size: 12px;
      }

      .alert-success {
        background: #d4edda;
        color: #155724;
        border-left: 4px solid #27ae60;
      }

      .alert-partial {
        background: #fff3cd;
        color: #856404;
        border-left: 4px solid #ffc107;
      }

      /* Issues panel */
      .issues-panel {
        margin-bottom: 20px;
        border: 1px solid #ddd;
        border-radius: 4px;
        background: white;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.08);
        overflow: hidden;
      }

      .issues-panel-header {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 12px 16px;
        background: #f8f9fa;
        border-bottom: 1px solid #ddd;
      }

      .issues-title {
        margin: 0;
        font-size: 15px;
        color: #2c3e50;
      }

      .issues-summary {
        display: flex;
        gap: 8px;
      }

      .issues-table-header {
        display: grid;
        grid-template-columns: 80px 200px 24px 200px 1fr 80px;
        gap: 8px;
        padding: 8px 16px;
        background: #f1f3f4;
        font-size: 12px;
        font-weight: 600;
        color: #555;
        border-bottom: 1px solid #e0e0e0;
      }

      .issue-row {
        display: grid;
        grid-template-columns: 80px 200px 24px 200px 1fr 80px;
        gap: 8px;
        padding: 10px 16px;
        align-items: center;
        border-bottom: 1px solid #f0f0f0;
        font-size: 13px;
      }

      .issue-row:last-child {
        border-bottom: none;
      }

      .issue-error-row {
        background: #fff8f8;
      }

      .issue-warning-row {
        background: #fffdf0;
      }

      .badge {
        display: inline-block;
        padding: 2px 8px;
        border-radius: 12px;
        font-size: 11px;
        font-weight: 600;
        text-transform: uppercase;
      }

      .badge-error {
        background: #fde8e8;
        color: #c0392b;
        border: 1px solid #e74c3c;
      }

      .badge-warning {
        background: #fff3cd;
        color: #856404;
        border: 1px solid #ffc107;
      }

      .path-code {
        font-family: 'Courier New', monospace;
        font-size: 12px;
        background: #f4f4f4;
        padding: 2px 6px;
        border-radius: 3px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
        display: block;
      }

      .arrow-sep {
        text-align: center;
        color: #999;
      }

      .issue-msg {
        color: #555;
        font-size: 13px;
      }

      .btn-locate {
        background: #6c757d;
        color: white;
        font-size: 11px;
        padding: 3px 10px;
      }

      .btn-locate:hover {
        background: #5a6268;
      }

      .info-section {
        background: #e3f2fd;
        padding: 20px;
        border-radius: 4px;
        border-left: 4px solid #2196f3;
      }

      .info-section h3 {
        margin-top: 0;
        color: #1976d2;
      }

      .info-section ol {
        line-height: 1.8;
        color: #555;
      }

      .info-section p {
        color: #666;
        margin-bottom: 0;
      }

      .source-dropzone {
        position: relative;
        flex: 1;
        min-height: 400px;
        border: 2px dashed #ccc;
        border-radius: 4px;
        transition:
          border-color 0.2s,
          background-color 0.2s;
      }

      .source-dropzone.drag-over {
        border-color: #3498db;
        background-color: #f0f8ff;
      }
      .drop-overlay {
        position: absolute;
        inset: 0;
        background: rgba(52, 152, 219, 0.15);
        display: flex;
        align-items: center;
        justify-content: center;
        font-weight: bold;
        color: #3498db;
        font-size: 18px;
        pointer-events: none;
      }
      .spinner-overlay {
        position: absolute;
        inset: 0;
        background: rgba(255, 255, 255, 0.7);
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        z-index: 10;
      }

      .spinner {
        width: 40px;
        height: 40px;
        border: 4px solid #ddd;
        border-top: 4px solid #3498db;
        border-radius: 50%;
        animation: spin 1s linear infinite;
      }

      .spinner-text {
        margin-top: 10px;
        font-size: 14px;
        color: #555;
      }

      @keyframes spin {
        to {
          transform: rotate(360deg);
        }
      }
    `,
  ],
})
export class TransformationTestComponent implements OnInit {
  templates: FieldMappingTemplate[] = [];
  selectedTemplateId: string = '';
  sourceJson: string = '';
  transformedJson: string = '';
  fileName: string = '';
  error: string = '';
  errorDetails: string = '';
  success: string = '';
  transformResult: TransformResult | null = null;
  mappingIssues: MappingIssue[] = [];
  showAnnotatedView = false;
  annotatedSourceHtml: SafeHtml = '';
  isDragOver = false;
  isParsing = false;

  constructor(
    private apiService: ApiService,
    private sanitizer: DomSanitizer,
  ) {}

  get errorCount(): number {
    return this.mappingIssues.filter((i) => i.type === 'error').length;
  }

  get warningCount(): number {
    return this.mappingIssues.filter((i) => i.type === 'warning').length;
  }

  get hasErrors(): boolean {
    return this.errorCount > 0;
  }

  /** Number of issues that have a sourcePath (can be highlighted in source JSON) */
  get annotatedIssueCount(): number {
    return this.mappingIssues.filter((i) => !!i.sourcePath).length;
  }

  ngOnInit() {
    this.loadTemplates();
  }

  loadTemplates() {
    this.apiService.getTemplates().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.templates = response.data.templates.filter((t) => t.status === 'Published');
        }
      },
      error: (err) => {
        this.error = 'Failed to load templates';
        console.error(err);
      },
    });
  }

  onTemplateChange() {
    this.error = '';
    this.success = '';

    if (this.selectedTemplateId && !this.sourceJson) {
      const template = this.templates.find((t) => t.id === this.selectedTemplateId);
      if (template?.sampleInputJson) {
        try {
          const parsed = JSON.parse(template.sampleInputJson);
          this.sourceJson = JSON.stringify(parsed, null, 2);
          this.fileName = '';
          this.showAnnotatedView = false;
        } catch {
          /* ignore malformed JSON */
        }
      }
    }
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.fileName = file.name;
      const reader = new FileReader();
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      reader.onload = (e: any) => {
        this.isParsing = true;

        setTimeout(() => {
          try {
            const json = JSON.parse(e.target.result);
            this.sourceJson = JSON.stringify(json, null, 2);
            this.error = '';
            this.showAnnotatedView = false;
          } catch {
            this.error = 'Invalid JSON file';
            this.sourceJson = '';
          } finally {
            this.isParsing = false;
          }
        }, 0);
      };
      reader.readAsText(file);
    }
  }

  loadSampleInput() {
    if (!this.selectedTemplateId) {
      this.error = 'Please select a template first to load its sample input.';
      return;
    }

    this.apiService.getTemplateById(this.selectedTemplateId).subscribe({
      next: (response) => {
        const template = response.data;
        if (template?.sampleInputJson) {
          try {
            const parsed = JSON.parse(template.sampleInputJson);
            this.sourceJson = JSON.stringify(parsed, null, 2);
            this.fileName = 'sample-input.json';
            this.showAnnotatedView = false;
            this.error = '';
          } catch {
            this.error = "The template's sample input JSON is invalid and could not be loaded.";
          }
        } else {
          this.error = 'This template does not have a sample input JSON assigned.';
        }
      },
      error: (err) => {
        this.error = 'Failed to load template sample input.';
        console.error(err);
      },
    });
  }

  clearAll() {
    this.sourceJson = '';
    this.transformedJson = '';
    this.fileName = '';
    this.error = '';
    this.errorDetails = '';
    this.success = '';
    this.transformResult = null;
    this.mappingIssues = [];
    this.showAnnotatedView = false;
    this.annotatedSourceHtml = '';
  }

  canTransform(): boolean {
    return !!(this.selectedTemplateId && this.sourceJson);
  }

  transform() {
    if (!this.canTransform()) return;

    this.error = '';
    this.errorDetails = '';
    this.success = '';
    this.transformedJson = '';
    this.transformResult = null;
    this.mappingIssues = [];
    this.showAnnotatedView = false;
    this.annotatedSourceHtml = '';

    try {
      JSON.parse(this.sourceJson);
    } catch (e) {
      this.error = 'Invalid JSON format in source';
      return;
    }

    const request: TransformRequest = {
      sourceJson: this.sourceJson,
      templateId: this.selectedTemplateId,
    };

    // Server always returns HTTP 200 — read everything from the next callback
    this.apiService.transformJsonWithTemplate(request).subscribe({
      next: (response) => {
        const data: TransformResult = response?.data ?? {};

        if (data.outputJson) {
          this.transformedJson = data.outputJson;
        } else if (data.transformedData) {
          this.transformedJson = JSON.stringify(data.transformedData, null, 2);
        }

        this.transformResult = data;

        // Normalise errors and warnings into a unified MappingIssue list
        const issues: MappingIssue[] = [];

        for (const err of data.errors ?? []) {
          issues.push({
            type: 'error',
            code: err.errorCode,
            sourcePath: err.sourcePath ?? undefined,
            targetPath: err.fieldPath ?? undefined,
            message: err.message,
          });
        }

        for (const warn of data.warnings ?? []) {
          issues.push({
            type: 'warning',
            code: warn.code,
            sourcePath: warn.sourcePath ?? undefined,
            targetPath: warn.targetPath ?? undefined,
            message: warn.message,
          });
        }

        this.mappingIssues = issues;

        if (data.success) {
          this.success =
            issues.length > 0
              ? `Transformation completed with ${this.warningCount} warning(s).`
              : 'Transformation completed successfully.';
        } else {
          const partialNote = this.transformedJson ? ' Partial output is shown below.' : '';
          this.success = `Partial transformation: ${this.errorCount} required field(s) could not be mapped.${partialNote}`;
        }

        if (this.annotatedIssueCount > 0) {
          this.buildAnnotatedJson();
        }
      },
      error: (err) => {
        // Only reached on network errors or HTTP 5xx
        this.error = err.error?.message || 'Failed to connect to transformation service';
        if (err.error?.errors) {
          this.errorDetails = JSON.stringify(err.error.errors, null, 2);
        }
        console.error(err);
      },
    });
  }

  toggleAnnotatedView(): void {
    this.showAnnotatedView = !this.showAnnotatedView;
    if (this.showAnnotatedView) {
      this.buildAnnotatedJson();
    }
  }

  /**
   * Switches to the annotated view and scrolls to the first occurrence
   * of the given source path's root key.
   */
  locateInSource(sourcePath: string): void {
    this.showAnnotatedView = true;
    this.buildAnnotatedJson();
    const rootKey = sourcePath.split(/[.[]/)[0];
    setTimeout(() => {
      const selector = `mark[data-key="${CSS.escape(rootKey)}"]`;
      const el = document.querySelector(selector);
      if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }, 50);
  }

  /**
   * Builds a sanitized HTML string from the source JSON with problem fields
   * wrapped in <mark> elements (hl-error or hl-warning).
   */
  buildAnnotatedJson(): void {
    if (!this.sourceJson) {
      this.annotatedSourceHtml = '';
      return;
    }

    // Collect root keys with their issue priority (error beats warning)
    const pathIssues = new Map<string, 'error' | 'warning'>();
    for (const issue of this.mappingIssues) {
      if (!issue.sourcePath) continue;
      const rootKey = issue.sourcePath.split(/[.[]/)[0];
      if (!rootKey) continue;
      const existing = pathIssues.get(rootKey);
      if (!existing || (existing === 'warning' && issue.type === 'error')) {
        pathIssues.set(rootKey, issue.type);
      }
    }

    let html = this.escapeHtml(this.sourceJson);

    // After HTML-escaping, double-quotes become &quot;
    // Match JSON key pattern: &quot;keyName&quot; followed by optional whitespace and colon
    for (const [key, issueType] of pathIssues) {
      const cssClass = issueType === 'error' ? 'hl-error' : 'hl-warning';
      const escapedKey = key.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
      const pattern = new RegExp(`(&quot;${escapedKey}&quot;)(\\s*:)`, 'g');
      html = html.replace(pattern, `<mark class="${cssClass}" data-key="${key}">$1</mark>$2`);
    }

    this.annotatedSourceHtml = this.sanitizer.bypassSecurityTrustHtml('<pre class="json-annotated">' + html + '</pre>');
  }

  formatJson(target: 'source' | 'target') {
    const json = target === 'source' ? this.sourceJson : this.transformedJson;

    try {
      const parsed = JSON.parse(json);
      const formatted = JSON.stringify(parsed, null, 2);

      if (target === 'source') {
        this.sourceJson = formatted;
      } else {
        this.transformedJson = formatted;
      }

      this.error = '';
      this.annotatedSourceHtml = '';
    } catch (e: unknown) {
      const errors = this.validateJsonAllErrors(json);

      if (errors.length > 0) {
        this.error = `Invalid JSON (${errors.length} errors found)`;

        this.errorDetails = errors.join('\n');
      } else {
        this.error = 'Invalid JSON format';
      }
    }
  }

  annotateJsonError(json: string, errorDetails: { line: number; column: number }): string {
    if (!json || !errorDetails) return json;

    const { line, column } = errorDetails;
    const lines = json.split('\n');

    if (line < 1 || line > lines.length) return json;

    const errorLine = lines[line - 1];

    if (!errorLine || column < 1 || column > errorLine.length) {
      return json;
    }

    const highlightedChar = errorLine[column - 1] || '';

    const highlighted =
      errorLine.substring(0, column - 1) +
      `<span class="json-error-marker">${this.escapeHtml(highlightedChar)}</span>` +
      errorLine.substring(column);

    lines[line - 1] = highlighted;

    return lines.join('\n');
  }

  validateJsonAllErrors(json: string): string[] {
    const errors: ParseError[] = [];

    parseTree(json, errors);

    const lines = json.split('\n');

    return errors.map((e, index) => {
      const beforeError = json.substring(0, e.offset);
      const line = beforeError.split('\n').length;
      const column = beforeError.split('\n').pop()?.length || 0;

      const lineText = lines[line - 1] || '';

      const fieldName = this.getFieldNameFromLine(lineText);

      const baseMessage = `${index + 1}: ${printParseErrorCode(e.error)} at line ${line}, column ${column}`;

      return fieldName ? `${baseMessage} (near field: ${fieldName})` : baseMessage;
    });
  }

  private getFieldNameFromLine(lineText: string): string | null {
    const match = lineText.match(/"([^"]+)"\s*:/);

    return match ? match[1] : null;
  }

  getJsonErrorDetails(json: string, error: Error) {
    const match = /position (\d+)/.exec(error.message);
    if (!match) return null;

    const pos = Number(match[1]);
    const lines = json.substring(0, pos).split('\n');

    return {
      line: lines.length,
      column: lines[lines.length - 1].length + 1,
      position: pos,
      message: error.message,
    };
  }

  copyToClipboard() {
    navigator.clipboard.writeText(this.transformedJson).then(() => {
      this.success = 'Copied to clipboard!';
      setTimeout(() => (this.success = ''), 2000);
    });
  }

  getJsonSize(json: string): string {
    const bytes = new Blob([json]).size;
    if (bytes < 1024) return `${bytes} bytes`;
    if (bytes < 1048576) return `${(bytes / 1024).toFixed(2)} KB`;
    return `${(bytes / 1048576).toFixed(2)} MB`;
  }

  private escapeHtml(text: string): string {
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    this.isDragOver = false;
  }

  onFileDrop(event: DragEvent) {
    event.preventDefault();
    this.isDragOver = false;

    if (event.dataTransfer?.files.length) {
      const file = event.dataTransfer.files[0];
      if (file.type === 'application/json' || file.name.endsWith('.json')) {
        const reader = new FileReader();
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        reader.onload = (e: any) => {
          this.isParsing = true;

          setTimeout(() => {
            try {
              const json = JSON.parse(e.target.result);
              this.sourceJson = JSON.stringify(json, null, 2);
              this.error = '';
              this.showAnnotatedView = false;
            } catch {
              this.error = 'Invalid JSON file';
              this.sourceJson = '';
            } finally {
              this.isParsing = false;
            }
          }, 0);
        };
        reader.readAsText(file);
        this.fileName = file.name;
      } else {
        this.error = 'Only JSON files are supported';
      }
    }
  }
}

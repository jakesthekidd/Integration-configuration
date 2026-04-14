import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ApiService } from '../../services/api.service';
import { FieldMappingTemplate } from '../../models/template.model';
import { parseTree, printParseErrorCode, ParseError } from 'jsonc-parser';
import { MappingIssue, TransformResult } from '../../models/transformation-test.model';
import { ApiClient } from '../../models/api-client.model';

@Component({
  selector: 'app-transformation-test',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './transformation-test.component.html',
  styleUrl: './transformation-test.component.scss',
})
export class TransformationTestComponent implements OnInit {
  templates: FieldMappingTemplate[] = [];
  selectedTemplateId: string = '';
  apiClients: ApiClient[] = [];
  selectedClientId: string = '';
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
  isTransforming: boolean = false;
  lineNumbers: number[] = [1];
  private typingTimeout: any;

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
    this.loadApiClients();
  }

  loadApiClients() {
    this.apiService.getApiClients().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.apiClients = response.data.apiClients.filter((c) => c.isActive);
        }
      },
      error: (err) => console.error('Failed to load API clients', err),
    });
  }

  loadTemplates() {
    this.apiService.getTemplates().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.templates = response.data.templates.filter((t) => t.status === 'Active');
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

    if (!file) return;

    const fileExtension = file.name.split('.').pop()?.toLowerCase();
    if (fileExtension !== 'json') {
      this.error = 'Only JSON files are supported';
      this.fileName = '';
      return;
    }

    this.error = '';
    this.fileName = file.name;

    const reader = new FileReader();
    reader.onload = (e: any) => {
      this.isParsing = true;

      setTimeout(() => {
        const content = e.target.result;
        try {
          const parsed = JSON.parse(content);
          this.sourceJson = JSON.stringify(parsed, null, 2);
          this.showAnnotatedView = false;
        } catch (err) {
          this.error = 'Invalid JSON file';
          this.sourceJson = content;
        }
        this.isParsing = false;
      }, 0);
    };
    reader.readAsText(file);
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
    return !!(this.selectedTemplateId && this.selectedClientId && this.sourceJson);
  }

  transform() {
    if (!this.canTransform()) {
      return;
    }

    this.isTransforming = true;
    this.error = '';
    this.errorDetails = '';
    this.success = '';
    this.transformedJson = '';
    this.transformResult = null;
    this.mappingIssues = [];
    this.showAnnotatedView = false;
    this.annotatedSourceHtml = '';

    let parsedSource: unknown;
    try {
      parsedSource = JSON.parse(this.sourceJson);
    } catch (e) {
      this.error = 'Invalid JSON format in source';
      this.isTransforming = false;
      return;
    }

    const selectedTemplate = this.templates.find((t) => t.id === this.selectedTemplateId);
    if (!selectedTemplate?.version) {
      this.error = 'No published version found for the selected template.';
      this.isTransforming = false;
      return;
    }

    // Server always returns HTTP 200 — read everything from the next callback
    this.apiService
      .transformJsonWithTemplate(this.selectedTemplateId, selectedTemplate.version, parsedSource, this.selectedClientId)
      .subscribe({
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

          const execMs = data.executionTimeMs ?? 0;
          const execLabel = execMs < 1000 ? `${execMs}ms` : `${(execMs / 1000).toFixed(2)}s`;

          if (data.success) {
            this.success =
              issues.length > 0
                ? `Transformation completed with ${this.warningCount} warning(s). (${execLabel})`
                : `Transformation completed successfully. (${execLabel})`;
          } else {
            const partialNote = this.transformedJson ? ' Partial output is shown below.' : '';
            this.success = `Partial transformation: ${this.errorCount} required field(s) could not be mapped.${partialNote} (${execLabel})`;
          }

          if (this.annotatedIssueCount > 0) {
            this.buildAnnotatedJson();
            this.showAnnotatedView = true;
          }

          this.isTransforming = false;
        },
        error: (err) => {
          // Only reached on network errors or HTTP 5xx
          this.error = err.error?.message || 'Failed to connect to transformation service';
          if (err.error?.errors) {
            this.errorDetails = JSON.stringify(err.error.errors, null, 2);
          }
          console.error(err);
          this.isTransforming = false;
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

    const selector = `mark[data-key="${CSS.escape(rootKey)}"]`;

    requestAnimationFrame(() => {
      const el = document.querySelector(selector);
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
    });
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
      this.errorDetails = '';
      this.annotatedSourceHtml = '';
    } catch (e: any) {
      const errors = this.validateJsonAllErrors(json);

      this.error = errors.length ? `Invalid JSON (${errors.length} errors found)` : 'Invalid JSON format';

      this.errorDetails = errors.join('\n');
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
            const content = e.target.result;

            this.sourceJson = content;
            this.error = '';
            this.showAnnotatedView = false;
            this.isParsing = false;
          }, 0);
        };
        reader.readAsText(file);
        this.fileName = file.name;
      } else {
        this.error = 'Only JSON files are supported';
      }
    }
  }

  getLineNumbers(text: any): number[] {
    if (typeof text !== 'string') {
      text = text ? String(text) : '';
    }

    const lines = text.split('\n').length;
    return Array.from({ length: lines }, (_, i) => i + 1);
  }

  syncScroll(event: any) {
    const textarea = event.target;
    const lineNumbers = textarea.parentElement.querySelector('.line-numbers-inner');

    if (lineNumbers) {
      lineNumbers.style.transform = `translateY(-${textarea.scrollTop}px)`;
    }
  }
  updateLineNumbers() {
    const maxLines = 2000;
    const lines = this.sourceJson ? this.sourceJson.split('\n').length : 1;

    const count = Math.min(lines, maxLines);

    this.lineNumbers = Array.from({ length: count }, (_, i) => i + 1);
  }
  onJsonChange(value: string) {
    this.sourceJson = value;

    clearTimeout(this.typingTimeout);

    this.typingTimeout = setTimeout(() => {
      this.updateLineNumbers();
    }, 300);
  }
}

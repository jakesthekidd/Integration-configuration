import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { MenuItem } from 'primeng/api';

import { Router, ActivatedRoute } from '@angular/router';
import { GeneralService } from '../services/general.service';
import { DraftService } from '../services/draft.service';
import { Deployment } from '../models/deployment.model';
import { Version, VersionState } from '../models/version.model';
import { mockVersions } from '../mocks/mock-data';
import { AutofocusDirective } from './autofocus.directive';

/**
 * Publish & Activate tab — implements PUBLISH-ACTIVATE-PLAN.md.
 *
 * Layout:
 *   1. Conditional draft banner at top (singleton uncommitted state)
 *   2. PrimeNG p-table of all published versions (Active / Published / Archived)
 *
 * Rules:
 *   - Exactly one Active per deployment.
 *   - Activating a Published or Archived row auto-archives the previous Active.
 *   - Undo banner appears for 5s after auto-archive, restores both rows on click.
 */
@Component({
  selector: 'app-test-publish-tab',
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    MenuModule,
    TableModule,
    TagModule,
    AutofocusDirective,
  ],
  template: `
    <!-- ── Draft banner ─────────────────────────────────────────── -->
    @if (currentDraft(); as draft) {
      <section class="draft-banner">
        <div class="draft-banner__head">
          <div>
            <span class="draft-banner__chip">
              <i class="pi pi-pencil"></i>
              Unsaved draft
            </span>
            <span class="draft-banner__meta">
              based on v{{ draft.basedOnVersionNumber ?? '—' }} ·
              edited {{ draft.createdAt | date: 'MMM d, y · h:mm a' }} ·
              {{ draft.createdBy }}
            </span>
          </div>
          <div class="draft-banner__actions">
            <p-button
              label="Discard"
              icon="pi pi-trash"
              severity="secondary"
              [outlined]="true"
              size="small"
              (onClick)="discardDraft()"
            />
            <p-button
              label="Publish"
              icon="pi pi-cloud-upload"
              severity="primary"
              size="small"
              [loading]="busy() === 'publish'"
              (onClick)="publishDraft()"
            />
          </div>
        </div>
        <label class="draft-banner__notes">
          <span class="draft-banner__notes-label">Notes</span>
          <textarea
            rows="2"
            placeholder="What changed in this draft?"
            [ngModel]="draft.notes ?? ''"
            (ngModelChange)="updateDraftNotes($event)"
          ></textarea>
        </label>
      </section>
    }

    <!-- ── Undo banner (auto-archive feedback) ─────────────────── -->
    @if (undoSnapshot(); as snap) {
      <section class="undo-banner">
        <i class="pi pi-info-circle"></i>
        <span>
          v{{ snap.archived.versionNumber }} was auto-archived to activate
          v{{ snap.activated.versionNumber }}.
        </span>
        <button type="button" class="undo-banner__btn" (click)="undoActivate()">
          Undo
        </button>
      </section>
    }

    <!-- ── Versions table ──────────────────────────────────────── -->
    <p-table
      [value]="tableRows()"
      [tableStyle]="{ 'min-width': '720px' }"
      styleClass="p-datatable-sm versions-table"
      [paginator]="false"
      dataKey="id"
    >
      <ng-template pTemplate="caption">
        <div class="versions-table__caption">
          <h4>Versions</h4>
          <span class="muted">
            {{ tableRows().length }} {{ tableRows().length === 1 ? 'version' : 'versions' }}
          </span>
        </div>
      </ng-template>
      <ng-template pTemplate="header">
        <tr>
          <th style="width: 70px">Version</th>
          <th style="width: 140px">Status</th>
          <th style="width: 140px">Published</th>
          <th style="width: 180px">Activated</th>
          <th>Notes</th>
          <th style="width: 56px"></th>
        </tr>
      </ng-template>
      <ng-template pTemplate="body" let-v>
        <tr [class.row--active]="v.state === 'Activated'">
          <td><strong>v{{ v.versionNumber }}</strong></td>
          <td>
            @switch (v.state) {
              @case ('Activated') {
                <p-tag value="Active" severity="success" icon="pi pi-check-circle" />
              }
              @case ('Published') {
                <p-tag value="Published" severity="info" />
              }
              @case ('Archived') {
                <p-tag value="Archived" severity="secondary" />
              }
            }
          </td>
          <td>
            @if (v.publishedAt) {
              {{ v.publishedAt | date: 'MMM d, y' }}
            } @else {
              <span class="muted">—</span>
            }
          </td>
          <td>
            @if (v.state === 'Activated' && v.activatedAt) {
              {{ v.activatedAt | date: 'MMM d, y' }}
            } @else if (v.state === 'Archived' && v.activatedAt && v.archivedAt) {
              {{ v.activatedAt | date: 'MMM d' }} – {{ v.archivedAt | date: 'MMM d, y' }}
            } @else {
              <span class="muted">—</span>
            }
          </td>
          <td class="notes-cell">
            @if (editingNotesId() === v.id) {
              <textarea
                appAutofocus
                rows="2"
                [ngModel]="notesDraft()"
                (ngModelChange)="notesDraft.set($event)"
                (blur)="saveNotes(v)"
                (keydown.enter)="saveNotes(v); $event.preventDefault()"
                (keydown.escape)="cancelNotesEdit()"
              ></textarea>
            } @else {
              <button
                type="button"
                class="notes-display"
                (click)="startNotesEdit(v)"
                [title]="v.notes || 'Click to add notes'"
              >
                {{ v.notes || 'Add notes…' }}
              </button>
            }
          </td>
          <td>
            <p-button
              icon="pi pi-ellipsis-h"
              severity="secondary"
              [text]="true"
              [rounded]="true"
              size="small"
              (onClick)="openMenu(v.id, $event, rowMenu)"
              aria-label="Row actions"
            />
          </td>
        </tr>
      </ng-template>
      <ng-template pTemplate="emptymessage">
        <tr>
          <td colspan="6" class="empty">
            <i class="pi pi-inbox"></i>
            No published versions yet. Publish the current draft to seed this list.
          </td>
        </tr>
      </ng-template>
    </p-table>

    <!-- Shared row menu (model rebuilt per click) -->
    <p-menu #rowMenu [popup]="true" [model]="currentMenuItems()" appendTo="body" />
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-5);
      }

      .muted { color: var(--tf-text-muted); }

      /* ── Draft banner ────────────────────────────────────────── */
      .draft-banner {
        background: #fff8e1;
        border: 1px solid #f1c40f;
        border-radius: var(--tf-radius-md);
        padding: var(--tf-space-4) var(--tf-space-5);
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-3);
      }
      .draft-banner__head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--tf-space-3);
        flex-wrap: wrap;
      }
      .draft-banner__chip {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        background: #92510a;
        color: #fff;
        font-size: var(--tf-text-meta);
        font-weight: 700;
        padding: 4px 10px;
        border-radius: var(--tf-radius-pill);
        text-transform: uppercase;
        letter-spacing: .03em;
      }
      .draft-banner__meta {
        font-size: var(--tf-text-meta);
        color: #92510a;
        margin-left: var(--tf-space-2);
      }
      .draft-banner__actions {
        display: flex;
        gap: var(--tf-space-2);
      }
      .draft-banner__notes {
        display: flex;
        flex-direction: column;
        gap: 4px;
      }
      .draft-banner__notes-label {
        font-size: var(--tf-text-meta);
        font-weight: 600;
        color: #92510a;
      }
      .draft-banner__notes textarea {
        width: 100%;
        font-family: inherit;
        font-size: var(--tf-text-body);
        padding: var(--tf-space-2) var(--tf-space-3);
        border: 1px solid #e5b800;
        border-radius: var(--tf-radius-sm);
        resize: vertical;
        background: #fff;
      }

      /* ── Undo banner ─────────────────────────────────────────── */
      .undo-banner {
        display: flex;
        align-items: center;
        gap: var(--tf-space-2);
        background: #eef2ff;
        border: 1px solid #c7d2fe;
        color: #3730a3;
        padding: var(--tf-space-2) var(--tf-space-4);
        border-radius: var(--tf-radius-md);
        font-size: var(--tf-text-body);
      }
      .undo-banner__btn {
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
      .undo-banner__btn:hover {
        background: #3730a3;
        color: #fff;
      }

      /* ── Versions table ──────────────────────────────────────── */
      .versions-table__caption {
        display: flex;
        align-items: baseline;
        gap: var(--tf-space-3);
        padding: var(--tf-space-2) 0;
      }
      .versions-table__caption h4 {
        margin: 0;
        font-size: var(--tf-text-heading);
      }

      :host ::ng-deep tr.row--active td {
        background: #eafaf0;
      }

      .notes-cell {
        max-width: 360px;
      }
      .notes-display {
        background: none;
        border: 1px dashed transparent;
        text-align: left;
        cursor: text;
        color: var(--tf-text-strong);
        font-size: var(--tf-text-body);
        padding: 4px 6px;
        border-radius: var(--tf-radius-sm);
        width: 100%;
        white-space: normal;
      }
      .notes-display:hover {
        border-color: var(--tf-slate-400);
        background: var(--tf-slate-100);
      }
      .notes-cell textarea {
        width: 100%;
        font-family: inherit;
        font-size: var(--tf-text-body);
        padding: 4px 6px;
        border: 1px solid var(--tf-primary, #1a56db);
        border-radius: var(--tf-radius-sm);
        resize: vertical;
      }

      .empty {
        text-align: center;
        color: var(--tf-text-muted);
        padding: var(--tf-space-5);
        font-size: var(--tf-text-body);
      }
      .empty i {
        margin-right: 6px;
      }

      :host ::ng-deep .p-menu .menu-item--danger .p-menuitem-link {
        color: #83131a;
      }
      :host ::ng-deep .p-menu .menu-item--danger .p-menuitem-icon {
        color: #83131a;
      }
    `,
  ],
})
export class TestPublishTabComponent implements OnChanges {
  @Input({ required: true }) deployment!: Deployment;
  @Input() customerName = 'this customer';
  @Output() statusChanged = new EventEmitter<void>();

  private gen = inject(GeneralService);
  private draftService = inject(DraftService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  versions = signal<Version[]>([]);
  busy = signal<string | null>(null);

  /** Last-seen spawn request counter — used to ignore stale ticks. */
  private lastSeenSpawnRequest = 0;

  constructor() {
    // Keep the shared DraftService in sync so the tab strip can decorate
    // its "Publish & Activate" label whenever a draft exists.
    effect(() => {
      const id = this.deployment?.id;
      if (!id) return;
      this.draftService.setDraft(id, !!this.currentDraft());
    });

    // Auto-fork: the Mapping/Connection tabs bump `requestSpawnDraft` on the
    // user's first edit while the deployment has no Draft. We seed a Draft row
    // from the current Active (or most-recently-published) version. The
    // currentDraft computed reactively flips and the cross-tab amber dot
    // lights up.
    effect(() => {
      const id = this.deployment?.id;
      if (!id) return;
      const ticket = this.draftService.spawnRequest(id);
      if (ticket === 0 || ticket === this.lastSeenSpawnRequest) return;
      this.lastSeenSpawnRequest = ticket;
      // Don't double-spawn if a Draft already exists.
      if (this.currentDraft()) return;
      const baseVersion =
        this.versions().find((v) => v.state === 'Activated') ??
        this.versions().find((v) => v.state === 'Published') ??
        this.versions().find((v) => v.state === 'Archived');
      // Brand-new deployment with no version history → seed a v1 Draft from
      // scratch so the user has something to publish & activate.
      const nextNumber = baseVersion
        ? Math.max(0, ...this.versions().map((x) => x.versionNumber)) + 1
        : 1;
      const newDraft: Version = {
        id: `v-${Date.now()}`,
        deploymentId: id,
        versionNumber: nextNumber,
        state: 'Draft',
        createdAt: new Date().toISOString(),
        createdBy: this.currentAuthor,
        notes: baseVersion
          ? `Auto-forked from v${baseVersion.versionNumber} (${baseVersion.state}) on first edit`
          : 'New draft — first configuration for this capability',
        basedOnVersionNumber: baseVersion?.versionNumber,
      };
      this.versions.update((list) => [newDraft, ...list]);
    });
  }

  /** Inline notes editor state (for table rows). */
  editingNotesId = signal<string | null>(null);
  notesDraft = signal<string>('');

  /** Active undo snapshot for the most recent auto-archive (5s window). */
  undoSnapshot = signal<{ archived: Version; activated: Version } | null>(null);
  private undoTimer: ReturnType<typeof setTimeout> | null = null;

  private currentAuthor = 'Jake Cummings';

  // ── Computed views ────────────────────────────────────────────
  currentDraft = computed<Version | null>(
    () => this.versions().find((v) => v.state === 'Draft') ?? null,
  );

  /** Rows shown in the p-table: everything except the singleton draft. */
  tableRows = computed<Version[]>(() => {
    const order: Record<VersionState, number> = {
      Activated: 0,
      Published: 1,
      Archived: 2,
      Draft: 99,
    };
    return this.versions()
      .filter((v) => v.state !== 'Draft')
      .sort((a, b) => {
        if (order[a.state] !== order[b.state]) return order[a.state] - order[b.state];
        return b.versionNumber - a.versionNumber;
      });
  });

  // ── Manage menu (one shared p-menu, model rebuilt per click) ──
  private menuVersionId = signal<string | null>(null);
  currentMenuItems = computed<MenuItem[]>(() => {
    const id = this.menuVersionId();
    if (!id) return [];
    const v = this.versions().find((x) => x.id === id);
    if (!v) return [];
    return this.buildMenuItems(v);
  });

  private buildMenuItems(v: Version): MenuItem[] {
    const items: MenuItem[] = [];

    items.push({
      label: 'View field mappings',
      icon: 'pi pi-eye',
      command: () => this.viewFieldMappings(v),
    });

    if (v.state === 'Published') {
      items.push({
        label: 'Activate',
        icon: 'pi pi-check-circle',
        command: () => this.activate(v),
      });
      items.push({
        label: 'Archive',
        icon: 'pi pi-box',
        command: () => this.archive(v),
      });
    }

    if (v.state === 'Archived') {
      items.push({
        label: 'Reactivate',
        icon: 'pi pi-undo',
        command: () => this.activate(v),
      });
    }

    if (v.state !== 'Activated') {
      items.push({
        label: 'Edit as new draft',
        icon: 'pi pi-pencil',
        command: () => this.editAsDraft(v),
      });
    }

    if (v.state === 'Archived') {
      items.push({ separator: true });
      items.push({
        label: 'Delete permanently',
        icon: 'pi pi-trash',
        styleClass: 'menu-item--danger',
        command: () => this.deleteArchived(v),
      });
    }

    return items;
  }

  openMenu(versionId: string, event: MouseEvent | Event, menu: { toggle: (e: Event) => void }) {
    this.menuVersionId.set(versionId);
    menu.toggle(event);
  }

  // ── Lifecycle ────────────────────────────────────────────────
  ngOnChanges(changes: SimpleChanges) {
    if (changes['deployment']) {
      const id = this.deployment?.id;
      const seed = id ? mockVersions[id] ?? [] : [];
      this.versions.set(seed.map((v) => ({ ...v })));
      this.busy.set(null);
      this.menuVersionId.set(null);
      this.editingNotesId.set(null);
      this.notesDraft.set('');
      this.clearUndo();
    }
  }

  // ── Draft actions ────────────────────────────────────────────
  updateDraftNotes(value: string) {
    this.versions.update((list) =>
      list.map((v) => (v.state === 'Draft' ? { ...v, notes: value } : v)),
    );
  }

  publishDraft() {
    const draft = this.currentDraft();
    if (!draft) return;
    this.busy.set('publish');
    setTimeout(() => {
      const now = new Date().toISOString();
      this.versions.update((list) =>
        list.map((v) =>
          v.id === draft.id
            ? { ...v, state: 'Published' as VersionState, publishedAt: now }
            : v,
        ),
      );
      this.busy.set(null);
      this.gen.success(`v${draft.versionNumber} published.`);
      this.statusChanged.emit();
    }, 400);
  }

  discardDraft() {
    const draft = this.currentDraft();
    if (!draft) return;
    this.versions.update((list) => list.filter((v) => v.id !== draft.id));
    this.gen.success(`Draft v${draft.versionNumber} discarded.`);
  }

  // ── Row actions ──────────────────────────────────────────────
  activate(v: Version) {
    if (v.state === 'Activated') return;
    const now = new Date().toISOString();
    const prevActive = this.versions().find((x) => x.state === 'Activated') ?? null;

    this.versions.update((list) =>
      list.map((x) => {
        if (x.id === v.id) {
          return { ...x, state: 'Activated' as VersionState, activatedAt: now, archivedAt: undefined };
        }
        if (prevActive && x.id === prevActive.id) {
          return { ...x, state: 'Archived' as VersionState, archivedAt: now };
        }
        return x;
      }),
    );

    if (prevActive) {
      this.armUndo(prevActive, v);
      this.gen.success(`v${v.versionNumber} activated · v${prevActive.versionNumber} auto-archived.`);
    } else {
      this.gen.success(`v${v.versionNumber} activated.`);
    }
    this.statusChanged.emit();
  }

  archive(v: Version) {
    if (v.state === 'Activated') {
      this.gen.success('Activate another version first to archive the current Active.');
      return;
    }
    const now = new Date().toISOString();
    this.versions.update((list) =>
      list.map((x) =>
        x.id === v.id ? { ...x, state: 'Archived' as VersionState, archivedAt: now } : x,
      ),
    );
    this.gen.success(`v${v.versionNumber} archived.`);
  }

  editAsDraft(v: Version) {
    // Forking from a non-Draft version: spawn a new singleton draft based on this one.
    if (this.currentDraft()) {
      this.gen.success('A draft already exists. Publish or discard it before forking a new one.');
      return;
    }
    const nextNumber = Math.max(0, ...this.versions().map((x) => x.versionNumber)) + 1;
    const newDraft: Version = {
      id: `v-${Date.now()}`,
      deploymentId: this.deployment.id,
      versionNumber: nextNumber,
      state: 'Draft',
      createdAt: new Date().toISOString(),
      createdBy: this.currentAuthor,
      notes: v.notes ? `Forked from v${v.versionNumber}: ${v.notes}` : `Forked from v${v.versionNumber}`,
      basedOnVersionNumber: v.versionNumber,
    };
    this.versions.update((list) => [newDraft, ...list]);
  }

  deleteArchived(v: Version) {
    this.versions.update((list) => list.filter((x) => x.id !== v.id));
    this.gen.success(`v${v.versionNumber} deleted.`);
  }

  viewFieldMappings(v: Version) {
    const label = `v${v.versionNumber} (${v.state})`;
    this.draftService.setViewVersion(this.deployment.id, { id: v.id, label });
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab: 'mapping' },
      queryParamsHandling: 'merge',
    });
  }

  // ── Undo plumbing ────────────────────────────────────────────
  private armUndo(archived: Version, activated: Version) {
    this.clearUndo();
    this.undoSnapshot.set({ archived, activated });
    this.undoTimer = setTimeout(() => this.clearUndo(), 5000);
  }

  undoActivate() {
    const snap = this.undoSnapshot();
    if (!snap) return;
    this.versions.update((list) =>
      list.map((x) => {
        if (x.id === snap.archived.id) {
          return { ...snap.archived };
        }
        if (x.id === snap.activated.id) {
          return { ...snap.activated };
        }
        return x;
      }),
    );
    this.clearUndo();
    this.gen.success(`Reverted: v${snap.archived.versionNumber} is Active again.`);
  }

  private clearUndo() {
    if (this.undoTimer) {
      clearTimeout(this.undoTimer);
      this.undoTimer = null;
    }
    this.undoSnapshot.set(null);
  }

  // ── Notes inline editor (row-level) ──────────────────────────
  startNotesEdit(v: Version) {
    this.editingNotesId.set(v.id);
    this.notesDraft.set(v.notes ?? '');
  }

  saveNotes(v: Version) {
    if (this.editingNotesId() !== v.id) return;
    const value = this.notesDraft().trim();
    this.versions.update((list) =>
      list.map((x) => (x.id === v.id ? { ...x, notes: value || undefined } : x)),
    );
    this.editingNotesId.set(null);
    this.notesDraft.set('');
  }

  cancelNotesEdit() {
    this.editingNotesId.set(null);
    this.notesDraft.set('');
  }
}

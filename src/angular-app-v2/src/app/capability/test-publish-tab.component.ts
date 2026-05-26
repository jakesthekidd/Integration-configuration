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
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';

import { GeneralService } from '../services/general.service';
import { Deployment } from '../models/deployment.model';
import { Version, VersionState } from '../models/version.model';
import { mockVersions } from '../mocks/mock-data';
import { LifecycleChevronComponent } from './lifecycle-chevron.component';
import { AutofocusDirective } from './autofocus.directive';

type ChevronStage = 'Draft' | 'Published' | 'Activated';

/**
 * Status tab — version stack home for one deployment.
 *
 * Implements DESIGN-STATUS-VERSIONING.md. Every deployment has a stack of
 * versions:
 *
 *   Drafts (newest first, expanded)
 *   ─ Published (if any, expanded)
 *   ─ Activated (one, expanded, with "+ New draft from this")
 *   ─ Archived (collapsed by default, history)
 *
 * The chevron on each container is the primary lifecycle control. Click
 * advance → confirm → commit. Secondary actions (Archive, Rollback,
 * Duplicate, Delete) live in each container's ⋯ Manage menu.
 */
@Component({
  selector: 'app-test-publish-tab',
  imports: [CommonModule, FormsModule, ButtonModule, MenuModule, LifecycleChevronComponent, AutofocusDirective],
  template: `
    <div class="versions">
      <!-- ── Drafts ───────────────────────────────────────────────── -->
      @for (v of draftVersions(); track v.id) {
        <article class="version" data-state="Draft">
          <header class="version__header">
            <div class="version__id">
              <span class="version__chip" data-state="Draft">Draft</span>
              <strong>v{{ v.versionNumber }}</strong>
              <span class="version__meta">
                Created {{ v.createdAt | date: 'MMM d, y \\'·\\' h:mm a' }} · {{ v.createdBy }}
              </span>
            </div>
            <p-button
              icon="pi pi-ellipsis-h"
              severity="secondary"
              [text]="true"
              [rounded]="true"
              size="small"
              ariaLabel="Manage version"
              (onClick)="openMenu(v.id, $event, sharedMenu)"
            />
          </header>
          <div class="version__body">
            @if (editingNotesId() === v.id) {
              <textarea
                class="version__notes-edit"
                rows="2"
                appAutofocus
                [ngModel]="notesDraft()"
                (ngModelChange)="notesDraft.set($event)"
                (blur)="saveNotes(v)"
                (keydown.enter)="onNotesEnter($event, v)"
                (keydown.escape)="cancelNotesEdit()"
                placeholder="Describe this version — what changed, why, who reviewed it…"
              ></textarea>
            } @else if (v.notes) {
              <button
                type="button"
                class="version__notes"
                (click)="startNotesEdit(v)"
                aria-label="Edit description"
              >
                <span>{{ v.notes }}</span>
                <i class="pi pi-pencil version__notes-pencil" aria-hidden="true"></i>
              </button>
            } @else {
              <button
                type="button"
                class="version__notes-add"
                (click)="startNotesEdit(v)"
              >
                <i class="pi pi-plus" aria-hidden="true"></i>
                Add description
              </button>
            }
            <app-lifecycle-chevron
              [current]="v.state"
              (advance)="onAdvance(v, $event)"
            />
          </div>
        </article>
      }

      <!-- ── Published (the in-between, if any) ────────────────────── -->
      @for (v of publishedVersions(); track v.id) {
        <article class="version" data-state="Published">
          <header class="version__header">
            <div class="version__id">
              <span class="version__chip" data-state="Published">Published</span>
              <strong>v{{ v.versionNumber }}</strong>
              <span class="version__meta">
                Published {{ v.publishedAt | date: 'MMM d, y' }} · {{ v.createdBy }}
              </span>
            </div>
            <p-button
              icon="pi pi-ellipsis-h"
              severity="secondary"
              [text]="true"
              [rounded]="true"
              size="small"
              ariaLabel="Manage version"
              (onClick)="openMenu(v.id, $event, sharedMenu)"
            />
          </header>
          <div class="version__body">
            @if (editingNotesId() === v.id) {
              <textarea
                class="version__notes-edit"
                rows="2"
                appAutofocus
                [ngModel]="notesDraft()"
                (ngModelChange)="notesDraft.set($event)"
                (blur)="saveNotes(v)"
                (keydown.enter)="onNotesEnter($event, v)"
                (keydown.escape)="cancelNotesEdit()"
                placeholder="Describe this version — what changed, why, who reviewed it…"
              ></textarea>
            } @else if (v.notes) {
              <button
                type="button"
                class="version__notes"
                (click)="startNotesEdit(v)"
                aria-label="Edit description"
              >
                <span>{{ v.notes }}</span>
                <i class="pi pi-pencil version__notes-pencil" aria-hidden="true"></i>
              </button>
            } @else {
              <button
                type="button"
                class="version__notes-add"
                (click)="startNotesEdit(v)"
              >
                <i class="pi pi-plus" aria-hidden="true"></i>
                Add description
              </button>
            }
            <app-lifecycle-chevron
              [current]="v.state"
              (advance)="onAdvance(v, $event)"
            />
          </div>
        </article>
      }

      <!-- ── Activated (zero or one) ───────────────────────────────── -->
      @if (activatedVersion(); as v) {
        <article class="version version--activated" data-state="Activated">
          <header class="version__header">
            <div class="version__id">
              <span class="version__chip" data-state="Activated">Activated</span>
              <strong>v{{ v.versionNumber }}</strong>
              <span class="version__meta">
                Active since {{ v.activatedAt | date: 'MMM d, y' }} · {{ v.createdBy }}
              </span>
            </div>
            <p-button
              icon="pi pi-ellipsis-h"
              severity="secondary"
              [text]="true"
              [rounded]="true"
              size="small"
              ariaLabel="Manage version"
              (onClick)="openMenu(v.id, $event, sharedMenu)"
            />
          </header>
          <div class="version__body">
            @if (editingNotesId() === v.id) {
              <textarea
                class="version__notes-edit"
                rows="2"
                appAutofocus
                [ngModel]="notesDraft()"
                (ngModelChange)="notesDraft.set($event)"
                (blur)="saveNotes(v)"
                (keydown.enter)="onNotesEnter($event, v)"
                (keydown.escape)="cancelNotesEdit()"
                placeholder="Describe this version — what changed, why, who reviewed it…"
              ></textarea>
            } @else if (v.notes) {
              <button
                type="button"
                class="version__notes"
                (click)="startNotesEdit(v)"
                aria-label="Edit description"
              >
                <span>{{ v.notes }}</span>
                <i class="pi pi-pencil version__notes-pencil" aria-hidden="true"></i>
              </button>
            } @else {
              <button
                type="button"
                class="version__notes-add"
                (click)="startNotesEdit(v)"
              >
                <i class="pi pi-plus" aria-hidden="true"></i>
                Add description
              </button>
            }
            <app-lifecycle-chevron [current]="v.state" [inert]="true" />
            <div class="version__activated-actions">
              <p-button
                label="+ New draft from this"
                icon="pi pi-plus"
                severity="primary"
                [outlined]="true"
                size="small"
                (onClick)="forkNewDraft(v)"
              />
            </div>
          </div>
        </article>
      }

      <!-- ── Archived (history, collapsed) ─────────────────────────── -->
      @if (archivedVersions().length > 0) {
        <div class="archived-header">History</div>
      }
      @for (v of archivedVersions(); track v.id) {
        <article
          class="version version--archived"
          data-state="Archived"
          [class.version--collapsed]="!isExpanded(v.id)"
        >
          <header class="version__header version__header--clickable" (click)="toggleExpanded(v.id)">
            <div class="version__id">
              <i
                class="pi"
                [class.pi-chevron-right]="!isExpanded(v.id)"
                [class.pi-chevron-down]="isExpanded(v.id)"
                aria-hidden="true"
              ></i>
              <span class="version__chip" data-state="Archived">Archived</span>
              <strong>v{{ v.versionNumber }}</strong>
              <span class="version__meta">
                Archived {{ v.archivedAt | date: 'MMM d, y' }} · {{ v.createdBy }}
              </span>
            </div>
            <p-button
              icon="pi pi-ellipsis-h"
              severity="secondary"
              [text]="true"
              [rounded]="true"
              size="small"
              ariaLabel="Manage version"
              (onClick)="openMenu(v.id, $event, sharedMenu); $event.stopPropagation()"
            />
          </header>
          @if (isExpanded(v.id)) {
            <div class="version__body">
              @if (v.notes) {
                <p class="version__notes">{{ v.notes }}</p>
              }
              <app-lifecycle-chevron [current]="v.state" [inert]="true" />
              <div class="version__lineage">
                Published {{ v.publishedAt | date: 'MMM d, y' }}
                @if (v.activatedAt) {
                  · Was Active {{ v.activatedAt | date: 'MMM d' }} – {{ v.archivedAt | date: 'MMM d, y' }}
                }
              </div>
            </div>
          }
        </article>
      }

      <!-- Single shared p-menu — model is rebuilt per-version on toggle -->
      <p-menu #sharedMenu [model]="currentMenuItems()" [popup]="true" appendTo="body" />
    </div>
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-4);
      }

      .versions {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-3);
      }

      /* ── Version container ──────────────────────────────────────── */
      .version {
        background: white;
        border: 1px solid var(--tf-slate-300);
        border-radius: var(--tf-radius-md);
        padding: var(--tf-space-4) var(--tf-space-5);
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-3);
      }
      /* Containers use white background — the chevron bar is the visual
         hero. Borders pick up a faint state tint so containers still
         feel grouped at a glance without competing with the chevron. */
      .version[data-state='Draft'] {
        border-color: #f5cf94;
      }
      .version[data-state='Published'] {
        border-color: #a3d9b1;
      }
      .version[data-state='Activated'] {
        border-color: #1b6b3a;
        border-width: 1px;
        box-shadow: 0 1px 3px rgba(27, 107, 58, 0.08);
      }
      .version[data-state='Archived'] {
        border-color: var(--tf-slate-300);
      }
      .version--collapsed {
        padding: var(--tf-space-3) var(--tf-space-5);
      }
      .version--collapsed .version__body {
        display: none;
      }

      .version__header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--tf-space-3);
      }
      .version__header--clickable {
        cursor: pointer;
      }
      .version__id {
        display: flex;
        align-items: center;
        gap: 10px;
        font-size: var(--tf-text-body);
        color: var(--tf-text-strong);
      }
      .version__id strong {
        font-weight: 700;
      }
      .version__meta {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-meta);
        font-weight: 400;
      }

      .version__chip {
        display: inline-flex;
        align-items: center;
        padding: 2px 10px;
        border-radius: var(--tf-radius-pill);
        font-size: 11px;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.4px;
      }
      .version__chip[data-state='Draft'] {
        background: #fff6e5;
        color: #92510a;
      }
      .version__chip[data-state='Published'] {
        background: #e6f0fb;
        color: #1d4e89;
      }
      .version__chip[data-state='Activated'] {
        background: #e5f9ea;
        color: #1b6b3a;
      }
      .version__chip[data-state='Archived'] {
        background: var(--tf-slate-200);
        color: var(--tf-text-muted);
      }

      .version__body {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-3);
        padding-left: 2px;
      }
      /* Click-to-edit notes — rendered as a button so it's keyboard reachable. */
      .version__notes {
        margin: 0;
        padding: 6px 8px;
        background: transparent;
        border: 1px dashed transparent;
        border-radius: var(--tf-radius-sm);
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        font-style: italic;
        font-family: inherit;
        text-align: left;
        max-width: 70ch;
        cursor: text;
        display: inline-flex;
        align-items: center;
        gap: 8px;
        transition: background 0.12s ease, border-color 0.12s ease;
      }
      .version__notes:hover,
      .version__notes:focus-visible {
        background: var(--tf-slate-50, #f8fafc);
        border-color: var(--tf-slate-300, #cbd5e1);
        outline: 0;
      }
      .version__notes-pencil {
        font-size: 11px;
        opacity: 0;
        transition: opacity 0.12s ease;
      }
      .version__notes:hover .version__notes-pencil,
      .version__notes:focus-visible .version__notes-pencil {
        opacity: 0.6;
      }

      .version__notes-add {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        background: transparent;
        border: 1px dashed var(--tf-slate-300, #cbd5e1);
        border-radius: var(--tf-radius-sm);
        color: var(--tf-text-muted);
        font-size: var(--tf-text-meta);
        font-weight: 500;
        font-family: inherit;
        padding: 6px 10px;
        cursor: pointer;
        align-self: flex-start;
      }
      .version__notes-add:hover,
      .version__notes-add:focus-visible {
        background: var(--tf-slate-50, #f8fafc);
        color: var(--tf-text-strong);
        border-color: var(--tf-slate-400, #94a3b8);
        outline: 0;
      }
      .version__notes-add i {
        font-size: 10px;
      }

      .version__notes-edit {
        width: 100%;
        max-width: 70ch;
        padding: 8px 10px;
        border: 1px solid var(--tf-blue-400, #5b9bd5);
        border-radius: var(--tf-radius-sm);
        background: white;
        color: var(--tf-text-strong);
        font-family: inherit;
        font-size: var(--tf-text-body);
        font-style: italic;
        line-height: 1.5;
        resize: vertical;
        min-height: 50px;
      }
      .version__notes-edit:focus-visible {
        outline: 2px solid #1d6fc0;
        outline-offset: -1px;
        border-color: #1d6fc0;
      }
      .version__activated-actions {
        margin-top: 4px;
      }
      .version__lineage {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-meta);
        font-style: italic;
      }

      .archived-header {
        margin-top: var(--tf-space-3);
        font-size: var(--tf-text-meta);
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.5px;
        color: var(--tf-text-muted);
        padding: 0 var(--tf-space-2);
      }

      /* PrimeNG menu danger item styling */
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
  /** Customer display name — used to make confirm copy specific
   *  ("apply the new mapping to {customerName}'s integration"). */
  @Input() customerName = 'this customer';
  @Output() statusChanged = new EventEmitter<void>();

  private gen = inject(GeneralService);

  /** Local version stack — seeded from mockVersions per deployment, then
   *  mutated locally as the user works. Lives in component state so the
   *  parent's rail-refresh logic doesn't reset the demo. */
  versions = signal<Version[]>([]);

  /** Which archived versions are expanded by the user. */
  private expandedSet = signal<Set<string>>(new Set());

  /** Per-version action busy state. */
  busyAction = signal<{ versionId: string; action: string } | null>(null);

  /** Inline notes editor state — which version is currently being edited
   *  and the in-progress draft text. Editable on every version per Q-set. */
  editingNotesId = signal<string | null>(null);
  notesDraft = signal<string>('');

  /** Author display name for newly forked drafts. */
  private currentAuthor = 'Jake Cummings';

  // ── Bucketed views (sorted within each group) ────────────────────
  draftVersions = computed<Version[]>(() =>
    this.versions()
      .filter((v) => v.state === 'Draft')
      .sort((a, b) => b.versionNumber - a.versionNumber),
  );

  publishedVersions = computed<Version[]>(() =>
    this.versions()
      .filter((v) => v.state === 'Published')
      .sort((a, b) => b.versionNumber - a.versionNumber),
  );

  activatedVersion = computed<Version | null>(
    () => this.versions().find((v) => v.state === 'Activated') ?? null,
  );

  archivedVersions = computed<Version[]>(() =>
    this.versions()
      .filter((v) => v.state === 'Archived')
      .sort((a, b) => b.versionNumber - a.versionNumber),
  );

  // ── Manage menu (one shared p-menu, model rebuilt per click) ─────
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
    if (v.state === 'Activated') {
      items.push({
        label: 'Archive this version',
        icon: 'pi pi-pause',
        styleClass: 'menu-item--danger',
        command: () => this.archive(v),
      });
    }
    if (v.state === 'Archived') {
      items.push({
        label: 'Rollback (reactivate)',
        icon: 'pi pi-undo',
        command: () => this.rollback(v),
      });
    }
    items.push({
      label: 'Duplicate as new draft',
      icon: 'pi pi-copy',
      command: () => this.duplicate(v),
    });
    if (v.state === 'Draft') {
      items.push({ separator: true });
      items.push({
        label: 'Delete draft',
        icon: 'pi pi-trash',
        styleClass: 'menu-item--danger',
        command: () => this.deleteDraft(v),
      });
    }
    return items;
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['deployment']) {
      const id = this.deployment?.id;
      // Deep-clone the seed so per-deployment mutations don't bleed back.
      const seed = id ? mockVersions[id] ?? [] : [];
      this.versions.set(seed.map((v) => ({ ...v })));
      this.expandedSet.set(new Set());
      this.busyAction.set(null);
      this.menuVersionId.set(null);
      this.editingNotesId.set(null);
      this.notesDraft.set('');
    }
  }

  // ── Notes inline editor ──────────────────────────────────────────
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

  /** Enter saves; Shift+Enter inserts a newline (default browser behavior). */
  onNotesEnter(event: Event, v: Version) {
    const kb = event as KeyboardEvent;
    if (kb.shiftKey) return;
    event.preventDefault();
    this.saveNotes(v);
  }

  // ── Helpers used in the template ─────────────────────────────────
  isExpanded(versionId: string): boolean {
    return this.expandedSet().has(versionId);
  }

  toggleExpanded(versionId: string) {
    this.expandedSet.update((s) => {
      const next = new Set(s);
      if (next.has(versionId)) next.delete(versionId);
      else next.add(versionId);
      return next;
    });
  }

  /** Bound to the per-row "⋯" button — sets which version's menu items
   *  to render, then toggles the shared p-menu. The menu reference is
   *  passed in directly from the template via #sharedMenu. */
  openMenu(versionId: string, event: Event, menu: { toggle: (e: Event) => void }) {
    this.menuVersionId.set(versionId);
    menu.toggle(event);
  }

  // ── Lifecycle transitions ────────────────────────────────────────
  /** Chevron advance click. */
  onAdvance(v: Version, target: ChevronStage) {
    if (target === 'Published') {
      this.publishDraft(v);
    } else if (target === 'Activated') {
      this.activatePublished(v);
    }
  }

  /** Draft → Published. */
  private publishDraft(v: Version) {
    if (this.busyAction()) return;
    this.gen
      .confirm({
        title: `Publish v${v.versionNumber}?`,
        text:
          'This locks the current Connection + Mapping into a versioned snapshot. The version is not ' +
          'live yet — activate it next when you are ready.',
        confirmText: 'Yes, publish',
        confirmColor: '#1d6fc0',
        icon: 'info',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.busyAction.set({ versionId: v.id, action: 'publish' });
        setTimeout(() => {
          this.versions.update((list) =>
            list.map((x) =>
              x.id === v.id
                ? { ...x, state: 'Published' as VersionState, publishedAt: new Date().toISOString() }
                : x,
            ),
          );
          this.busyAction.set(null);
          this.gen.success(`v${v.versionNumber} published.`);
          this.statusChanged.emit();
        }, 500);
      });
  }

  /** Published → Activated. Auto-archives any prior Activated. */
  private activatePublished(v: Version) {
    if (this.busyAction()) return;
    const currentActive = this.activatedVersion();
    const archiveCopy = currentActive
      ? `Activating v${v.versionNumber} will archive v${currentActive.versionNumber} and apply the new mapping to ${this.customerName}'s integration. Continue?`
      : `Activating v${v.versionNumber} will apply the new mapping to ${this.customerName}'s integration. Continue?`;

    this.gen
      .confirm({
        title: `Activate v${v.versionNumber}?`,
        text: archiveCopy,
        confirmText: 'Yes, activate',
        confirmColor: '#28a745',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.busyAction.set({ versionId: v.id, action: 'activate' });
        setTimeout(() => {
          const now = new Date().toISOString();
          this.versions.update((list) =>
            list.map((x) => {
              if (x.id === v.id) {
                return { ...x, state: 'Activated' as VersionState, activatedAt: now };
              }
              if (x.state === 'Activated') {
                return { ...x, state: 'Archived' as VersionState, archivedAt: now };
              }
              return x;
            }),
          );
          this.busyAction.set(null);
          this.gen.success(`v${v.versionNumber} is live.`);
          this.statusChanged.emit();
        }, 600);
      });
  }

  /** Activated → Archived (manual archive via Manage menu). */
  archive(v: Version) {
    if (this.busyAction()) return;
    this.gen
      .confirm({
        title: `Archive v${v.versionNumber}?`,
        text: `This deployment goes offline until another version is activated. ${this.customerName}'s integration stops processing traffic immediately.`,
        confirmText: 'Yes, archive',
        confirmColor: '#83131a',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.busyAction.set({ versionId: v.id, action: 'archive' });
        setTimeout(() => {
          const now = new Date().toISOString();
          this.versions.update((list) =>
            list.map((x) => (x.id === v.id ? { ...x, state: 'Archived' as VersionState, archivedAt: now } : x)),
          );
          this.busyAction.set(null);
          this.gen.success(`v${v.versionNumber} archived.`);
          this.statusChanged.emit();
        }, 500);
      });
  }

  /** Archived → Activated. Auto-archives the currently-Activated version. */
  rollback(v: Version) {
    if (this.busyAction()) return;
    const currentActive = this.activatedVersion();
    const text = currentActive
      ? `Reactivating v${v.versionNumber} will archive v${currentActive.versionNumber} and apply v${v.versionNumber}'s mapping to ${this.customerName}'s integration immediately. Continue?`
      : `Reactivating v${v.versionNumber} will apply its mapping to ${this.customerName}'s integration immediately. Continue?`;

    this.gen
      .confirm({
        title: `Rollback to v${v.versionNumber}?`,
        text,
        confirmText: 'Yes, rollback',
        confirmColor: '#1d6fc0',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.busyAction.set({ versionId: v.id, action: 'rollback' });
        setTimeout(() => {
          const now = new Date().toISOString();
          this.versions.update((list) =>
            list.map((x) => {
              if (x.id === v.id) {
                return { ...x, state: 'Activated' as VersionState, activatedAt: now, archivedAt: undefined };
              }
              if (x.state === 'Activated') {
                return { ...x, state: 'Archived' as VersionState, archivedAt: now };
              }
              return x;
            }),
          );
          this.busyAction.set(null);
          this.gen.success(`Rolled back to v${v.versionNumber}.`);
          this.statusChanged.emit();
        }, 600);
      });
  }

  /** Create a new Draft seeded with this version's notes/config. */
  duplicate(v: Version) {
    if (this.busyAction()) return;
    const next = this.nextVersionNumber();
    const draft: Version = {
      id: `${v.deploymentId}-ver-${next}-${Date.now()}`,
      deploymentId: v.deploymentId,
      versionNumber: next,
      state: 'Draft',
      createdAt: new Date().toISOString(),
      createdBy: this.currentAuthor,
      notes: v.notes ? `Duplicated from v${v.versionNumber} — ${v.notes}` : `Duplicated from v${v.versionNumber}`,
    };
    this.versions.update((list) => [draft, ...list]);
    this.gen.success(`v${next} created from v${v.versionNumber}.`);
  }

  /** Activated container "+ New draft from this" button. */
  forkNewDraft(v: Version) {
    if (this.busyAction()) return;
    const next = this.nextVersionNumber();
    const draft: Version = {
      id: `${v.deploymentId}-ver-${next}-${Date.now()}`,
      deploymentId: v.deploymentId,
      versionNumber: next,
      state: 'Draft',
      createdAt: new Date().toISOString(),
      createdBy: this.currentAuthor,
      notes: `Forked from v${v.versionNumber} for new changes`,
    };
    this.versions.update((list) => [draft, ...list]);
    this.gen.success(`v${next} draft created. Edit Connection or Mapping, then publish.`);
  }

  /** Permanently remove an unpublished draft. */
  deleteDraft(v: Version) {
    if (this.busyAction()) return;
    if (v.state !== 'Draft') return;
    this.gen
      .confirm({
        title: `Delete v${v.versionNumber} draft?`,
        text: 'The draft and any pending edits will be permanently removed. This cannot be undone.',
        confirmText: 'Yes, delete',
        confirmColor: '#83131a',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;
        this.versions.update((list) => list.filter((x) => x.id !== v.id));
        this.gen.success(`v${v.versionNumber} deleted.`);
      });
  }

  private nextVersionNumber(): number {
    const max = this.versions().reduce((acc, v) => Math.max(acc, v.versionNumber), 0);
    return max + 1;
  }

}

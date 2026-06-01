import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { forkJoin } from 'rxjs';

import { ApiService } from '../services/api.service';
import { DraftService } from '../services/draft.service';
import { Customer } from '../models/customer.model';
import { Application } from '../models/application.model';
import { Capability } from '../models/capability.model';
import { TmsSystem } from '../models/tms-system.model';
import { Deployment, DeploymentStatus } from '../models/deployment.model';
import { ConnectionTabComponent } from '../capability/connection-tab.component';
import { MappingTabComponent } from '../capability/mapping-tab.component';
import { TestPublishTabComponent } from '../capability/test-publish-tab.component';
import { ActivityTabComponent } from '../capability/activity-tab.component';
import { AddDeploymentDialogComponent } from '../capability/add-deployment-dialog.component';

type CapabilityTab = 'connection' | 'mapping' | 'publish-activate' | 'activity';

interface CapabilityNode {
  deploymentId: string;
  capabilityId: string;
  capabilityName: string;
  connectionName: string;
  status: DeploymentStatus;
  snapshotVersion: number;
}

interface AppGroup {
  applicationId: string;
  applicationName: string;
  expanded: boolean;
  capabilities: CapabilityNode[];
}

/**
 * Customer Detail — the tree view + right pane that replaces the wizard.
 *
 * Per PRODUCT-GUIDING-PRINCIPLES.md §6 this is the workhorse screen for
 * per-customer deployment work. Left rail navigates applications →
 * capabilities; right pane edits the selected capability across four tabs
 * (Connection, Mapping, Test & Publish, Activity).
 *
 * Phase 2: shell only — rail navigation + placeholder right pane.
 * Phase 3 will add the four real tabs.
 */
@Component({
  selector: 'app-customer-detail',
  imports: [
    ButtonModule,
    TagModule,
    ConnectionTabComponent,
    MappingTabComponent,
    TestPublishTabComponent,
    ActivityTabComponent,
    AddDeploymentDialogComponent,
  ],
  template: `
    <div class="tf-section-header">
      <div class="header-left">
        <button class="back" (click)="back()" aria-label="Back to all customers">
          <i class="pi pi-arrow-left"></i>
        </button>
        <div>
          <h2>{{ customer()?.customerName ?? '…' }}</h2>
          @if (customer(); as c) {
            <p>
              {{ c.tmsName || 'No connection on file' }} ·
              <span [class.status-active]="c.enabled" [class.status-inactive]="!c.enabled">
                {{ c.enabled ? 'Active' : 'Inactive' }}
              </span>
            </p>
          }
        </div>
      </div>
    </div>

    @if (loading()) {
      <div class="loading-shade">Loading deployments…</div>
    } @else if (groups().length === 0) {
      <!-- Whole-page empty state — no deployments yet for this customer -->
      <div class="empty-page">
        <div class="empty-page__inner">
          <i class="pi pi-inbox" aria-hidden="true"></i>
          <h3>No integrations yet</h3>
          <p class="muted">
            Add this customer's first integration to start configuring connections, mappings,
            and deploys.
          </p>
          <p-button
            label="Add your first integration"
            icon="pi pi-plus"
            severity="primary"
            size="small"
            (onClick)="addApplication()"
          />
        </div>
      </div>
    } @else {
      <!-- Tree view + right pane -->
      <div class="layout">
        <!-- LEFT RAIL -->
        <aside class="rail">
          <div class="rail__head">
            <span class="rail__heading">Applications</span>
            <p-button
              icon="pi pi-plus"
              size="small"
              severity="secondary"
              [text]="true"
              [rounded]="true"
              pTooltip="Add application"
              (onClick)="addApplication()"
              aria-label="Add application"
            />
          </div>

          <nav class="rail__tree">
            @for (g of groups(); track g.applicationId) {
              <div class="app-group">
                <button
                  type="button"
                  class="app-group__head"
                  (click)="toggleGroup(g.applicationId)"
                >
                  <i
                    class="pi"
                    [class.pi-chevron-down]="g.expanded"
                    [class.pi-chevron-right]="!g.expanded"
                    aria-hidden="true"
                  ></i>
                  <span class="app-group__name">{{ g.applicationName }}</span>
                  <span class="app-group__count">{{ g.capabilities.length }}</span>
                </button>

                @if (g.expanded) {
                  <ul class="cap-list">
                    @for (cap of g.capabilities; track cap.deploymentId) {
                      <li>
                        <button
                          type="button"
                          class="cap-node"
                          [class.cap-node--selected]="cap.deploymentId === selectedDeploymentId()"
                          (click)="selectCapability(cap.deploymentId)"
                        >
                          <span class="cap-node__dot" [attr.data-status]="cap.status"></span>
                          <span class="cap-node__name">{{ cap.capabilityName }}</span>
                          <p-tag
                            [value]="cap.status"
                            [severity]="statusSeverity(cap.status)"
                            [rounded]="true"
                          />
                        </button>
                      </li>
                    }
                    <li class="cap-add">
                      <button type="button" class="cap-add__btn" (click)="addCapability(g.applicationId)">
                        <i class="pi pi-plus"></i>
                        Add capability
                      </button>
                    </li>
                  </ul>
                }
              </div>
            }
          </nav>
        </aside>

        <!-- RIGHT PANE -->
        <main class="pane">
          @if (selectedNode(); as node) {
            <header class="pane__head">
              <div>
                <h3>{{ node.capabilityName }}</h3>
                <p class="muted">
                  {{ appNameFor(node.deploymentId) }} · {{ node.connectionName }} ·
                  <span class="version">v{{ node.snapshotVersion }}</span>
                </p>
              </div>
              <p-tag
                [value]="node.status"
                [severity]="statusSeverity(node.status)"
                [rounded]="true"
              />
            </header>

            <div class="tab-strip">
              <button
                class="tab"
                type="button"
                [class.tab--active]="activeTab() === 'connection'"
                (click)="selectTab('connection')"
              >
                Connection
              </button>
              <button
                class="tab"
                type="button"
                [class.tab--active]="activeTab() === 'mapping'"
                (click)="selectTab('mapping')"
              >
                Mapping
              </button>
              <button
                class="tab"
                type="button"
                [class.tab--active]="activeTab() === 'publish-activate'"
                (click)="selectTab('publish-activate')"
              >
                Publish &amp; Activate
                @if (hasDraftForSelected()) {
                  <span class="tab__dot" title="Unsaved draft exists"></span>
                }
              </button>
              <button
                class="tab"
                type="button"
                [class.tab--active]="activeTab() === 'activity'"
                (click)="selectTab('activity')"
              >
                Activity
              </button>
            </div>
            <div class="tab-body">
              @if (hasDraftForSelected() && activeTab() !== 'publish-activate') {
                <div class="draft-cross-tab-banner">
                  <i class="pi pi-exclamation-triangle"></i>
                  <span>
                    This capability has an unsaved draft. Publish it to make the
                    changes available for activation.
                  </span>
                  <button
                    type="button"
                    class="draft-cross-tab-banner__link"
                    (click)="selectTab('publish-activate')"
                  >
                    Review draft
                  </button>
                </div>
              }
              @if (selectedDeployment(); as dep) {
                @switch (activeTab()) {
                  @case ('connection') {
                    <app-connection-tab [deployment]="dep" (saved)="onDeploymentChanged()" />
                  }
                  @case ('mapping') {
                    <app-mapping-tab [deployment]="dep" (saved)="onDeploymentChanged()" />
                  }
                  @case ('publish-activate') {
                    <app-test-publish-tab
                      [deployment]="dep"
                      [customerName]="customer()?.customerName ?? 'this customer'"
                      (statusChanged)="onDeploymentChanged()"
                    />
                  }
                  @case ('activity') {
                    <app-activity-tab [deployment]="dep" />
                  }
                }
              }
            </div>
          } @else {
            <div class="pane-empty">
              <i class="pi pi-arrow-left" aria-hidden="true"></i>
              <p>Select a capability from the left to configure it.</p>
            </div>
          }
        </main>
      </div>
    }

    <!-- Add-deployment dialog (used by both rail buttons) -->
    <app-add-deployment-dialog
      [(visible)]="dialogOpen"
      [customerId]="customerId()"
      [applications]="applications()"
      [capabilities]="capabilities()"
      [existingDeployments]="deployments()"
      [preSelectedAppId]="dialogPreSelectedApp()"
      (created)="onDeploymentCreated($event)"
    />
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        height: 100%;
        min-height: 0;
      }

      .header-left {
        display: flex;
        align-items: center;
        gap: var(--tf-space-3);
      }
      .back {
        width: 32px;
        height: 32px;
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-sm);
        background: white;
        color: var(--tf-text-muted);
        cursor: pointer;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
      }
      .back:hover {
        background: var(--tf-slate-100);
        color: var(--tf-text-strong);
      }
      .status-active {
        color: var(--tf-green-500);
        font-weight: 600;
      }
      .status-inactive {
        color: var(--tf-text-muted);
      }

      .loading-shade {
        flex: 1 1 auto;
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--tf-text-muted);
        padding: var(--tf-space-6);
      }

      /* ─── Whole-page empty state ───────────────────────────── */
      .empty-page {
        flex: 1 1 auto;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: var(--tf-space-8);
      }
      .empty-page__inner {
        max-width: 420px;
        text-align: center;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: var(--tf-space-3);
      }
      .empty-page__inner i {
        font-size: 40px;
        color: var(--tf-slate-500);
      }
      .empty-page__inner h3 {
        margin: 0;
        font-size: var(--tf-text-heading);
      }
      .muted {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin: 0;
      }

      /* ─── Tree + pane layout ───────────────────────────────── */
      .layout {
        flex: 1 1 auto;
        min-height: 0;
        display: grid;
        grid-template-columns: 280px 1fr;
        overflow: hidden;
      }

      .rail {
        border-right: 1px solid var(--tf-slate-400);
        background: var(--tf-slate-50);
        overflow-y: auto;
        display: flex;
        flex-direction: column;
      }
      .rail__head {
        position: sticky;
        top: 0;
        background: var(--tf-slate-50);
        border-bottom: 1px solid var(--tf-slate-300);
        padding: var(--tf-space-3) var(--tf-space-3) var(--tf-space-2) var(--tf-space-4);
        display: flex;
        align-items: center;
        justify-content: space-between;
        z-index: 1;
      }
      .rail__heading {
        font-size: var(--tf-text-meta);
        font-weight: 700;
        letter-spacing: 0.4px;
        text-transform: uppercase;
        color: var(--tf-text-muted);
      }
      .rail__tree {
        padding: var(--tf-space-2) 0 var(--tf-space-4);
      }

      .app-group__head {
        width: 100%;
        background: transparent;
        border: 0;
        cursor: pointer;
        padding: var(--tf-space-2) var(--tf-space-4);
        display: flex;
        align-items: center;
        gap: var(--tf-space-2);
        font-family: inherit;
        font-size: var(--tf-text-body);
        font-weight: 600;
        color: var(--tf-text-strong);
        text-align: left;
      }
      .app-group__head:hover {
        background: var(--tf-slate-200);
      }
      .app-group__head .pi {
        font-size: 10px;
        color: var(--tf-text-muted);
        width: 12px;
      }
      .app-group__name {
        flex: 1 1 auto;
      }
      .app-group__count {
        font-size: var(--tf-text-meta);
        color: var(--tf-text-muted);
        font-weight: 600;
      }

      .cap-list {
        list-style: none;
        margin: 0;
        padding: 0 0 var(--tf-space-2) 0;
      }
      .cap-node {
        width: 100%;
        background: transparent;
        border: 0;
        cursor: pointer;
        padding: 6px var(--tf-space-4) 6px calc(var(--tf-space-4) + 22px);
        display: flex;
        align-items: center;
        gap: var(--tf-space-2);
        font-family: inherit;
        font-size: var(--tf-text-body);
        color: var(--tf-text-strong);
        text-align: left;
      }
      .cap-node:hover {
        background: var(--tf-slate-200);
      }
      .cap-node--selected {
        background: var(--tf-blue-100);
        font-weight: 600;
      }
      .cap-node--selected:hover {
        background: var(--tf-blue-100);
      }
      .cap-node__dot {
        width: 6px;
        height: 6px;
        border-radius: 50%;
        flex-shrink: 0;
        background: var(--tf-slate-500);
      }
      .cap-node__dot[data-status='Active'] {
        background: var(--tf-green-500);
      }
      .cap-node__dot[data-status='Tested'],
      .cap-node__dot[data-status='Published'] {
        background: var(--tf-blue-400);
      }
      .cap-node__dot[data-status='Draft'] {
        background: var(--tf-orange-500);
      }
      .cap-node__dot[data-status='Retired'] {
        background: var(--tf-slate-500);
      }
      .cap-node__name {
        flex: 1 1 auto;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }

      .cap-add__btn {
        width: 100%;
        background: transparent;
        border: 0;
        cursor: pointer;
        padding: 6px var(--tf-space-4) 6px calc(var(--tf-space-4) + 22px);
        display: flex;
        align-items: center;
        gap: 6px;
        font-family: inherit;
        font-size: var(--tf-text-meta);
        color: var(--tf-blue-500);
        font-weight: 600;
        text-align: left;
      }
      .cap-add__btn:hover {
        background: var(--tf-slate-200);
      }
      .cap-add__btn .pi {
        font-size: 10px;
      }

      /* ─── Right pane ───────────────────────────────────────── */
      .pane {
        overflow-y: auto;
        display: flex;
        flex-direction: column;
        background: white;
      }
      .pane__head {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: var(--tf-space-4);
        padding: var(--tf-space-4) var(--tf-space-6);
        border-bottom: 1px solid var(--tf-slate-300);
      }
      .pane__head h3 {
        margin: 0;
        font-size: var(--tf-text-heading);
      }
      .version {
        font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
        font-size: var(--tf-text-meta);
      }

      .tab-strip {
        display: flex;
        gap: 0;
        padding: 0 var(--tf-space-6);
        background: var(--tf-slate-50);
        border-bottom: 1px solid var(--tf-slate-300);
      }
      .tab {
        background: transparent;
        border: 0;
        border-bottom: 3px solid transparent;
        padding: var(--tf-space-3) var(--tf-space-4);
        font-family: inherit;
        font-size: var(--tf-text-body);
        font-weight: 600;
        color: var(--tf-text-muted);
        cursor: pointer;
      }
      .tab:hover {
        color: var(--tf-blue-500);
      }
      .tab--active {
        color: var(--tf-blue-500);
        border-bottom-color: var(--tf-blue-500);
      }
      .tab__dot {
        display: inline-block;
        width: 8px;
        height: 8px;
        margin-left: 6px;
        border-radius: 50%;
        background: #f1c40f;
        box-shadow: 0 0 0 2px rgba(241, 196, 15, 0.25);
      }

      .draft-cross-tab-banner {
        display: flex;
        align-items: center;
        gap: 8px;
        background: #fff8e1;
        border: 1px solid #f1c40f;
        color: #92510a;
        padding: 8px 14px;
        border-radius: var(--tf-radius-md);
        font-size: var(--tf-text-body);
        margin-bottom: var(--tf-space-4);
      }
      .draft-cross-tab-banner__link {
        margin-left: auto;
        background: none;
        border: 1px solid #92510a;
        color: #92510a;
        font-weight: 600;
        padding: 4px 12px;
        border-radius: var(--tf-radius-pill);
        cursor: pointer;
        font-size: var(--tf-text-meta);
      }
      .draft-cross-tab-banner__link:hover {
        background: #92510a;
        color: #fff;
      }

      .tab-body {
        padding: var(--tf-space-5) var(--tf-space-6);
        flex: 1 1 auto;
      }

      .pane-empty {
        flex: 1 1 auto;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: var(--tf-space-8);
        color: var(--tf-text-muted);
        gap: var(--tf-space-2);
      }
      .pane-empty i {
        font-size: 24px;
      }
    `,
  ],
})
export class CustomerDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = inject(ApiService);
  private draftService = inject(DraftService);

  /** Reactive: does the selected deployment have an uncommitted Draft? */
  hasDraftForSelected = computed(() => {
    const id = this.selectedDeployment()?.id;
    return !!id && this.draftService.hasDraft(id);
  });

  customerId = signal<string>('');
  customer = signal<Customer | null>(null);
  deployments = signal<Deployment[]>([]);
  applications = signal<Application[]>([]);
  capabilities = signal<Capability[]>([]);
  connections = signal<TmsSystem[]>([]);
  loading = signal<boolean>(true);

  /** Per-app expansion state, keyed by applicationId. Defaults to expanded. */
  private collapsedApps = signal<Set<string>>(new Set());

  /** Selected deployment id, synced with `?cap=` query param. */
  selectedDeploymentId = signal<string>('');

  /** Active capability tab, synced with `?tab=` query param. Defaults to connection. */
  activeTab = signal<CapabilityTab>('connection');

  /** Full Deployment object for the selected node, passed to each tab component. */
  selectedDeployment = computed<Deployment | null>(() => {
    const id = this.selectedDeploymentId();
    if (!id) return null;
    return this.deployments().find((d) => d.id === id) ?? null;
  });

  groups = computed<AppGroup[]>(() => {
    const apps = new Map(this.applications().map((a) => [a.id, a]));
    const caps = new Map(this.capabilities().map((c) => [c.id, c]));
    const conns = new Map(this.connections().map((c) => [c.id, c]));
    const collapsed = this.collapsedApps();

    const byApp = new Map<string, CapabilityNode[]>();
    for (const d of this.deployments()) {
      const node: CapabilityNode = {
        deploymentId: d.id,
        capabilityId: d.capabilityId,
        capabilityName: caps.get(d.capabilityId)?.displayName ?? d.capabilityId,
        connectionName: conns.get(d.connectionId)?.displayName ?? d.connectionId,
        status: d.status,
        snapshotVersion: d.snapshotVersion,
      };
      const list = byApp.get(d.applicationId) ?? [];
      list.push(node);
      byApp.set(d.applicationId, list);
    }

    return Array.from(byApp.entries())
      .map(([applicationId, capList]) => {
        // Number duplicate capabilities: if same capabilityId appears more than once, suffix them (1), (2)...
        const countById = new Map<string, number>();
        capList.forEach(n => countById.set(n.capabilityId, (countById.get(n.capabilityId) ?? 0) + 1));
        const indexById = new Map<string, number>();
        const numberedList = capList.map(n => {
          if ((countById.get(n.capabilityId) ?? 1) > 1) {
            const idx = (indexById.get(n.capabilityId) ?? 0) + 1;
            indexById.set(n.capabilityId, idx);
            return { ...n, capabilityName: `${n.capabilityName} (${idx})` };
          }
          return n;
        });
        return {
          applicationId,
          applicationName: apps.get(applicationId)?.displayName ?? applicationId,
          expanded: !collapsed.has(applicationId),
          capabilities: numberedList.sort((a, b) => a.capabilityName.localeCompare(b.capabilityName)),
        };
      })
      .sort((a, b) => a.applicationName.localeCompare(b.applicationName));
  });

  selectedNode = computed<CapabilityNode | null>(() => {
    const id = this.selectedDeploymentId();
    if (!id) return null;
    for (const g of this.groups()) {
      const found = g.capabilities.find((c) => c.deploymentId === id);
      if (found) return found;
    }
    return null;
  });

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.customerId.set(id);

    // Sync selection + active tab with query params so refresh + deep-link work.
    this.route.queryParamMap.subscribe((params) => {
      const cap = params.get('cap') ?? '';
      this.selectedDeploymentId.set(cap);

      const tab = params.get('tab') ?? 'connection';
      const valid: CapabilityTab[] = ['connection', 'mapping', 'publish-activate', 'activity'];
      this.activeTab.set((valid as string[]).includes(tab) ? (tab as CapabilityTab) : 'connection');
    });

    this.loadAll();
  }

  private loadAll() {
    const id = this.customerId();
    if (!id) {
      this.loading.set(false);
      return;
    }
    forkJoin({
      customer: this.api.getCustomerById(id),
      deployments: this.api.getDeployments(id),
      applications: this.api.getApplications(),
      capabilities: this.api.getCapabilities(),
      connections: this.api.getTmsSystems(true),
    }).subscribe((res) => {
      if (res.customer.success && res.customer.data) this.customer.set(res.customer.data);
      if (res.deployments.success && res.deployments.data) this.deployments.set(res.deployments.data.deployments);
      if (res.applications.success && res.applications.data) this.applications.set(res.applications.data.applications);
      if (res.capabilities.success && res.capabilities.data) this.capabilities.set(res.capabilities.data.capabilities);
      if (res.connections.success && res.connections.data) this.connections.set(res.connections.data.systems);
      this.loading.set(false);

      // Auto-select the first deployment if nothing's chosen and there are some.
      if (!this.selectedDeploymentId() && this.deployments().length > 0) {
        this.selectCapability(this.deployments()[0].id);
      }
    });
  }

  back() {
    this.router.navigate(['/customers']);
  }

  toggleGroup(applicationId: string) {
    this.collapsedApps.update((set) => {
      const next = new Set(set);
      if (next.has(applicationId)) next.delete(applicationId);
      else next.add(applicationId);
      return next;
    });
  }

  selectCapability(deploymentId: string) {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { cap: deploymentId },
      queryParamsHandling: 'merge',
    });
  }

  selectTab(tab: CapabilityTab) {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab },
      queryParamsHandling: 'merge',
    });
  }

  /** Called by a tab when it saves something that may have changed the deployment
   *  (status, snapshot version, connection, template). Refetches deployments. */
  onDeploymentChanged() {
    const id = this.customerId();
    if (!id) return;
    this.api.getDeployments(id).subscribe((res) => {
      if (res.success && res.data) this.deployments.set(res.data.deployments);
    });
  }

  // ─── Add-deployment dialog ────────────────────────────────────────────

  dialogOpen = false;
  dialogPreSelectedApp = signal<string | null>(null);

  /** Rail root button: pick an application first, then a capability. */
  addApplication() {
    this.dialogPreSelectedApp.set(null);
    this.dialogOpen = true;
  }

  /** Rail leaf button: skip the application picker, capability only. */
  addCapability(applicationId: string) {
    this.dialogPreSelectedApp.set(applicationId);
    this.dialogOpen = true;
  }

  /** Append the new deployment to local state, then select it. */
  onDeploymentCreated(d: Deployment) {
    this.deployments.update((list) => [...list, d]);
    // Make sure the new deployment's app group is expanded.
    this.collapsedApps.update((set) => {
      const next = new Set(set);
      next.delete(d.applicationId);
      return next;
    });
    // Select the new node and default to the Connection tab so the user can start filling it in.
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { cap: d.id, tab: 'connection' },
      queryParamsHandling: 'merge',
    });
  }

  appNameFor(deploymentId: string): string {
    const d = this.deployments().find((x) => x.id === deploymentId);
    if (!d) return '';
    return this.applications().find((a) => a.id === d.applicationId)?.displayName ?? '';
  }

  statusSeverity(s: DeploymentStatus): 'success' | 'info' | 'warn' | 'secondary' | 'danger' {
    switch (s) {
      case 'Active':
        return 'success';
      case 'Published':
      case 'Tested':
        return 'info';
      case 'Draft':
        return 'warn';
      case 'Retired':
      default:
        return 'secondary';
    }
  }
}

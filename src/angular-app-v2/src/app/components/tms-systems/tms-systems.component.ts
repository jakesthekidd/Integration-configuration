import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { MenuItem } from 'primeng/api';

import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { TmsSystem, CreateTmsSystemRequest } from '../../models/tms-system.model';
import { Application } from '../../models/application.model';
import { Capability } from '../../models/capability.model';
import { Customer } from '../../models/customer.model';
import { Deployment } from '../../models/deployment.model';

import { DataTableComponent, DataTableColumn } from '../../design-system/data-table.component';
import { RowActionsComponent } from '../../design-system/row-actions.component';
import { StatusTagComponent } from '../../design-system/status-tag.component';
import { SectionHeaderComponent } from '../../design-system/section-header.component';

interface ConnectionUsageSummary {
  applications: string[];
  capabilities: string[];
  totalActive: number;
  activeCustomers: string[];
}

/**
 * Integration Library → "Connections" tab.
 *
 * First screen migrated to the unified table stack: app-section-header +
 * app-data-table + app-status-tag + app-row-actions. Reference implementation
 * for the remaining migrations.
 */
@Component({
  selector: 'app-tms-systems',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DatePipe,
    ButtonModule,
    CheckboxModule,
    DataTableComponent,
    RowActionsComponent,
    StatusTagComponent,
    SectionHeaderComponent,
  ],
  template: `
    <section class="conn">
      <app-section-header
        title="Connections"
        subtitle="System-to-system connectors that capabilities use to move data in or out of a customer environment."
      >
        <label class="conn__filter">
          <input
            type="checkbox"
            [(ngModel)]="activeOnly"
            (change)="loadSystems()"
          />
          Show active only
        </label>
        <button
          pButton
          type="button"
          label="Create Connection"
          icon="pi pi-plus"
          severity="primary"
          (click)="showCreateForm = !showCreateForm"
        ></button>
      </app-section-header>

      @if (showCreateForm) {
        <div class="conn__form">
          <h3>Create Connection</h3>
          <form (ngSubmit)="createSystem()">
            <div class="conn__field">
              <label for="systemName">Name</label>
              <input id="systemName" type="text" [(ngModel)]="newSystem.name" name="name" required />
            </div>
            <div class="conn__field">
              <label for="displayName">Display Name</label>
              <input
                id="displayName"
                type="text"
                [(ngModel)]="newSystem.displayName"
                name="displayName"
                required
              />
            </div>
            <div class="conn__field">
              <label for="sysDescription">Description</label>
              <textarea
                id="sysDescription"
                [(ngModel)]="newSystem.description"
                name="description"
              ></textarea>
            </div>
            <div class="conn__field">
              <label for="sysVersion">Version</label>
              <input id="sysVersion" type="text" [(ngModel)]="newSystem.version" name="version" />
            </div>
            <button pButton type="submit" label="Create" severity="success"></button>
          </form>
        </div>
      }

      @if (error) {
        <div class="conn__error">{{ error }}</div>
      }

      <app-data-table
        [rows]="systems"
        [columns]="columns"
        [loading]="loading"
        dataKey="id"
        emptyIcon="pi-link"
        emptyHeading="No connections yet"
        emptyMessage="Create your first connection to wire a customer system to a capability."
      >
        <ng-template #row let-system>
          <tr>
            <td class="conn__name">{{ system.name }}</td>
            <td>{{ system.displayName }}</td>
            <td>{{ system.version }}</td>
            <td class="conn__usage">{{ applicationsFor(system.id) }}</td>
            <td class="conn__usage">{{ capabilitiesFor(system.id) }}</td>
            <td style="text-align: center">
              <span class="conn__active-wrap">
                <span
                  class="conn__active-count"
                  [class.conn__active-count--zero]="totalActiveFor(system.id) === 0"
                >
                  {{ totalActiveFor(system.id) }}
                </span>
                @if (activeCustomerList(system.id).length > 0) {
                  <div class="conn__active-tip">
                    <div class="conn__active-tip-header">Active customers</div>
                    @for (name of activeCustomerList(system.id); track name) {
                      <div class="conn__active-tip-row">• {{ name }}</div>
                    }
                  </div>
                }
              </span>
            </td>
            <td>
              <app-status-tag [status]="system.isActive ? 'Active' : 'Inactive'" />
            </td>
            <td class="conn__date">{{ system.createdAt | date: 'short' }}</td>
            <td style="text-align: center">
              <app-row-actions [items]="menuFor(system)" />
            </td>
          </tr>
        </ng-template>
      </app-data-table>
    </section>
  `,
  styles: [
    `
      :host {
        display: block;
        height: 100%;
        overflow: auto;
      }
      .conn {
        padding: 1.5rem 2rem 3rem;
        max-width: 1400px;
        margin: 0 auto;
      }
      .conn__filter {
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
        font-size: 0.875rem;
        color: #475569;
        cursor: pointer;
      }
      .conn__form {
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 8px;
        padding: 1rem 1.25rem;
        margin-bottom: 1rem;
      }
      .conn__form h3 {
        margin: 0 0 0.75rem;
        font-size: 1rem;
        font-weight: 600;
        color: #0f172a;
      }
      .conn__field {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        margin-bottom: 0.75rem;
      }
      .conn__field label {
        font-size: 0.8125rem;
        color: #475569;
        font-weight: 500;
      }
      .conn__field input,
      .conn__field textarea {
        padding: 0.45rem 0.6rem;
        border: 1px solid #cbd5e1;
        border-radius: 6px;
        font-size: 0.875rem;
      }
      .conn__error {
        background: #fef2f2;
        border: 1px solid #fecaca;
        color: #991b1b;
        padding: 0.6rem 1rem;
        border-radius: 8px;
        margin-bottom: 0.75rem;
        font-size: 0.875rem;
      }
      .conn__name {
        font-weight: 500;
      }
      .conn__usage {
        color: #475569;
      }
      .conn__date {
        color: #64748b;
        font-size: 0.8125rem;
        white-space: nowrap;
      }

      /* Hover tooltip on Total Active count. */
      .conn__active-wrap {
        position: relative;
        display: inline-block;
      }
      .conn__active-count {
        background: #dcfce7;
        color: #166534;
        font-weight: 600;
        padding: 0.15rem 0.6rem;
        border-radius: 999px;
        font-size: 0.8125rem;
        min-width: 1.75rem;
        display: inline-block;
        text-align: center;
        cursor: default;
      }
      .conn__active-count--zero {
        background: #f1f5f9;
        color: #94a3b8;
      }
      .conn__active-tip {
        display: none;
        position: absolute;
        bottom: calc(100% + 8px);
        left: 50%;
        transform: translateX(-50%);
        background: #0f172a;
        color: #ffffff;
        border-radius: 6px;
        padding: 0.5rem 0.75rem;
        font-size: 0.75rem;
        white-space: nowrap;
        z-index: 100;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.18);
        text-align: left;
        pointer-events: none;
      }
      .conn__active-tip::after {
        content: '';
        position: absolute;
        top: 100%;
        left: 50%;
        transform: translateX(-50%);
        border: 5px solid transparent;
        border-top-color: #0f172a;
      }
      .conn__active-tip-header {
        font-weight: 600;
        margin-bottom: 0.25rem;
        color: #cbd5e1;
        text-transform: uppercase;
        font-size: 0.6875rem;
        letter-spacing: 0.04em;
      }
      .conn__active-tip-row {
        line-height: 1.4;
      }
      .conn__active-wrap:hover .conn__active-tip {
        display: block;
      }
    `,
  ],
})
export class TmsSystemsComponent implements OnInit {
  systems: TmsSystem[] = [];
  loading = false;
  error: string | null = null;
  showCreateForm = false;
  activeOnly = false;
  newSystem: CreateTmsSystemRequest = {
    name: '',
    displayName: '',
    description: '',
    version: '1.0',
    applicationId: '',
    capabilityId: '',
  };

  /** Column metadata for the unified data table. */
  columns: DataTableColumn[] = [
    { field: 'name', header: 'Name', width: '14rem' },
    { field: 'displayName', header: 'Display Name' },
    { field: 'version', header: 'Version', width: '5rem' },
    { field: '', header: 'Application', sortable: false, width: '11rem' },
    { field: '', header: 'Capability', sortable: false, width: '11rem' },
    { field: '', header: 'Total Active', sortable: false, width: '8rem', align: 'center' },
    { field: 'isActive', header: 'Status', width: '7rem' },
    { field: 'createdAt', header: 'Created', width: '9rem' },
    { field: '', header: '', sortable: false, width: '4rem', align: 'center' },
  ];

  /** Per-connection-id usage summary, refreshed any time we reload the data. */
  usage: Record<string, ConnectionUsageSummary> = {};

  constructor(
    private apiService: ApiService,
    private generalService: GeneralService,
  ) {}

  ngOnInit() {
    this.loadSystems();
  }

  loadSystems() {
    this.loading = true;
    this.error = null;

    // Load connections, deployments, applications and capabilities together so
    // we can derive per-connection usage columns (Application, Capability,
    // Total Active) without N additional requests.
    forkJoin({
      systems: this.apiService.getTmsSystems(this.activeOnly),
      deployments: this.apiService.getDeployments(),
      applications: this.apiService.getApplications(),
      capabilities: this.apiService.getCapabilities(),
      customers: this.apiService.getCustomers(),
    }).subscribe({
      next: (res) => {
        if (res.systems.success && res.systems.data) this.systems = res.systems.data.systems;
        const deployments: Deployment[] =
          res.deployments.success && res.deployments.data ? res.deployments.data.deployments : [];
        const applications: Application[] =
          res.applications.success && res.applications.data ? res.applications.data.applications : [];
        const capabilities: Capability[] =
          res.capabilities.success && res.capabilities.data ? res.capabilities.data.capabilities : [];
        const customers: Customer[] =
          res.customers.success && res.customers.data ? res.customers.data.customers : [];
        this.usage = this.buildUsageMap(deployments, applications, capabilities, customers);
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load connections';
        this.loading = false;
        console.error(err);
      },
    });
  }

  /** Build a connectionId → { applications, capabilities, totalActive, activeCustomers } lookup. */
  private buildUsageMap(
    deployments: Deployment[],
    applications: Application[],
    capabilities: Capability[],
    customers: Customer[],
  ): Record<string, ConnectionUsageSummary> {
    const appById = new Map(applications.map((a) => [a.id, a.displayName]));
    const capById = new Map(capabilities.map((c) => [c.id, c.displayName]));
    const custById = new Map(customers.map((c) => [c.customerId, c.customerName]));
    const out: Record<string, ConnectionUsageSummary> = {};
    for (const d of deployments) {
      if (!d.connectionId) continue;
      const entry =
        out[d.connectionId] ??
        (out[d.connectionId] = {
          applications: [],
          capabilities: [],
          totalActive: 0,
          activeCustomers: [],
        });
      const appName = appById.get(d.applicationId);
      const capName = capById.get(d.capabilityId);
      if (appName && !entry.applications.includes(appName)) entry.applications.push(appName);
      if (capName && !entry.capabilities.includes(capName)) entry.capabilities.push(capName);
      if (d.status === 'Active') {
        entry.totalActive += 1;
        const custName = custById.get(d.customerId) ?? d.customerId;
        if (!entry.activeCustomers.includes(custName)) entry.activeCustomers.push(custName);
      }
    }
    return out;
  }

  /** Template helpers — return display strings (or em-dash when empty). */
  applicationsFor(systemId: string): string {
    const list = this.usage[systemId]?.applications ?? [];
    return list.length ? list.sort().join(', ') : '—';
  }
  capabilitiesFor(systemId: string): string {
    const list = this.usage[systemId]?.capabilities ?? [];
    return list.length ? list.sort().join(', ') : '—';
  }
  totalActiveFor(systemId: string): number {
    return this.usage[systemId]?.totalActive ?? 0;
  }
  activeCustomerList(systemId: string): string[] {
    return (this.usage[systemId]?.activeCustomers ?? []).slice().sort();
  }

  /** Row kebab menu items. */
  menuFor(system: TmsSystem): MenuItem[] {
    return [
      {
        label: 'Delete',
        icon: 'pi pi-trash',
        styleClass: 'menu-item-danger',
        command: () => this.deleteSystem(system.id),
      },
    ];
  }

  createSystem() {
    this.apiService.createTmsSystem(this.newSystem).subscribe({
      next: (response) => {
        if (response.success) {
          this.showCreateForm = false;
          this.newSystem = {
            name: '',
            displayName: '',
            description: '',
            version: '1.0',
            applicationId: '',
            capabilityId: '',
          };
          this.loadSystems();
        }
      },
      error: (err) => {
        this.error = 'Failed to create connection';
        console.error(err);
      },
    });
  }

  deleteSystem(id: string) {
    this.generalService
      .confirm({
        title: 'Delete Connection',
        text: 'Are you sure you want to delete this connection?',
        confirmText: 'Yes, Delete',
        confirmColor: '#e74c3c',
        icon: 'warning',
      })
      .then((result) => {
        if (!result.isConfirmed) return;

        this.apiService.deleteTmsSystem(id).subscribe({
          next: () => {
            this.generalService.success('Connection deleted successfully');
            this.loadSystems();
          },
          error: (err) => {
            this.error = 'Failed to delete connection';
            console.error(err);
          },
        });
      });
  }
}

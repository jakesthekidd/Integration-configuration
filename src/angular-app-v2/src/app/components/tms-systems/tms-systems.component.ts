import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { GeneralService } from '../../services/general.service';
import { TmsSystem, CreateTmsSystemRequest } from '../../models/tms-system.model';
import { Application } from '../../models/application.model';
import { Capability } from '../../models/capability.model';
import { Deployment } from '../../models/deployment.model';

interface ConnectionUsageSummary {
  applications: string[];
  capabilities: string[];
  totalActive: number;
}

@Component({
    selector: 'app-tms-systems',
    imports: [CommonModule, FormsModule],
    templateUrl: './tms-systems.component.html',
    styleUrl: './tms-systems.component.scss'
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
    }).subscribe({
      next: (res) => {
        if (res.systems.success && res.systems.data) this.systems = res.systems.data.systems;
        const deployments: Deployment[] =
          res.deployments.success && res.deployments.data ? res.deployments.data.deployments : [];
        const applications: Application[] =
          res.applications.success && res.applications.data ? res.applications.data.applications : [];
        const capabilities: Capability[] =
          res.capabilities.success && res.capabilities.data ? res.capabilities.data.capabilities : [];
        this.usage = this.buildUsageMap(deployments, applications, capabilities);
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load connections';
        this.loading = false;
        console.error(err);
      },
    });
  }

  /** Build a connectionId → { applications, capabilities, totalActive } lookup. */
  private buildUsageMap(
    deployments: Deployment[],
    applications: Application[],
    capabilities: Capability[],
  ): Record<string, ConnectionUsageSummary> {
    const appById = new Map(applications.map((a) => [a.id, a.displayName]));
    const capById = new Map(capabilities.map((c) => [c.id, c.displayName]));
    const out: Record<string, ConnectionUsageSummary> = {};
    for (const d of deployments) {
      if (!d.connectionId) continue;
      const entry =
        out[d.connectionId] ?? (out[d.connectionId] = { applications: [], capabilities: [], totalActive: 0 });
      const appName = appById.get(d.applicationId);
      const capName = capById.get(d.capabilityId);
      if (appName && !entry.applications.includes(appName)) entry.applications.push(appName);
      if (capName && !entry.capabilities.includes(capName)) entry.capabilities.push(capName);
      if (d.status === 'Active') entry.totalActive += 1;
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

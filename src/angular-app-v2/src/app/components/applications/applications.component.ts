import { Component, OnInit } from '@angular/core';
import { TagModule } from 'primeng/tag';

import { ApiService } from '../../services/api.service';
import { Application } from '../../models/application.model';
import { Capability } from '../../models/capability.model';
import { forkJoin } from 'rxjs';

interface AppWithCaps extends Application {
  capabilities: Capability[];
}

type Dir = 'Inbound' | 'Outbound' | 'Bidirectional';

@Component({
  selector: 'app-applications',
  imports: [TagModule],
  template: `
    <div class="tf-section-header">
      <div>
        <h2>Applications &amp; Capabilities</h2>
        <p>
          Applications and their Capabilities are managed by engineering. Read-only catalog used by the
          Mapping Template editor and the Setup Wizard.
        </p>
      </div>
    </div>

    <div class="apps-body">
      @if (loading) {
        <div class="muted">Loading…</div>
      }

      @if (!loading) {
        <div class="apps">
          @for (app of apps; track app) {
            <article class="app-card">
              <header class="app-header">
                <div class="app-name-group">
                  <span class="app-name">{{ app.displayName }}</span>
                  <p-tag
                    [value]="app.isActive ? 'Active' : 'Inactive'"
                    [severity]="app.isActive ? 'success' : 'secondary'"
                    [rounded]="true"
                  />
                </div>
                <span class="count">
                  {{ app.capabilities.length }} {{ app.capabilities.length === 1 ? 'capability' : 'capabilities' }}
                </span>
              </header>
              @if (app.description) {
                <p class="app-desc">{{ app.description }}</p>
              }
              <ul class="caps">
                @for (cap of app.capabilities; track cap) {
                  <li class="cap">
                    <div class="cap-main">
                      <span class="cap-name">{{ cap.displayName }}</span>
                      <p-tag
                        [value]="cap.direction"
                        [severity]="dirSeverity(cap.direction)"
                        [rounded]="true"
                      />
                    </div>
                    @if (cap.description) {
                      <p class="cap-desc">{{ cap.description }}</p>
                    }
                  </li>
                }
                @if (app.capabilities.length === 0) {
                  <li class="empty muted">No capabilities defined.</li>
                }
              </ul>
            </article>
          }
        </div>
      }
    </div>
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        height: 100%;
        min-height: 0;
      }
      .apps-body {
        flex: 1 1 auto;
        min-height: 0;
        overflow: auto;
        padding: var(--tf-space-5) var(--tf-space-6);
      }
      .muted {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin: 0;
      }
      .apps {
        display: flex;
        flex-direction: column;
        gap: var(--tf-space-4);
      }
      .app-card {
        background: white;
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-md);
        padding: var(--tf-space-4) var(--tf-space-5);
      }
      .app-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: var(--tf-space-3);
      }
      .app-name-group {
        display: flex;
        align-items: center;
        gap: var(--tf-space-2);
      }
      .app-name {
        font-size: var(--tf-text-heading);
        font-weight: 700;
        color: var(--tf-text-strong);
      }
      .count {
        font-size: var(--tf-text-meta);
        color: var(--tf-text-muted);
        font-weight: 600;
        letter-spacing: 0.4px;
        text-transform: uppercase;
        white-space: nowrap;
      }
      .app-desc {
        font-size: var(--tf-text-body);
        color: var(--tf-text-muted);
        margin: var(--tf-space-1) 0 var(--tf-space-3) 0;
      }
      .caps {
        list-style: none;
        margin: var(--tf-space-3) 0 0 0;
        padding: 0;
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
        gap: var(--tf-space-2);
      }
      .cap {
        border: 1px solid var(--tf-slate-400);
        border-radius: var(--tf-radius-sm);
        padding: var(--tf-space-3);
        background: var(--tf-slate-50);
      }
      .cap-main {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--tf-space-2);
      }
      .cap-name {
        font-weight: 600;
        font-size: var(--tf-text-body);
        color: var(--tf-text-strong);
      }
      .cap-desc {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-meta);
        margin: var(--tf-space-1) 0 0 0;
        line-height: 1.4;
      }
      .empty {
        padding: var(--tf-space-2) var(--tf-space-3);
        color: var(--tf-text-muted);
      }
    `,
  ],
})
export class ApplicationsComponent implements OnInit {
  apps: AppWithCaps[] = [];
  loading = true;

  dirSeverity(dir: string): 'info' | 'warn' | 'contrast' {
    if (dir === 'Inbound') return 'info';
    if (dir === 'Outbound') return 'warn';
    return 'contrast';
  }

  constructor(private apiService: ApiService) {}

  ngOnInit() {
    this.apiService.getApplications().subscribe((res) => {
      if (!res.success || !res.data) {
        this.loading = false;
        return;
      }
      const applications = res.data.applications;
      if (applications.length === 0) {
        this.apps = [];
        this.loading = false;
        return;
      }
      forkJoin(applications.map((a) => this.apiService.getCapabilitiesForApplication(a.id))).subscribe((capRes) => {
        this.apps = applications.map((a, i) => ({
          ...a,
          capabilities: capRes[i].success && capRes[i].data ? capRes[i].data!.capabilities : [],
        }));
        this.loading = false;
      });
    });
  }
}

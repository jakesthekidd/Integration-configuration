import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';

import { ApplicationsComponent } from '../components/applications/applications.component';
import { TmsSystemsComponent } from '../components/tms-systems/tms-systems.component';
import { TemplatesComponent } from '../components/templates/templates.component';
import { LookupTablesComponent } from '../components/lookup-tables/lookup-tables.component';
import { TransformationLogsComponent } from '../components/transformation-logs/transformation-logs.component';
import { IntegrationsComponent } from '../components/integrations/integrations.component';

const TABS = ['applications', 'connections', 'templates', 'lookups', 'integrations', 'logs'] as const;
type TabId = (typeof TABS)[number];

/**
 * Admin (Integration Library) shell. Sub-navigation lives in the navy stage banner
 * (driven by AppComponent via stage tabs); this shell only renders the active tab's content.
 *
 * Active tab is sourced from the `?tab=` query param so it survives refresh and deep links.
 */
@Component({
  selector: 'app-admin-shell',
  imports: [
    ApplicationsComponent,
    TmsSystemsComponent,
    TemplatesComponent,
    LookupTablesComponent,
    TransformationLogsComponent,
    IntegrationsComponent,
  ],
  template: `
    @switch (activeTab()) {
      @case ('applications') { <app-applications /> }
      @case ('connections') { <app-tms-systems /> }
      @case ('templates') { <app-templates /> }
      @case ('lookups') { <app-lookup-tables /> }
      @case ('integrations') { <app-integrations /> }
      @case ('logs') { <app-transformation-logs /> }
    }
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        height: 100%;
        min-height: 0;
      }
    `,
  ],
})
export class AdminShellComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private currentUrl = signal<string>('');

  activeTab = computed<TabId>(() => {
    const url = this.currentUrl();
    const match = url.match(/[?&]tab=([^&]+)/);
    const id = match ? (decodeURIComponent(match[1]) as TabId) : ('applications' as TabId);
    return (TABS as readonly string[]).includes(id) ? id : 'applications';
  });

  constructor() {
    this.currentUrl.set(this.router.url || '');
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe((e) => {
      this.currentUrl.set((e as NavigationEnd).urlAfterRedirects);
    });
  }
}

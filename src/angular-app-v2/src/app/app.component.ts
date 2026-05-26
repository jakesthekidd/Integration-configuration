import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

import { HeaderApp, HeaderToolbarComponent } from './design-system/header-toolbar.component';
import { StageLayoutComponent, StageTab } from './design-system/stage-layout.component';
import { PasswordGateComponent } from './components/password-gate.component';
import { AuthService } from './services/auth.service';

/**
 * Top-level applications, each representing a distinct persona's view of integrations.
 *
 *  - `library` — Integration Library (engineering): build Connections, Mapping Templates,
 *                Lookup Tables, API Clients. The reusable catalog.
 *  - `setup`   — Customer Setup (professional services): apply published library pieces
 *                to a customer via the wizard.
 *
 * Naming is intentionally persona-led; both are "integrations" but the work is different.
 */
interface IntegrationApp extends HeaderApp {
  route: string;
  pageTitle: string;
  pageIcon: string;
}

const APPS: IntegrationApp[] = [
  {
    id: 'library',
    label: 'Integration Library',
    icon: 'pi pi-book',
    route: '/admin',
    pageTitle: 'Integration Library',
    pageIcon: 'pi pi-book',
  },
  {
    id: 'setup',
    label: 'Customer Setup',
    icon: 'pi pi-users',
    route: '/customers',
    pageTitle: 'Customer Setup',
    pageIcon: 'pi pi-users',
  },
];

/** Sub-tabs that live inside the navy stage banner when the Integration Library app is active. */
const LIBRARY_TABS: StageTab[] = [
  { id: 'applications', label: 'Applications & Capabilities' },
  { id: 'connections', label: 'Connections' },
  { id: 'templates', label: 'Mapping Templates' },
  { id: 'lookups', label: 'Lookup Tables' },
  { id: 'logs', label: 'Logs' },
];

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    HeaderToolbarComponent,
    StageLayoutComponent,
    ToastModule,
    ConfirmDialogModule,
    PasswordGateComponent,
  ],
  template: `
    @if (activeApp().id === 'library' && !auth.isAuthenticated()) {
      <app-password-gate />
    }

    <app-header-toolbar
      [pageLabel]="activeApp().pageTitle"
      userInitials="JS"
      [apps]="apps"
      [activeApp]="activeApp().id"
      (appChange)="onAppChange($event)"
    />

    <app-stage-layout
      class="stage-wrap"
      [pageTitle]="activeApp().pageTitle"
      [pageIcon]="activeApp().pageIcon"
      [breadcrumbs]="[]"
      [tabs]="stageTabs()"
      [activeTab]="activeStageTab()"
      [navType]="stageTabs().length ? 'tabs' : 'breadcrumbs'"
      [showSideNav]="false"
      [showPlaceholder]="false"
      (tabChange)="onStageTabChange($event)"
    >
      <router-outlet></router-outlet>
    </app-stage-layout>

    <p-toast position="top-right" [breakpoints]="{ '640px': { width: '100%', right: '0', left: '0' } }" />
    <p-confirmdialog [style]="{ width: '440px' }" />
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        height: 100vh;
        overflow: hidden;
        background: #ffffff;
      }
      .stage-wrap {
        flex: 1 1 auto;
        display: block;
        min-height: 0;
      }
    `,
  ],
})
export class AppComponent {
  auth = inject(AuthService);
  apps: HeaderApp[] = APPS.map(({ id, label, icon }) => ({ id, label, icon }));

  private currentUrl = signal<string>('');

  activeApp = computed<IntegrationApp>(() => {
    const url = this.currentUrl();
    return APPS.find((a) => url.startsWith(a.route)) ?? APPS[1]; // default to Customer Setup
  });

  /** Tabs shown in the navy stage banner — depends on the active app. */
  stageTabs = computed<StageTab[]>(() => {
    return this.activeApp().id === 'library' ? LIBRARY_TABS : [];
  });

  /** Active stage tab — derived from the `tab` query param, defaulting to the first tab. */
  activeStageTab = computed<string>(() => {
    const url = this.currentUrl();
    const tabs = this.stageTabs();
    if (!tabs.length) return '';
    const match = url.match(/[?&]tab=([^&]+)/);
    const fromUrl = match ? decodeURIComponent(match[1]) : null;
    return tabs.some((t) => t.id === fromUrl) ? fromUrl! : tabs[0].id;
  });

  constructor(private router: Router) {
    this.currentUrl.set(this.router.url || '/');
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe((e) => {
      this.currentUrl.set((e as NavigationEnd).urlAfterRedirects);
    });
  }

  onAppChange(id: string) {
    const app = APPS.find((a) => a.id === id);
    if (app) this.router.navigate([app.route]);
  }

  onStageTabChange(tabId: string) {
    this.router.navigate([this.activeApp().route], { queryParams: { tab: tabId } });
  }
}

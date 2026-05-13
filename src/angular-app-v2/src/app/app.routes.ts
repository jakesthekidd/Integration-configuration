import { Routes } from '@angular/router';
import { AdminShellComponent } from './shells/admin-shell.component';
import { WizardShellComponent } from './shells/wizard-shell.component';
import { CustomerDetailComponent } from './shells/customer-detail.component';

/**
 * Two top-level apps.
 *
 * Customer Setup app:
 *   /customers              — Customer list (table)
 *   /customers/:id          — Customer Detail (tree view + tabs; the workhorse)
 *
 * Integration Library app:
 *   /admin                  — landing tab (Applications & Capabilities)
 *   /admin?tab=...          — sub-tab
 *
 * The legacy /wizard/* routes are kept temporarily during the pivot so any
 * existing bookmarks redirect cleanly. They will be removed in Phase 5.
 */
export const routes: Routes = [
  { path: '', redirectTo: 'customers', pathMatch: 'full' },

  { path: 'admin', component: AdminShellComponent },

  // New Customer Setup hierarchy
  { path: 'customers', component: WizardShellComponent },
  { path: 'customers/:id', component: CustomerDetailComponent },

  // Legacy wizard routes — redirect to the new structure
  { path: 'wizard', redirectTo: 'customers', pathMatch: 'full' },
  { path: 'wizard/new', redirectTo: 'customers', pathMatch: 'full' },
  { path: 'wizard/customer/:id', redirectTo: 'customers/:id', pathMatch: 'full' },
  { path: 'wizard/customer/:id/new', redirectTo: 'customers/:id', pathMatch: 'full' },
  { path: 'wizard/deployment/:deploymentId/edit', redirectTo: 'customers', pathMatch: 'full' },

  { path: '**', redirectTo: 'customers' },
];

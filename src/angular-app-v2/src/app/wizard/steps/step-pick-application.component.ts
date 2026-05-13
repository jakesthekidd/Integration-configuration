import { Component, OnInit, computed, inject, signal } from '@angular/core';

import { ApiService } from '../../services/api.service';
import { Application } from '../../models/application.model';
import { WizardStateService } from '../wizard-state.service';
import { PickItem, WizardPickGridComponent } from '../wizard-pick-grid.component';

/**
 * Wizard step 2 — Pick an Application (the Transflo product the customer
 * is integrating with: WorkflowAI, Mobile, LTL Nav).
 */
@Component({
  selector: 'app-step-pick-application',
  imports: [WizardPickGridComponent],
  template: `
    <p class="intro">Which Transflo product is this customer integrating with?</p>
    <wizard-pick-grid [items]="items()" [selectedId]="state.draft().applicationId" (pick)="onPick($event)" />
  `,
  styles: [
    `
      .intro {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin: 0 0 var(--tf-space-4) 0;
      }
    `,
  ],
})
export class StepPickApplicationComponent implements OnInit {
  protected state = inject(WizardStateService);
  private api = inject(ApiService);

  apps = signal<Application[]>([]);

  /** Map application id → PrimeIcons class so cards have a visual identity. */
  private static readonly APP_ICONS: Record<string, string> = {
    'app-workflowai': 'pi pi-share-alt',
    'app-mobile': 'pi pi-mobile',
    'app-ltl-nav': 'pi pi-truck',
  };

  items = computed<PickItem[]>(() =>
    this.apps().map<PickItem>((a) => ({
      id: a.id,
      label: a.displayName,
      description: a.description ?? '',
      icon: StepPickApplicationComponent.APP_ICONS[a.id] ?? 'pi pi-box',
      tag: a.isActive
        ? { value: 'Active', severity: 'success' }
        : { value: 'Inactive', severity: 'secondary' },
      disabled: !a.isActive,
    })),
  );

  ngOnInit() {
    this.api.getApplications().subscribe((res) => {
      if (res.success && res.data) this.apps.set(res.data.applications);
    });
  }

  onPick(id: string) {
    const prev = this.state.draft().applicationId;
    // Changing the app invalidates downstream picks (capability, connection, template).
    this.state.patch({
      applicationId: id,
      ...(id !== prev
        ? { capabilityId: '', connectionId: '', templateId: '', templateVersion: null, credentials: {} }
        : {}),
    });
  }
}

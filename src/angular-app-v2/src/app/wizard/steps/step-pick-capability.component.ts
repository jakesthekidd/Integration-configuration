import { Component, computed, effect, inject, signal } from '@angular/core';

import { ApiService } from '../../services/api.service';
import { Capability, CapabilityDirection } from '../../models/capability.model';
import { WizardStateService } from '../wizard-state.service';
import { PickItem, WizardPickGridComponent } from '../wizard-pick-grid.component';

/**
 * Wizard step 3 — Pick a Capability under the previously-chosen Application.
 * Capabilities are filtered to the selected application's id; if the user
 * goes Back and changes the app, the list refetches automatically.
 */
@Component({
  selector: 'app-step-pick-capability',
  imports: [WizardPickGridComponent],
  template: `
    @if (!state.draft().applicationId) {
      <p class="warn">Pick an application in the previous step first.</p>
    } @else {
      <p class="intro">Pick the integration capability you're setting up for this customer.</p>
      <wizard-pick-grid [items]="items()" [selectedId]="state.draft().capabilityId" (pick)="onPick($event)" />
    }
  `,
  styles: [
    `
      .intro {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin: 0 0 var(--tf-space-4) 0;
      }
      .warn {
        color: var(--tf-required);
        font-size: var(--tf-text-body);
      }
    `,
  ],
})
export class StepPickCapabilityComponent {
  protected state = inject(WizardStateService);
  private api = inject(ApiService);

  capabilities = signal<Capability[]>([]);

  /** Map capability direction → PrimeNG tag severity (matches the legend in §7). */
  private static readonly DIRECTION_TAG: Record<
    CapabilityDirection,
    PickItem['tag']
  > = {
    Inbound: { value: 'Inbound', severity: 'info' },
    Outbound: { value: 'Outbound', severity: 'warn' },
    Bidirectional: { value: 'Bidirectional', severity: 'contrast' },
  };

  /** Map capability id → PrimeIcons class. Falls back to a generic icon. */
  private static readonly CAP_ICONS: Record<string, string> = {
    'cap-wfai-import-orders': 'pi pi-download',
    'cap-wfai-export-docs': 'pi pi-upload',
    'cap-wfai-webhook': 'pi pi-bolt',
    'cap-mobile-pod-upload': 'pi pi-camera',
    'cap-mobile-status': 'pi pi-sync',
    'cap-ltl-rate-quote': 'pi pi-dollar',
  };

  items = computed<PickItem[]>(() =>
    this.capabilities().map<PickItem>((c) => ({
      id: c.id,
      label: c.displayName,
      description: c.description ?? '',
      icon: StepPickCapabilityComponent.CAP_ICONS[c.id] ?? 'pi pi-cog',
      tag: StepPickCapabilityComponent.DIRECTION_TAG[c.direction],
      disabled: !c.isActive,
    })),
  );

  constructor() {
    // Refetch whenever the selected application changes.
    effect(() => {
      const appId = this.state.draft().applicationId;
      if (!appId) {
        this.capabilities.set([]);
        return;
      }
      this.api.getCapabilitiesForApplication(appId).subscribe((res) => {
        if (res.success && res.data) this.capabilities.set(res.data.capabilities);
      });
    });
  }

  onPick(id: string) {
    const prev = this.state.draft().capabilityId;
    // Changing the capability invalidates downstream connection/template picks.
    this.state.patch({
      capabilityId: id,
      ...(id !== prev ? { connectionId: '', templateId: '', templateVersion: null, credentials: {} } : {}),
    });
  }
}

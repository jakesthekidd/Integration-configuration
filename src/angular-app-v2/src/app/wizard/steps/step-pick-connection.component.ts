import { Component, OnInit, computed, inject, signal } from '@angular/core';

import { ApiService } from '../../services/api.service';
import { TmsSystem } from '../../models/tms-system.model';
import { WizardStateService } from '../wizard-state.service';
import { PickItem, WizardPickGridComponent } from '../wizard-pick-grid.component';

/**
 * Wizard step 4 — Pick a Connection (the adapter that talks to the customer's
 * external system: McLeod v23, SAP S/4, NetSuite, Webhook Receiver, SFTP).
 *
 * Future: the list should be filtered to Connections that have at least one
 * Published MasterTemplate for the (Application, Capability) picked previously.
 * For now we show all active Connections.
 */
@Component({
  selector: 'app-step-pick-connection',
  imports: [WizardPickGridComponent],
  template: `
    <p class="intro">Which system does this customer connect through?</p>
    <wizard-pick-grid [items]="items()" [selectedId]="state.draft().connectionId" (pick)="onPick($event)" />
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
export class StepPickConnectionComponent implements OnInit {
  protected state = inject(WizardStateService);
  private api = inject(ApiService);

  connections = signal<TmsSystem[]>([]);

  /** Map connection id → PrimeIcons class for visual identity. */
  private static readonly CONN_ICONS: Record<string, string> = {
    'conn-mcleod-v22': 'pi pi-truck',
    'conn-mcleod-v23': 'pi pi-truck',
    'conn-sap-s4': 'pi pi-database',
    'conn-netsuite': 'pi pi-database',
    'conn-webhook': 'pi pi-bolt',
    'conn-sftp': 'pi pi-server',
  };

  items = computed<PickItem[]>(() =>
    this.connections()
      .filter((c) => c.isActive)
      .map<PickItem>((c) => ({
        id: c.id,
        label: c.displayName,
        description: c.description ?? '',
        icon: StepPickConnectionComponent.CONN_ICONS[c.id] ?? 'pi pi-link',
        meta: `Version ${c.version}`,
      })),
  );

  ngOnInit() {
    this.api.getTmsSystems(true).subscribe((res) => {
      if (res.success && res.data) this.connections.set(res.data.systems);
    });
  }

  onPick(id: string) {
    const prev = this.state.draft().connectionId;
    // Changing the connection invalidates downstream credentials / template picks.
    this.state.patch({
      connectionId: id,
      ...(id !== prev ? { credentials: {}, templateId: '', templateVersion: null } : {}),
    });
  }
}

import { Component, OnInit, computed, inject, signal } from '@angular/core';

import { ApiService } from '../../services/api.service';
import { FieldMappingTemplate } from '../../models/template.model';
import { WizardStateService } from '../wizard-state.service';
import { PickItem, WizardPickGridComponent } from '../wizard-pick-grid.component';

/** Sentinel id used when the user opts to start from scratch instead of forking a master. */
export const TEMPLATE_FROM_SCRATCH = '__scratch__';

/**
 * Wizard step 6 — Pick a Master Mapping Template to fork, or start from scratch.
 *
 * Future: this list should be filtered to MasterTemplates published for the
 * (Application, Capability, Connection) tuple selected previously. Until the
 * template model carries those scope fields we show all Published templates
 * plus a "Start from scratch" option as the first card.
 */
@Component({
  selector: 'app-step-pick-template',
  imports: [WizardPickGridComponent],
  template: `
    <p class="intro">
      Pick a published Master Template to fork for this customer, or start from scratch.
      Whatever you pick can be customized in the next step.
    </p>
    <wizard-pick-grid [items]="items()" [selectedId]="state.draft().templateId" (pick)="onPick($event)" />
  `,
  styles: [
    `
      .intro {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin: 0 0 var(--tf-space-4) 0;
        max-width: 640px;
      }
    `,
  ],
})
export class StepPickTemplateComponent implements OnInit {
  protected state = inject(WizardStateService);
  private api = inject(ApiService);

  templates = signal<FieldMappingTemplate[]>([]);

  items = computed<PickItem[]>(() => {
    const scratch: PickItem = {
      id: TEMPLATE_FROM_SCRATCH,
      label: 'Start from scratch',
      description: 'Build a brand-new mapping for this customer with no inherited rules.',
      icon: 'pi pi-plus',
      meta: 'Empty template',
    };
    const fromMasters = this.templates()
      .filter((t) => (t.latestVersionStatus ?? t.status) === 'Published')
      .map<PickItem>((t) => ({
        id: t.id,
        label: t.name,
        description: t.description ?? '',
        icon: 'pi pi-file',
        meta: `v${t.version} · Published`,
        tag: { value: 'Published', severity: 'success' },
      }));
    return [scratch, ...fromMasters];
  });

  ngOnInit() {
    this.api.getTemplates().subscribe((res) => {
      if (res.success && res.data) this.templates.set(res.data.templates);
    });
  }

  onPick(id: string) {
    const prev = this.state.draft().templateId;
    // Persist the chosen template + version. Scratch = no master version pinned.
    const next = this.templates().find((t) => t.id === id);
    this.state.patch({
      templateId: id,
      templateVersion: next ? next.version : null,
      ...(id !== prev ? {} : {}),
    });
  }
}

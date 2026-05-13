import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';

import { ApiService } from '../../services/api.service';
import { Customer } from '../../models/customer.model';
import { WizardStateService } from '../wizard-state.service';
import { PickItem, WizardPickGridComponent } from '../wizard-pick-grid.component';

/**
 * Wizard step 1 — Select an existing customer to onboard a new integration for.
 *
 * Per PRODUCT-GUIDING-PRINCIPLES.md §6 the wizard does NOT create customers;
 * it binds library pieces to a customer already in the system.
 */
@Component({
  selector: 'app-step-select-customer',
  imports: [FormsModule, InputTextModule, IconFieldModule, InputIconModule, WizardPickGridComponent],
  template: `
    <p class="intro">Pick the customer this integration is being set up for.</p>

    <p-iconfield class="search" iconPosition="left">
      <p-inputicon class="pi pi-search" />
      <input
        pInputText
        type="text"
        placeholder="Search customers by name or TMS…"
        [ngModel]="query()"
        (ngModelChange)="query.set($event)"
      />
    </p-iconfield>

    <wizard-pick-grid [items]="items()" [selectedId]="state.draft().customerId" (pick)="onPick($event)" />
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .intro {
        color: var(--tf-text-muted);
        font-size: var(--tf-text-body);
        margin: 0 0 var(--tf-space-4) 0;
      }
      .search {
        display: block;
        margin-bottom: var(--tf-space-4);
        max-width: 420px;
      }
      .search input {
        width: 100%;
      }
    `,
  ],
})
export class StepSelectCustomerComponent implements OnInit {
  protected state = inject(WizardStateService);
  private api = inject(ApiService);

  customers = signal<Customer[]>([]);
  query = signal<string>('');

  items = computed<PickItem[]>(() => {
    const q = this.query().trim().toLowerCase();
    const matches = q
      ? this.customers().filter(
          (c) => c.customerName.toLowerCase().includes(q) || c.tmsName.toLowerCase().includes(q),
        )
      : this.customers();
    return matches.map<PickItem>((c) => ({
      id: c.customerId,
      label: c.customerName,
      description: c.tmsName,
      meta: c.lastSyncTime ? `Last sync ${this.formatDate(c.lastSyncTime)}` : 'Never synced',
      tag: c.enabled
        ? { value: 'Active', severity: 'success' }
        : { value: 'Inactive', severity: 'secondary' },
    }));
  });

  ngOnInit() {
    this.api.getCustomers().subscribe((res) => {
      if (res.success && res.data) this.customers.set(res.data.customers);
    });
  }

  onPick(id: string) {
    this.state.patch({ customerId: id });
  }

  private formatDate(iso: string): string {
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return iso;
    return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
  }
}

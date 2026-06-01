import { Injectable, computed, signal } from '@angular/core';
import { mockCustomers } from '../mocks/mock-data';
import { Customer } from '../models/customer.model';

const STORAGE_KEY = 'tf_customer_access_v1';

/**
 * Shape persisted to localStorage. Map of customerId → enabled bool + last-changed timestamp.
 * We keep this separate from the mock customer rows themselves so a refresh restores
 * user toggles without bleeding through seed-data edits.
 */
interface PersistedState {
  enabled: Record<string, boolean>;
  changedAt: Record<string, string>;
}

/**
 * Single source of truth for "which customers are exposed in the Customer Setup app".
 *
 * - The Integration Library "Customers" tab reads/writes this service.
 * - The Customer Setup app filters its loaded list through `isEnabled()`.
 * - All mutations go through `setIntegrationEnabled(ids, enabled)` — the one seam
 *   a backend dev needs to swap for a Supabase write later.
 */
@Injectable({ providedIn: 'root' })
export class CustomerAccessService {
  /** Mirror of the persisted enabled map, used as the reactive signal source. */
  private enabledMap = signal<Record<string, boolean>>({});
  private changedAtMap = signal<Record<string, string>>({});

  /** Snapshot held while an undo banner is showing (Enable action only). */
  private undoSnapshot = signal<{
    ids: string[];
    previousEnabled: Record<string, boolean>;
    previousChangedAt: Record<string, string>;
  } | null>(null);

  /** Full customer list (the seed data). Cached once; we never mutate it. */
  readonly allCustomers: Customer[] = [...mockCustomers].sort((a, b) =>
    a.customerName.localeCompare(b.customerName),
  );

  constructor() {
    this.hydrate();
  }

  /** Initial state: read from localStorage if present, otherwise seed from mockCustomers. */
  private hydrate() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as PersistedState;
        this.enabledMap.set(parsed.enabled ?? {});
        this.changedAtMap.set(parsed.changedAt ?? {});
        return;
      }
    } catch {
      // fall through to seed
    }
    const seedEnabled: Record<string, boolean> = {};
    const seedChangedAt: Record<string, string> = {};
    for (const c of mockCustomers) {
      seedEnabled[c.customerId] = !!c.integrationEnabled;
      if (c.integrationStatusChangedAt) {
        seedChangedAt[c.customerId] = c.integrationStatusChangedAt;
      }
    }
    this.enabledMap.set(seedEnabled);
    this.changedAtMap.set(seedChangedAt);
    this.persist();
  }

  private persist() {
    try {
      const payload: PersistedState = {
        enabled: this.enabledMap(),
        changedAt: this.changedAtMap(),
      };
      localStorage.setItem(STORAGE_KEY, JSON.stringify(payload));
    } catch {
      // localStorage unavailable — silently ignore (in-memory state still works)
    }
  }

  // ---- Reads ----------------------------------------------------------------

  /** Reactive view: every customer decorated with the current enabled/changedAt. */
  readonly decoratedCustomers = computed<Customer[]>(() => {
    const en = this.enabledMap();
    const ts = this.changedAtMap();
    return this.allCustomers.map((c) => ({
      ...c,
      integrationEnabled: !!en[c.customerId],
      integrationStatusChangedAt: ts[c.customerId] ?? c.integrationStatusChangedAt,
    }));
  });

  /** Only the customers Customer Setup should see. */
  readonly enabledCustomers = computed<Customer[]>(() =>
    this.decoratedCustomers().filter((c) => c.integrationEnabled),
  );

  /** Counts for the filter chips. */
  readonly counts = computed(() => {
    const all = this.decoratedCustomers();
    const enabled = all.filter((c) => c.integrationEnabled).length;
    return { all: all.length, enabled, disabled: all.length - enabled };
  });

  isEnabled(customerId: string): boolean {
    return !!this.enabledMap()[customerId];
  }

  // ---- The one mutation seam -----------------------------------------------

  /**
   * Flip integration access for one or more customers.
   * Captures a snapshot on Enable so the UI can wire an Undo banner.
   */
  setIntegrationEnabled(customerIds: string[], enabled: boolean): void {
    if (!customerIds.length) return;
    const prevEnabled = { ...this.enabledMap() };
    const prevChangedAt = { ...this.changedAtMap() };
    const nowIso = new Date().toISOString();

    const nextEnabled = { ...prevEnabled };
    const nextChangedAt = { ...prevChangedAt };
    for (const id of customerIds) {
      nextEnabled[id] = enabled;
      nextChangedAt[id] = nowIso;
    }
    this.enabledMap.set(nextEnabled);
    this.changedAtMap.set(nextChangedAt);
    this.persist();

    // Snapshot only when enabling — Disable goes through a confirmation dialog,
    // so an undo banner would be redundant.
    if (enabled) {
      this.undoSnapshot.set({
        ids: [...customerIds],
        previousEnabled: prevEnabled,
        previousChangedAt: prevChangedAt,
      });
    } else {
      this.undoSnapshot.set(null);
    }
  }

  // ---- Undo ----------------------------------------------------------------

  readonly pendingUndo = computed(() => this.undoSnapshot());

  undo(): void {
    const snap = this.undoSnapshot();
    if (!snap) return;
    this.enabledMap.set(snap.previousEnabled);
    this.changedAtMap.set(snap.previousChangedAt);
    this.persist();
    this.undoSnapshot.set(null);
  }

  clearUndo(): void {
    this.undoSnapshot.set(null);
  }
}

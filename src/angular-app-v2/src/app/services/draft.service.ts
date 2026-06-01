import { Injectable, computed, signal } from '@angular/core';

/**
 * Shared draft-existence tracker per deployment.
 *
 * Updated by the Publish & Activate tab whenever its in-memory version list
 * gains or loses a Draft. Read by the customer-detail shell to decorate the
 * "Publish & Activate" tab label with a small amber indicator so users on
 * Connection/Mapping/Activity tabs can see at a glance that uncommitted
 * changes exist.
 */
@Injectable({ providedIn: 'root' })
export class DraftService {
  private draftsByDeployment = signal<Record<string, boolean>>({});

  /** True if a Draft version currently exists for the given deployment. */
  hasDraft = (deploymentId: string): boolean => !!this.draftsByDeployment()[deploymentId];

  /** Computed signal — use this in templates so updates are reactive. */
  hasDraft$ = (deploymentId: string) =>
    computed(() => !!this.draftsByDeployment()[deploymentId]);

  setDraft(deploymentId: string, exists: boolean) {
    this.draftsByDeployment.update((m) => {
      if (!!m[deploymentId] === exists) return m;
      return { ...m, [deploymentId]: exists };
    });
  }

  /**
   * Optional "view this archived/published version" focus per deployment.
   * Set by the Publish & Activate tab when the user clicks "View field mappings";
   * read by the Mapping tab to scope itself read-only to that version.
   */
  private viewVersionByDeployment = signal<Record<string, { id: string; label: string } | null>>(
    {},
  );

  viewVersion = (deploymentId: string) => this.viewVersionByDeployment()[deploymentId] ?? null;

  setViewVersion(deploymentId: string, value: { id: string; label: string } | null) {
    this.viewVersionByDeployment.update((m) => ({ ...m, [deploymentId]: value }));
  }
}

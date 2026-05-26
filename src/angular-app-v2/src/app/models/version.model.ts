/**
 * Version — one snapshot of (Connection + Mapping) on a Deployment.
 *
 * Per DESIGN-STATUS-VERSIONING.md, each deployment has a stack of these
 * (multiple Drafts allowed, exactly one Activated, history Archived).
 * Draft + Published versions are mutable in spirit (Drafts especially);
 * Activated + Archived are conceptually immutable snapshots.
 */
export type VersionState = 'Draft' | 'Published' | 'Activated' | 'Archived';

export interface Version {
  id: string;
  deploymentId: string;
  /** Monotonically increasing per-deployment. 1, 2, 3, … */
  versionNumber: number;
  state: VersionState;
  /** ISO. Set when the version row is first created. */
  createdAt: string;
  /** Author display name for the header line ("by Jake Cummings"). */
  createdBy: string;
  /** ISO. Set when the version first transitions to Published. */
  publishedAt?: string;
  /** ISO. Set when the version first transitions to Activated. */
  activatedAt?: string;
  /** ISO. Set when the version transitions to Archived. */
  archivedAt?: string;
  /** Optional one-line change summary the author wrote when forking the draft. */
  notes?: string;
}

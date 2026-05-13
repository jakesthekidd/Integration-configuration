/**
 * Deployment — the activation bundle that ties a Customer to a specific
 * (Application × Capability × Connection × CustomerTemplate × Credentials)
 * with a lifecycle status. Per PRODUCT-GUIDING-PRINCIPLES.md §4 only one
 * Deployment is `Active` per (customerId, applicationId, capabilityId) tuple.
 */

export type DeploymentStatus = 'Draft' | 'Tested' | 'Published' | 'Active' | 'Retired';

export interface Deployment {
  id: string;
  customerId: string;
  applicationId: string;
  capabilityId: string;
  connectionId: string;
  /** Forked from this MasterTemplate id (empty if from scratch). */
  forkedFromTemplateId: string;
  /** Version of the master at the time of fork. Null when from scratch. */
  forkedFromTemplateVersion: number | null;
  /** Optional API client identity assigned to the deployment. */
  apiClientId?: string;
  status: DeploymentStatus;
  createdAt: string;
  updatedAt: string;
  /** Last successful real-order test correlation id, if any. */
  lastTestCorrelationId?: string;
  /** Snapshot version number — incremented on each Publish. */
  snapshotVersion: number;
}

export interface DeploymentListResponse {
  deployments: Deployment[];
  totalCount: number;
}

export type CapabilityDirection = 'Inbound' | 'Outbound' | 'Bidirectional';

export interface Capability {
  id: string;
  applicationId: string;
  name: string;
  displayName: string;
  description?: string;
  direction: CapabilityDirection;
  isActive: boolean;
  createdAt: string;
}

export interface CapabilityListResponse {
  capabilities: Capability[];
  totalCount: number;
}

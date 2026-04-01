export interface Customer {
  customerId: string;
  tmsName: string;
  lastSyncTime: string;
  updateOrInsertStatuses?: string | null;
  updateOnlyStatuses?: string | null;
  customerName: string;
  credentials: { [key: string]: string | null };
  settings?: { [key: string]: string } | null;
  syncFrequencyMinutes?: number | null;
  orderRetentionDays?: number | null;
  enabled: boolean;
  tonuCode?: string | null;
  outboundEnabled: boolean;
  whiteListedOrders?: string | null;
  syncBatchSize?: number | null;
  updateOrInsertStatusesList?: string[];
  updateOnlyStatusesList?: string[];
}

export interface Credential {
  key: string;
  value: string;
}

export interface CustomerRequest {
  customerName: string;
  tmsName: string;
  lastSyncTime: string;
  enabled: boolean;
  outboundEnabled: boolean;
}

export interface CustomerListResponse {
  customers: Customer[];
  totalCount: number;
}

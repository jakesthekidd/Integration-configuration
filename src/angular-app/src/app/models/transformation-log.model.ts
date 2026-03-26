export type TransformationStatus = 'Success' | 'Warning' | 'PartialSuccess' | 'Error';

export interface TransformationLogSummary {
  id: string;
  templateId: string;
  templateName?: string;
  timestamp: string;
  status: TransformationStatus;
  executionTimeMs: number;
  recordCount: number;
  source?: string;
  userId?: string;
  expiresAt?: string;
  hasErrors: boolean;
  hasOutput: boolean;
}

export interface TransformationLogDetail extends TransformationLogSummary {
  inputData?: string;
  outputData?: string;
  errors?: string;
}

export interface TransformationLogListResponse {
  logs: TransformationLogSummary[];
  totalCount: number;
}

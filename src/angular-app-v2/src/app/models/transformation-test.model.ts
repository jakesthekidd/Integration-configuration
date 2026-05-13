export interface TransformRequest {
  sourceJson: string;
  templateId: string;
  version?: number;
}

export interface MappingIssue {
  type: 'error' | 'warning';
  code: string;
  sourcePath?: string;
  targetPath?: string;
  message: string;
}

export interface TransformResult {
  success: boolean;
  outputJson?: string;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  transformedData?: any;
  fieldsMapped?: number;
  fieldsSkipped?: number;
  errors?: Array<{
    errorCode: string;
    fieldPath?: string;
    sourcePath?: string;
    message: string;
  }>;
  warnings?: Array<{
    code: string;
    sourcePath?: string;
    targetPath?: string;
    message: string;
  }>;
  executionTimeMs?: number;
}

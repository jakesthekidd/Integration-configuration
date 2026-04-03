export interface FieldMapping {
  id: string;
  templateId: string;
  sourcePath: string;
  targetPath: string;
  transformationType: string;
  transformationConfig?: string;
  executionOrder: number;
  isRequired: boolean;
  defaultValue?: string;
  validationRules?: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateFieldMappingRequest {
  templateId: string;
  templateVersionId?: string;
  sourcePath: string;
  targetPath: string;
  transformationType: string;
  transformationConfig?: string;
  executionOrder: number;
  isRequired: boolean;
  defaultValue?: string;
  validationRules?: string;
  lookupTableId?: string
}

export interface UpdateFieldMappingRequest {
  sourcePath: string;
  targetPath: string;
  transformationType: string;
  transformationConfig?: string;
  executionOrder: number;
  isRequired: boolean;
  defaultValue?: string;
  validationRules?: string;
}

export interface FieldMappingListResponse {
  mappings: FieldMapping[];
  totalCount: number;
}

export const TransformationTypes = [
  'Direct',
  'Concat',
  'Lookup',
  'Conditional',
  'ArrayMap',
  'ArrayFlatten',
  'DateFormat',
  'Math',
  'Substring',
  'Constant',
  'Template',
];

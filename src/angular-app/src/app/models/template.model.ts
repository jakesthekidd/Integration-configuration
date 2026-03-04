export interface FieldMappingTemplate {
  templateId: string;
  name: string;
  description?: string;
  tmsSystemId: string;
  customerId?: string;
  version: number;
  status: string;
  createdAt: Date;
  updatedAt: Date;
  createdBy?: string;
  sampleInputJson?: string;
  metadata?: string;
}

export interface CreateTemplateRequest {
  name: string;
  description?: string;
  tmsSystemId: string;
  customerId?: string;
  sampleInputJson?: string;
  metadata?: string;
}

export interface UpdateTemplateRequest {
  name?: string;
  description?: string;
  status?: string;
  customerId?: string;
  sampleInputJson?: string;
  metadata?: string;
}

export interface TemplateListResponse {
  templates: FieldMappingTemplate[];
  totalCount: number;
}

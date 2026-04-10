export interface FieldMappingTemplate {
  id: string; // Correctly matching backend DTO
  name: string;
  description?: string;
  version: number;
  status: string;
  latestVersionStatus?: string;
  createdAt: Date;
  updatedAt: Date;
  createdBy?: string;
  sampleInputJson?: string;
  sourceSchema?: string;
  targetSchema?: string;
  metadata?: string;
}

export interface CreateTemplateRequest {
  name: string;
  description?: string;
  sampleInputJson?: string;
  sourceSchema?: string;
  targetSchema?: string;
  metadata?: string;
}

export interface UpdateTemplateRequest {
  name?: string;
  description?: string;
  status?: string;
  sampleInputJson?: string;
  sourceSchema?: string;
  targetSchema?: string;
  metadata?: string;
}

export interface TemplateListResponse {
  templates: FieldMappingTemplate[];
  totalCount: number;
}

export interface TemplateVersionResponse {
  id: string;
  templateId: string;
  templateName?: string;
  templateStatus?: string;
  version: number;
  status: string;
  publishedAt?: string;
}

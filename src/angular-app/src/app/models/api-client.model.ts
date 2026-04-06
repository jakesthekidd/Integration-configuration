import { TemplateVersionResponse } from './template.model';

export interface ApiClient {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  createdBy?: string;
}

export interface CreateApiClientRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

export interface UpdateApiClientRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

export interface ApiClientListResponse {
  apiClients: ApiClient[];
  totalCount: number;
}

export interface ApiClientTemplateAssignmentRequest {
  templateVersionId: string;
}

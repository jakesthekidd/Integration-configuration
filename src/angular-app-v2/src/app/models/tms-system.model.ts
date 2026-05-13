export interface TmsSystem {
  id: string;
  name: string;
  displayName: string;
  description?: string;
  version: string;
  isActive: boolean;
  sampleJsonSchema?: string;
  connectionConfig?: string;
  createdAt: Date;
  updatedAt: Date;
  createdBy?: string;
  metadata?: string;
}

export interface CreateTmsSystemRequest {
  name: string;
  displayName: string;
  description?: string;
  version?: string;
  sampleJsonSchema?: string;
  connectionConfig?: string;
  metadata?: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

export interface TmsSystemListResponse {
  systems: TmsSystem[];
  totalCount: number;
}

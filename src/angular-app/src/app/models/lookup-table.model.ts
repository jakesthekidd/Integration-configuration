export interface LookupTable {
  id: string;
  tmsSystemId: string;
  fieldName: string;
  name: string;
  description?: string;
  mappings?: string; // JSON string of key-value pairs
  defaultValue?: string;
  isCaseSensitive: boolean;
  createdAt: Date;
  updatedAt: Date;
  createdBy?: string;
}

export interface CreateLookupTableRequest {
  tmsSystemId: string;
  fieldName: string;
  name: string;
  description?: string;
  mappings?: string;
  defaultValue?: string;
  isCaseSensitive: boolean;
}

export interface UpdateLookupTableRequest {
  fieldName: string;
  name: string;
  description?: string;
  mappings?: string;
  defaultValue?: string;
  isCaseSensitive: boolean;
}

export interface LookupTableListResponse {
  lookupTables: LookupTable[];
  totalCount: number;
}

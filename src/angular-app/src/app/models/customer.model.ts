export interface Customer {
  id: string;
  name: string;
  code?: string;
  contactEmail?: string;
  contactPhone?: string;
  isActive: boolean;
  notes?: string;
  createdAt: Date;
  updatedAt: Date;
  createdBy?: string;
}

export interface CreateCustomerRequest {
  name: string;
  code?: string;
  contactEmail?: string;
  contactPhone?: string;
  isActive: boolean;
  notes?: string;
  createdBy?: string;
}

export interface UpdateCustomerRequest {
  name?: string;
  code?: string;
  contactEmail?: string;
  contactPhone?: string;
  isActive?: boolean;
  notes?: string;
}

export interface CustomerListResponse {
  customers: Customer[];
  totalCount: number;
}

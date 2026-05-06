export interface PartnerListResponse {
  partners: Partner[];
  totalCount: number;
  page: number;
  pageSize: number;
}
export interface Partner {
  id: string;
  name: string;
  description?: string;
  createdAt: Date;
  updatedAt: Date;
  deletedAt?: Date;
  isDeleted: boolean;
  revision: number;
}

export interface CreatePartnerRequest {
  name: string;
  description?: string;
}

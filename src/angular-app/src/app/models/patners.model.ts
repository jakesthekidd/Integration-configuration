export interface Partner {
  id: string;
  name: string;
  createdAt: Date;
  updatedAt: Date;
  deletedAt?: Date;
  isDeleted: boolean;
  createdBy?: string | null;
  updatedBy?: string | null;
  metadata?: string | null;
  revision: number;
}

export interface CreatePartnerRequest {
  name: string;
}

export interface PartnerListResponse {
  partners: Partner[];
  totalCount: number;
}

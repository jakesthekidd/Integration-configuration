export interface Partner {
  id: string;
  name: string;
  description?: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface PartnerListResponse {
  partners: Partner[];
  totalCount: number;
  page: number;
  pageSize: number;
}
